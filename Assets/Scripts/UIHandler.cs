using UnityEngine;

/// <summary>
/// Controls the visibility of connection and messaging UI screens based on WebSocket and WebRTC connection events.
/// Subscribes to connection/disconnection events and updates the UI accordingly.
/// </summary>
public class UIHandler : MonoBehaviour
{
    // Reference to the UI GameObject shown while the user is connecting
    [SerializeField] private GameObject connectionScreen;

    // Reference to the UI GameObject shown after successful connection
    [SerializeField] private GameObject messagingScreen;

    /// <summary>
    /// Called once at startup. Subscribes to WebSocket and WebRTC events.
    /// </summary>
    private void Start()
    {
        // Check if WebSocketClient exists, then subscribe to its connection events
        if (WebSocketClient.Singleton == null) return;
        WebSocketClient.Singleton.OnConnected += HandleConnected;
        WebSocketClient.Singleton.OnDisconnected += HandleDisconnected;

        // Check if WebRtcClientManager exists, then subscribe to its ICE connection events
        if (WebRtcClientManager.Singleton == null) return;
        WebRtcClientManager.Singleton.OnIceConnected += HandleConnected;
        WebRtcClientManager.Singleton.OnIceDisconnected += HandleDisconnected;
    }

    /// <summary>
    /// Called when the object is destroyed. Unsubscribes from all events to avoid memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        if (WebSocketClient.Singleton == null) return;
        WebSocketClient.Singleton.OnConnected -= HandleConnected;
        WebSocketClient.Singleton.OnDisconnected -= HandleDisconnected;

        if (WebRtcClientManager.Singleton == null) return;
        WebRtcClientManager.Singleton.OnIceConnected -= HandleConnected;
        WebRtcClientManager.Singleton.OnIceDisconnected -= HandleDisconnected;
    }

    /// <summary>
    /// Called when either WebSocket or WebRTC connection is established.
    /// Updates the UI to show the messaging screen.
    /// </summary>
    private void HandleConnected()
    {
        connectionScreen.SetActive(false);  // Hide the connection UI
        messagingScreen.SetActive(true);    // Show the messaging/chat UI
    }

    /// <summary>
    /// Called when either WebSocket or WebRTC connection is lost.
    /// Reverts the UI to show the connection screen again.
    /// </summary>
    private void HandleDisconnected()
    {
        connectionScreen.SetActive(true);   // Show the connection UI again
        messagingScreen.SetActive(false);   // Hide the messaging/chat UI
    }
}
