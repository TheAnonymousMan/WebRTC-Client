using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;

/// <summary>
/// Handles displaying incoming remote video stream using Unity's RawImage UI component.
/// Connects to a remote WebRTC VideoStreamTrack and updates the texture on new frames.
/// </summary>
public class WebRtcVideoPlayer : MonoBehaviour
{
    // Reference to the RawImage UI element where the video will be rendered.
    [SerializeField] private RawImage rawImage;

    // Stores the current VideoStreamTrack being rendered.
    private VideoStreamTrack _videoTrack;

    // (Optional) If you later want to manage the full MediaStream, you can use this.
    private MediaStream _receiveStream;

    /// <summary>
    /// Ensures the rawImage reference is set during object initialization.
    /// </summary>
    private void Awake()
    {
        // If not assigned in the Inspector, try to find a RawImage on this GameObject
        if (rawImage == null)
            rawImage = GetComponent<RawImage>();
    }

    /// <summary>
    /// Assigns the incoming WebRTC video track and starts listening for video frame updates.
    /// Replaces any existing video track.
    /// </summary>
    /// <param name="track">The remote VideoStreamTrack received over WebRTC</param>
    public void SetTrack(VideoStreamTrack track)
    {
        // Clean up the previous track if one exists
        if (_videoTrack != null)
        {
            _videoTrack.OnVideoReceived -= OnFrameReceived; // Unsubscribe from frame event
            _videoTrack.Dispose(); // Release native resources
        }

        // Assign the new video track
        _videoTrack = track;

        // Subscribe to frame events if the new track is valid
        if (_videoTrack != null)
        {
            _videoTrack.OnVideoReceived += OnFrameReceived;
        }
    }

    /// <summary>
    /// Called every time a new video frame is received from the remote track.
    /// Updates the RawImage texture with the new frame.
    /// </summary>
    /// <param name="tex">The new texture/frame received from the remote peer</param>
    private void OnFrameReceived(Texture tex)
    {
        if (tex != null)
            rawImage.texture = tex; // Update the UI texture with the new video frame

        Debug.Log("New frame received");
    }

    /// <summary>
    /// Cleans up the video track and UI texture to prevent memory leaks and dangling references.
    /// Called when the object is destroyed or when a new track replaces the current one.
    /// </summary>
    private void Cleanup()
    {
        if (_videoTrack != null)
        {
            _videoTrack.OnVideoReceived -= OnFrameReceived; // Unsubscribe from event
            _videoTrack.Dispose(); // Dispose the track to release native resources
            _videoTrack = null;
        }

        rawImage.texture = null; // Clear the texture on screen
    }

    /// <summary>
    /// Unity lifecycle method called when the GameObject is being destroyed.
    /// Ensures that resources are released properly.
    /// </summary>
    private void OnDestroy()
    {
        Cleanup();
    }
}
