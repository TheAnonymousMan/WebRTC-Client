using TMPro; // Importing TextMeshPro namespace for UI text input handling
using UnityEngine; // Unity's core namespace for MonoBehaviour and other game development utilities

/// <summary>
/// Handles the Connect button click logic, including reading the input IP address,
/// initiating the WebSocket connection, and starting the WebRTC connection process.
/// </summary>
public class ConnectButtonHandler : MonoBehaviour
{
    // Reference to the TMP_InputField used for entering the server IP address.
    // This should be assigned in the Unity Inspector.
    [SerializeField] private TMP_InputField ipInput;

    /// <summary>
    /// Called when the Connect button is clicked.
    /// Initiates WebSocket and WebRTC connections using the entered IP address.
    /// </summary>
    public void ConnectButtonClicked()
    {
        // Log the IP address input (for debugging purposes).
        Debug.Log($"Connect button clicked. IP Address: {ipInput.text}");

        // Fallback to "localhost" if the input field is empty or null.
        string ip = string.IsNullOrEmpty(ipInput.text) ? "localhost" : ipInput.text;

        // Connect to the WebSocket signaling server using the provided IP and port 8080.
        WebSocketClient.Singleton.Connect($"{ip}:8080");

        // Start the coroutine that handles WebRTC peer connection setup.
        // This coroutine likely handles offer/answer exchange and ICE candidate processing.
        WebRtcClientManager.Singleton.StartCoroutine(WebRtcClientManager.Singleton.Connect());
    }
}
