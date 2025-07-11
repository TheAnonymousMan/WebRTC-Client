using System;
using Newtonsoft.Json;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;

/// <summary>
/// Manages WebSocket connection to a signaling server for exchanging WebRTC offer, answer, and ICE candidates.
/// Acts as the communication bridge between client and server for signaling.
/// </summary>
public class WebSocketClient : MonoBehaviour
{
    // Holds the current WebSocket connection instance
    public WebSocket ActiveWebSocket;

    // Singleton instance so this class can be accessed globally
    public static WebSocketClient Singleton { get; private set; }

    // Event invoked when WebSocket connects
    public event Action OnConnected;

    // Event invoked when WebSocket disconnects
    public event Action OnDisconnected;

    /// <summary>
    /// Ensures only one instance exists and persists across scenes.
    /// </summary>
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

    /// <summary>
    /// Initiates connection to the WebSocket signaling server.
    /// </summary>
    /// <param name="ip">IP and port of the server (e.g., "localhost:8080")</param>
    public void Connect(string ip = "localhost:8080")
    {
        Debug.Log($"[WebSocketClient] Connecting to server {ip}");

        // Create WebSocket client targeting the /ws endpoint
        ActiveWebSocket = new WebSocket($"ws://{ip}/ws");

        // Define behavior on successful connection
        ActiveWebSocket.OnOpen += (sender, e) =>
        {
            OnConnected?.Invoke();
            Debug.Log("[WebSocketClient] Connected to server.");
        };

        // Define behavior when a message is received
        ActiveWebSocket.OnMessage += (sender, e) =>
        {
            Debug.Log($"[WebSocketClient] Message received: {e.Data}");

            // Deserialize the JSON message into a WebRtcMessage object
            var message = JsonConvert.DeserializeObject<WebRtcMessage>(e.Data);

            // Handle signaling message types accordingly
            switch (message.Type)
            {
                case "answer":
                    Debug.Log("[WebSocketClient] Answer received");

                    // Enqueue handling on main Unity thread (required for WebRTC API)
                    MainThreadDispatcher.Enqueue(
                        WebRtcClientManager.Singleton.OnAnswerReceived(message)
                    );
                    break;

                case "candidate":
                    Debug.Log("[WebSocketClient] Candidate received");

                    // Directly apply or queue ICE candidate
                    WebRtcClientManager.Singleton.OnIceCandidateReceived(message);
                    break;
            }
        };

        // Define behavior on WebSocket close
        ActiveWebSocket.OnClose += (sender, e) =>
        {
            OnDisconnected?.Invoke();
            Debug.Log($"[WebSocketClient] Disconnected from server {sender}.");
        };

        // Start the WebSocket connection
        ActiveWebSocket.Connect();
    }

    /// <summary>
    /// Ensures graceful shutdown of WebSocket connection when application exits.
    /// </summary>
    private void OnApplicationQuit()
    {
        ActiveWebSocket?.Close();
    }
}
