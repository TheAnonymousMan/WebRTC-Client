using System;
using Newtonsoft.Json;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;

public class WebSocketClient : MonoBehaviour
{
    public WebSocket ActiveWebSocket;
    public static WebSocketClient Singleton { get; private set; }
    public event Action OnConnected;
    public event Action OnDisconnected;

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

    public void Connect(string ip = "localhost:8080")
    {
        Debug.Log($"[WebSocketClient] Connecting to server {ip}");
        ActiveWebSocket = new WebSocket($"ws://{ip}/ws");

        ActiveWebSocket.OnOpen += (sender, e) =>
        {
            OnConnected?.Invoke();
            Debug.Log("[WebSocketClient] Connected to server.");
        };
        ActiveWebSocket.OnMessage += (sender, e) =>
        {
            Debug.Log($"[WebSocketClient] Message received: {e.Data}");

            var message = JsonConvert.DeserializeObject<WebRtcMessage>(e.Data);

            switch (message.Type)
            {
                case "answer":
                    Debug.Log("[WebSocketClient] Answer received");
                    MainThreadDispatcher.Enqueue(
                        WebRtcClientManager.Singleton.OnAnswerReceived(message)
                    );
                    break;
                case "candidate":
                    Debug.Log("[WebSocketClient] Candidate received");
                    WebRtcClientManager.Singleton.OnIceCandidateReceived(message);
                    break;
            }
        };
        ActiveWebSocket.OnClose += (sender, e) =>
        {
            OnDisconnected?.Invoke();
            Debug.Log($"[WebSocketClient] Disconnected from server {sender}.");
        };

        ActiveWebSocket.Connect();
    }

    private void OnApplicationQuit()
    {
        ActiveWebSocket?.Close();
    }
}