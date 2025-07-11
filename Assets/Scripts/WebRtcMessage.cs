using Newtonsoft.Json;

/// <summary>
/// Represents a signaling message used to exchange SDP offers/answers and ICE candidates
/// between WebRTC peers over a signaling channel (typically WebSocket).
/// </summary>
public class WebRtcMessage
{
    /// <summary>
    /// Type of the message (e.g., "offer", "answer", "candidate", or custom types like "data").
    /// Used to determine how to handle the message on the receiving side.
    /// </summary>
    [JsonProperty("type")]
    public string Type;

    /// <summary>
    /// Session Description Protocol string (used for "offer" and "answer" messages).
    /// Describes the media format and connection details.
    /// </summary>
    [JsonProperty("sdp")]
    public string Sdp;

    /// <summary>
    /// ICE candidate string (used for "candidate" messages).
    /// Contains connection candidate information like IP, port, and transport protocol.
    /// </summary>
    [JsonProperty("candidate")]
    public string Candidate;

    /// <summary>
    /// The MID (media stream identification) of the media section this candidate is associated with.
    /// Part of the ICE candidate message.
    /// </summary>
    [JsonProperty("sdpMid")]
    public string SdpMid;

    /// <summary>
    /// The m-line index (media line index) in the SDP this candidate is associated with.
    /// </summary>
    [JsonProperty("sdpMLineIndex")]
    public int SdpMLineIndex;

    /// <summary>
    /// Optional field for custom data messages (e.g., chat messages or JSON payloads over data channel).
    /// Not used in standard SDP/ICE signaling but helpful for extended signaling.
    /// </summary>
    [JsonProperty("data")]
    public string Data;
}
