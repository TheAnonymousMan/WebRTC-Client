using TMPro; // For TMP_InputField, used to get text input from the UI
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles logic for the Send Message button in the UI.
/// Sends the user's typed message over the WebRTC data channel using WebRtcClientManager.
/// </summary>
public class SendButtonHandler : MonoBehaviour
{
    // Reference to the UI input field where the user types their message.
    // This field should be set in the Unity Inspector.
    [SerializeField] private TMP_InputField messageInput;

    /// <summary>
    /// Called when the Send button is clicked.
    /// Sends the user's message using WebRTC's data channel.
    /// </summary>
    public void SendMessageButtonClicked()
    {
        // Log for debugging that the send button was clicked.
        Debug.Log("Send message button clicked");

        // Check if the input field is not empty and prepare the message to send.
        string messageToSend = !string.IsNullOrEmpty(messageInput.text)
            ? $"Message from Client: {messageInput.text}"   // Use the user's input
            : "Empty message from Client";                  // Fallback if input is empty

        // Send the message using a buffered WebRTC data channel method.
        // Buffered means if the channel isn't open yet, the message will be queued.
        WebRtcClientManager.Singleton.SendMessageBuffered(messageToSend);
    }
}
