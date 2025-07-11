using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class SendButtonHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField messageInput;

    public void SendMessageButtonClicked()
    {
        Debug.Log("Send message button clicked");
        WebRtcClientManager.Singleton.SendMessageBuffered(!string.IsNullOrEmpty(messageInput.text)
            ? $"Message from Client: {messageInput.text}"
            : "Empty message from Client");
    }
}