using UnityEngine;

public class UIHandler : MonoBehaviour
{
    [SerializeField] private GameObject connectionScreen;
    [SerializeField] private GameObject messagingScreen;

    private void Start()
    {
        if (WebSocketClient.Singleton == null) return;
        WebSocketClient.Singleton.OnConnected += HandleConnected;
        WebSocketClient.Singleton.OnDisconnected += HandleDisconnected;

        if (WebRtcClientManager.Singleton == null) return;
        WebRtcClientManager.Singleton.OnIceConnected += HandleConnected;
        WebRtcClientManager.Singleton.OnIceDisconnected += HandleDisconnected;
    }

    private void OnDestroy()
    {
        if (WebSocketClient.Singleton == null) return;
        WebSocketClient.Singleton.OnConnected -= HandleConnected;
        WebSocketClient.Singleton.OnDisconnected -= HandleDisconnected;

        if (WebRtcClientManager.Singleton == null) return;
        WebRtcClientManager.Singleton.OnIceConnected -= HandleConnected;
        WebRtcClientManager.Singleton.OnIceDisconnected -= HandleDisconnected;
    }

    private void HandleConnected()
    {
        connectionScreen.SetActive(false);
        messagingScreen.SetActive(true);
    }

    private void HandleDisconnected()
    {
        connectionScreen.SetActive(true);
        messagingScreen.SetActive(false);
    }
}