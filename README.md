# Unity WebRTC Streaming Client (with Integrated WebSocket Signaling)

This Unity project implements a **fully in-Unity** WebRTC client capable of receiving live video streams and messages over a peer connection. It includes:

* WebRTC offer-answer and ICE negotiation
* WebSocket-based signaling (no Node.js needed)
* Remote video display
* Buffered data channel messaging
* Support for Google Cardboard VR rendering
* Clean UI state transitions

---

## Project Structure

| Component              | Description                                                                |
| ---------------------- | -------------------------------------------------------------------------- |
| `WebRtcClientManager`  | Handles all WebRTC connection logic (peer, SDP, ICE, data channel, tracks) |
| `WebSocketClient`      | Connects to the Unity-hosted WebSocket signaling server                    |
| `MainThreadDispatcher` | Ensures coroutines and Unity APIs run on the main thread                   |
| `WebRtcVideoPlayer`    | Displays incoming remote video using Unity UI (RawImage)                   |
| `UIHandler`            | Switches between connection and messaging screens                          |
| `ConnectButtonHandler` | UI handler to initiate WebSocket and WebRTC connection                     |
| `SendButtonHandler`    | UI handler to send messages over WebRTC data channel                       |
| `WebRtcMessage`        | Serializable class used for all signaling messages                         |

---

## Features

* Full WebRTC offer/answer and ICE negotiation using Unity APIs
* Remote video rendering via `VideoStreamTrack` and `RawImage`
* Reliable messaging over data channels with message queuing
* Integrated WebSocket client using `WebSocketSharp`
* VR Compatibility: Works with **Google Cardboard** out of the box
* Unity-only deployment (no Node.js or browser dependency)

---

## Screens

| UI State              | Description                            |
| --------------------- | -------------------------------------- |
| **Connection Screen** | Input IP address & start connection    |
| **Messaging Screen**  | Shows video stream and send message UI |

---

## How to Run

### 1. Setup Unity Environment

* Unity version: **2022.3+ recommended**
* Install the [Unity WebRTC package](https://github.com/Unity-Technologies/com.unity.webrtc)
* Install [WebSocketSharp](https://github.com/sta/websocket-sharp) via DLL or source

### 2. Launch the Server Unity App

* Make sure the WebSocket signaling server (if embedded in Unity) is running first.

### 3. Run the Client App

* Enter the IP address of the server (or leave blank for `localhost`)
* Press **Connect**
* Once connected, you should receive a video stream and be able to send messages

---

## WebRTC Signaling Messages

```json
// Offer
{
  "type": "offer",
  "sdp": "...session description..."
}

// Answer
{
  "type": "answer",
  "sdp": "...session description..."
}

// ICE Candidate
{
  "type": "candidate",
  "candidate": "...",
  "sdpMid": "...",
  "sdpMLineIndex": 0
}

// Custom Message
{
  "type": "data",
  "data": "Chat or control message"
}
```

---

## Dependencies

* `com.unity.webrtc` (Unity WebRTC)
* `WebSocketSharp` (Signaling communication)
* `Newtonsoft.Json` (Serialization)

---

## Future Extensions

* Reconnect logic
* Multi-client/multi-stream support
* UI chat window with message history
* Local video preview
* Secure WebSocket (WSS)

---

## Notes

* Signaling server is tightly coupled and embedded within Unity—this avoids needing external server infrastructure.
* Main thread dispatching is crucial for Unity APIs (especially WebRTC coroutines and track updates).

---

## Author

Developed by **Souporno Ghosh** as part of a VR WebRTC streaming prototype for clinical or training applications. The codebase is designed to be extensible and easily portable to Android/VR devices.

---

## License

MIT License or custom license based on deployment needs.
