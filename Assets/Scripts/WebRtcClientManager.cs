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

        // Create RTC connection configuration u
