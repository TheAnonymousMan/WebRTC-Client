using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class WebRtcClientManager : MonoBehaviour
{
    public static WebRtcClientManager Singleton;
    [SerializeField] private WebRtcVideoPlayer videoPlayer;

    private RTCPeerConnection _localConnection;
    private RTCDataChannel _sendChannel;

    private MediaStream receiveStream;

    private bool _isSendChannelReady;
    private readonly Queue<string> _queuedMessages = new();
    
    public event Action OnIceConnected;
    public event Action OnIceDisconnected;

    private bool _remoteDescSet = false;
    private readonly List<RTCIceCandidate> _pendingCandidates = new();

    private void Awake()
    {
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
        StartCoroutine(WebRTC.Update());
        if (videoPlayer == null)
            videoPlayer = FindFirstObjectByType<WebRtcVideoPlayer>();
    }

    public IEnumerator Connect()
    {
        Debug.Log("Connecting...");
        RTCConfiguration configuration = new RTCConfiguration
        {
            iceServers = new[]
            {
                new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } }
            }
        };
        _localConnection = new RTCPeerConnection(ref configuration);
        _localConnection.AddTransceiver(TrackKind.Video);
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

        _localConnection.OnIceConnectionChange = state =>
        {
            switch (state)
            {
                case RTCIceConnectionState.New:
                    Debug.Log($"IceConnectionState: New");
                    break;
                case RTCIceConnectionState.Checking:
                    Debug.Log($"IceConnectionState: Checking");
                    break;
                case RTCIceConnectionState.Closed:
                    OnIceDisconnected?.Invoke();
                    Debug.Log($"IceConnectionState: Closed");
                    break;
                case RTCIceConnectionState.Completed:
                    Debug.Log($"IceConnectionState: Completed");
                    break;
                case RTCIceConnectionState.Connected:
                    OnIceConnected?.Invoke();
                    Debug.Log($"IceConnectionState: Connected");
                    break;
                case RTCIceConnectionState.Disconnected:
                    OnIceDisconnected?.Invoke();
                    Debug.Log($"IceConnectionState: Disconnected");
                    break;
                case RTCIceConnectionState.Failed:
                    OnIceDisconnected?.Invoke();
                    Debug.Log($"IceConnectionState: Failed");
                    break;
                case RTCIceConnectionState.Max:
                    Debug.Log($"IceConnectionState: Max");
                    break;
                default:
                    break;
            }
        };

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

        receiveStream = new MediaStream();
        receiveStream.OnAddTrack = evt =>
        {
            if (evt.Track is VideoStreamTrack videoTrack)
            {
                Debug.Log("[WebRtcClientManager] Video track received via OnAddTrack.");
                videoPlayer?.SetTrack(videoTrack);
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

    public IEnumerator OnAnswerReceived(WebRtcMessage msg)
    {
        var desc = new RTCSessionDescription
        {
            type = RTCSdpType.Answer,
            sdp = msg.Sdp
        };
        yield return _localConnection.SetRemoteDescription(ref desc);
        _remoteDescSet = true;

        foreach (var c in _pendingCandidates)
            _localConnection.AddIceCandidate(c);
        _pendingCandidates.Clear();
    }

    private void HandleSendChannelStatusChange()
    {
        if (_sendChannel.ReadyState == RTCDataChannelState.Open)
        {
            Debug.Log("[WebRtcClientManager] Send channel opened.");
            _isSendChannelReady = true;

            // Flush queued messages
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

    private void HandleReceiveMessage(byte[] bytes)
    {
        var message = Encoding.UTF8.GetString(bytes);
        Debug.Log($"Message received: {message}");
    }

    private void OnDestroy()
    {
        _sendChannel?.Close();
        _localConnection?.Close();
        receiveStream?.Dispose();
    }
}