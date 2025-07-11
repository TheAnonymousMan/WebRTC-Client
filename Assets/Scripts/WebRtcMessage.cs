using Newtonsoft.Json;

public class WebRtcMessage
{
    [JsonProperty("type")] public string Type;
    [JsonProperty("sdp")] public string Sdp;
    [JsonProperty("candidate")] public string Candidate;
    [JsonProperty("sdpMid")] public string SdpMid;
    [JsonProperty("sdpMLineIndex")] public int SdpMLineIndex;
    [JsonProperty("data")] public string Data;
}
