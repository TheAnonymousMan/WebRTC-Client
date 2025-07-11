using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;

public class WebRtcVideoPlayer : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;

    private VideoStreamTrack _videoTrack;
    private MediaStream _receiveStream;

    private void Awake()
    {
        if (rawImage == null)
            rawImage = GetComponent<RawImage>();
    }

    /// <summary>
    /// Assign the incoming video track and begin rendering.
    /// </summary>
    /// <param name="track">The remote VideoStreamTrack</param>
    public void SetTrack(VideoStreamTrack track)
    {
        if (_videoTrack != null)
        {
            _videoTrack.OnVideoReceived -= OnFrameReceived;
            _videoTrack.Dispose();
        }

        _videoTrack = track;

        if (_videoTrack != null)
        {
            _videoTrack.OnVideoReceived += OnFrameReceived;
        }
    }

    private void OnFrameReceived(Texture tex)
    {
        if (tex != null)
            rawImage.texture = tex;
        Debug.Log("New frame received");
    }

    /// <summary>
    /// Clean up resources when replacing track or destroying object.
    /// </summary>
    private void Cleanup()
    {
        if (_videoTrack != null)
        {
            _videoTrack.OnVideoReceived -= OnFrameReceived;
            _videoTrack.Dispose();
            _videoTrack = null;
        }

        rawImage.texture = null;
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}