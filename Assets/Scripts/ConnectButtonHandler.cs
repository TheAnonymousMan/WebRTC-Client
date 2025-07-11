using TMPro;
using UnityEngine;

public class ConnectButtonHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField ipInput;

    public void ConnectButtonClicked()
    {
        Debug.Log($"Connect button clicked. IP Address: {ipInput.text}");
        string ip = string.IsNullOrEmpty(ipInput.text) ? "localhost" : ipInput.text;
        WebSocketClient.Singleton.Connect($"{ip}:8080");
        WebRtcClientManager.Singleton.StartCoroutine(WebRtcClientManager.Singleton.Connect());
    }
}