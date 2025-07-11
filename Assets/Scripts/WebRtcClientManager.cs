using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles WebRTC client-side operations: connection setup, data channel messaging,
/// receiving video streams, and signaling via WebSocket.
/// </summary>
public class WebRtcClientManager : MonoBehaviour
{
    public static WebRtcClientManager Singleton;

    [SerializeField] private WebRtcVideoPlayer videoPlayer; // Component for rendering received video frames

    private RTCPeerConnection _localConnection;
    private RTCDataChannel _sendChannel; // Used for data messaging

    private MediaStream receiveStream; // Holds received remote media tracks

    private bool _isSendChannelReady;
    private readonly Queue<string> _queuedMessages = new(); // Messages queued until the channel is ready

    // Events to notify UI of ICE connection state changes
    public event Action OnIceConnected;
    public event Action OnIceDisconnected;

    private bool _remoteDescSet = false;
    private readonly List<RTCIceCandidate> _pendingCandidates = new(); // ICE candidates buffered before remote description is set

    private void Awake()
    {
        // Singleton setup to persist and prevent duplicates
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        Singleton = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Start()
    {
        // Required for Unity WebRTC to process native events
        StartCoroutine(WebRTC.Update());

        // Try to auto-find the video player if not assigned
        if (videoPlayer == null)
            videoPlayer = FindFirstObjectByType<WebRtcVideoPlayer>();
    }

    /// <summary>
    /// Coroutine to create a new WebRTC offer and send it over the signaling channel.
    /// </summary>
    public IEnumerator Connect()
    {
        Debug.Log("Connecting...");

        // Create RTC connection configuration using Google's STUN server
        RTCConfiguration configuration = new RTCConfiguration
        {
            iceServers = new[]
            {
                new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } }
            }
        };

        // Create peer connection
        _localConnection = new RTCPeerConnection(ref configuration);

        // Add video transceiver so we can receive remote video
        _localConnection.AddTransceiver(TrackKind.Video);

        // Create a data channel for sending/receiving messages
        _sendChannel = _localConnection.CreateDataChannel("chat");
        _sendChannel.OnOpen = () =>
        {
            Debug.Log("[WebRtcClientManager::OnOpen] DataChannel open");
            HandleSendChannelStatusChange();
        };
        _sendChannel.OnMessage = bytes =>
        {
            Debug.Log($"[WebRtcClientManager::OnMessage] Received: {Encoding.UTF8.GetString(bytes)}");
            HandleReceiveMessage(bytes);
        };
        _sendChannel.OnClose = () =>
        {
            Debug.Log($"[WebRtcClientManager::OnClose] DataChannel closed");
            HandleSendChannelStatusChange();
        };

        // Handle ICE connection state changes
        _localConnection.OnIceConnectionChange = state =>
        {
            switch (state)
            {
                case RTCIceConnectionState.Connected:
                    Debug.Log("IceConnectionState: Connected");
                    OnIceConnected?.Invoke();
                    break;
                case RTCIceConnectionState.Disconnected:
                case RTCIceConnectionState.Failed:
                case RTCIceConnectionState.Closed:
                    Debug.Log($"IceConnectionState: {state}");
                    OnIceDisconnected?.Invoke();
                    break;
                default:
                    Debug.Log($"IceConnectionState: {state}");
                    break;
            }
        };

        // Send ICE candidates to remote peer via WebSocket
        _localConnection.OnIceCandidate = candidate =>
        {
            if (candidate != null)
            {
                Debug.Log($"[WebRtcClientManager] IceCandidate: {candidate.Candidate}");
                string json = JsonConvert.SerializeObject(new WebRtcMessage
                {
                    Type = "candidate",
                    Candidate = candidate.Candidate,
                    SdpMid = candidate.SdpMid,
                    SdpMLineIndex = candidate.SdpMLineIndex ?? 0
                });
                WebSocketClient.Singleton.ActiveWebSocket.Send(json);
            }
        };

        // Initialize media stream and handle incoming tracks
        receiveStream = new MediaStream();
        receiveStream.OnAddTrack = evt =>
        {
            if (evt.Track is VideoStreamTrack videoTrack)
            {
                Debug.Log("[WebRtcClientManager] Video track received via OnAddTrack.");
                videoPlayer?.SetTrack(videoTrack); // Pass to video renderer
            }
        };

        _localConnection.OnTrack = e =>
        {
            Debug.Log("[WebRtcClientManager] OnTrack triggered.");
            if (e.Track is VideoStreamTrack)
            {
                receiveStream.AddTrack(e.Track);
            }
        };

        // Create and send offer
        var offerOp = _localConnection.CreateOffer();
        yield return offerOp;

        var offer = offerOp.Desc;
        yield return _localConnection.SetLocalDescription(ref offer);

        var msg = new WebRtcMessage
        {
            Type = "offer",
            Sdp = offer.sdp
        };

        WebSocketClient.Singleton.ActiveWebSocket.Send(JsonConvert.SerializeObject(msg));
    }

    /// <summary>
    /// Handles a received ICE candidate from the signaling server.
    /// Adds it immediately or buffers it if remote description isn't set yet.
    /// </summary>
    public void OnIceCandidateReceived(WebRtcMessage msg)
    {
        Debug.Log("[WebRtcClientManager] Ice candidate received");

        var candidate = new RTCIceCandidate(new RTCIceCandidateInit
        {
            candidate = msg.Candidate,
            sdpMid = msg.SdpMid,
            sdpMLineIndex = msg.SdpMLineIndex
        });

        if (_remoteDescSet)
        {
            _localConnection.AddIceCandidate(candidate);
        }
        else
        {
            Debug.Log("[WebRtcClientManager] Candidate buffered.");
            _pendingCandidates.Add(candidate);
        }
    }

    /// <summary>
    /// Applies the answer SDP received from the remote peer.
    /// </summary>
    public IEnumerator OnAnswerReceived(WebRtcMessage msg)
    {
        var desc = new RTCSessionDescription
        {
            type = RTCSdpType.Answer,
            sdp = msg.Sdp
        };

        yield return _localConnection.SetRemoteDescription(ref desc);
        _remoteDescSet = true;

        // Apply any candidates that were buffered before remote description was set
        foreach (var c in _pendingCandidates)
            _localConnection.AddIceCandidate(c);
        _pendingCandidates.Clear();
    }

    /// <summary>
    /// Handles opening/closing of the data channel.
    /// Sends any messages that were queued before the channel was open.
    /// </summary>
    private void HandleSendChannelStatusChange()
    {
        if (_sendChannel.ReadyState == RTCDataChannelState.Open)
        {
            Debug.Log("[WebRtcClientManager] Send channel opened.");
            _isSendChannelReady = true;

            // Send all buffered messages
            while (_queuedMessages.Count > 0)
            {
                string msg = _queuedMessages.Dequeue();
                _sendChannel.Send(msg);
                Debug.Log($"[WebRtcClientManager] Flushed message: {msg}");
            }
        }
        else
        {
            Debug.Log("[WebRtcClientManager] Send channel closed or not ready.");
            _isSendChannelReady = false;
        }
    }

    /// <summary>
    /// Queues or sends a message through the WebRTC data channel.
    /// </summary>
    public void SendMessageBuffered(string message)
    {
        if (_isSendChannelReady)
        {
            _sendChannel.Send(message);
            Debug.Log($"[WebRtcClientManager] Sent message: {message}");
        }
        else
        {
            Debug.Log("[WebRtcClientManager] Channel not ready. Queuing message.");
            _queuedMessages.Enqueue(message);
        }
    }

    /// <summary>
    /// Handles receiving a message over the data channel.
    /// </summary>
    private void HandleReceiveMessage(byte[] bytes)
    {
        var message = Encoding.UTF8.GetString(bytes);
        Debug.Log($"Message received: {message}");
        // You can optionally trigger a UI update or event here
    }

    /// <summary>
    /// Cleans up all WebRTC-related resources when the object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        _sendChannel?.Close();
        _localConnection?.Close();
        receiveStream?.Dispose();
    }
}
