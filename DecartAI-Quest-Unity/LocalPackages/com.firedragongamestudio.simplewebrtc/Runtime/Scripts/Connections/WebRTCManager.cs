// Always use local NativeWebSocket package from LocalPackages/
using NativeWebSocket;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleWebRTC {
    public class WebRTCManager {
        // outboundInitMessage class removed - not needed for new API
        [Serializable]
        public class outboundOfferMessage {
            public string type;
            public string sdp;
        }
        [Serializable]
        public class inboundAnswerMessage {
            public string type;
            public string sdp;
        }
        [Serializable]
        public class inboundReadyMessage {
            public string type;
            public string connection_id;
            public string session_id;
        }

        [Serializable]
        public class ICECandidateData {
            public string candidate;
            public string sdpMid;
            public int sdpMLineIndex;
        }

        [Serializable]
        public class outboundICECandidateMessage {
            public string type;
            public ICECandidateData candidate;
        }

        [Serializable]
        public class outboundPromptSendMessage {
              public string type;
              public string prompt;
              public bool should_enrich;
        }

        [Serializable]
        public class inboundIceCandidateMessage {
            public string type;
        }
        public event Action<WebSocketState> OnWebSocketConnection;
        public event Action OnWebRTCConnection;
        public event Action OnVideoStreamEstablished;

        public bool IsWebSocketConnected { get; private set; }
        public bool IsWebSocketConnectionInProgress { get; private set; }
        public Texture ImmersiveVideoTexture { get; private set; }

        public RawImage VideoReceiver;

        public WebSocket ws;
        private bool isLocalPeerVideoAudioSender;
        private bool isLocalPeerVideoAudioReceiver;

        public RTCPeerConnection pc;


        private readonly Dictionary<string, RTCRtpSender> videoTrackSenders = new Dictionary<string, RTCRtpSender>();
        public event Action<string> OnPromptSent;

        private readonly string localPeerId;
        private readonly string stunServerAddress;
        private readonly WebRTCConnection connectionGameObject;
        private string currentWebSocketUrl;

        private static string sessionId = Guid.NewGuid().ToString();
//        private static string sessionId = "00000000-0000-0000-0000-000000000000";

        private bool isUsingLucyModel = false;

        private static Dictionary<string, string> miragePrompts = new Dictionary<string, string>() {
              {"Frozen World", "Frozen World World"},
              {"Versailles Palace", "Versailles Palace World"},
              {"Minecraft", "Minecraft World"},
              {"Lego", "Lego World"},
              {"California", "California World"},
              {"Magical Fantasy", "Magical Fantasy World"},
              {"Barbie", "Barbie World"},
              {"Cyberpunk", "Cyberpunk World"},
              {"Yarn", "Yarn World"},
              {"Ultra Realistic", "Ultra Realistic World"},
              {"Christmas", "Christmas World"},
              {"Claymation", "Claymation World"},
              {"Luxury", "Luxury World"},
              {"Glass", "Glass World"},
              {"Future Underground", "Future Underground World"},
              {"Colored Manga", "Colored Manga World"},
              {"Purple Nightlife", "Purple Nightlife World"},
              {"Fairy Lights", "Fairy Lights World"},
              {"Home Study", "Home Study World"},
              {"Space Pyramid", "Space Pyramid World"},
              {"Dollhouse", "Dollhouse World"},
              {"Office", "Office World"},
              {"Comic Book", "Comic Book World"},
              {"Purple Palace", "Purple Palace World"},
              {"Japan", "Japan World"},
              {"Spaceship", "Spaceship World"},
              {"Avatar", "Avatar World"},
              {"Zombie", "Zombie World"},
              {"Golden Dawn", "Golden Dawn World"},
              {"Wood", "Wood World"},
              {"Play-Doh", "Play-Doh World"},
              {"Wizards", "Wizards World"},
              {"Relaxing Anime", "Relaxing Anime World"},
              {"Hell", "Hell World"},
              {"Heaven", "Heaven World"},
              {"Hospital", "Hospital World"},
              {"Muted 2D", "Muted 2D World"},
              {"China", "China World"},
              {"Medieval ", "Medieval  World"},
              {"Beach", "Beach World"},
              {"Anime Log-Cabin", "Anime Log-Cabin World"},
              {"Jungle War", "Jungle War World"},
              {"Vikings", "Vikings World"},
              {"Run Down Office", "Run Down Office World"},
              {"Snowfall", "Snowfall World"},
              {"Anime", "Anime World"},
              {"Semi-Realistic", "Semi-Realistic World"},
              {"Sakura", "Sakura World"},
              {"Cigar Lounge", "Cigar Lounge World"},
              {"Forest", "Forest World"},
              {"Moon", "Moon World"},
              {"Diwali", "Diwali World"},
              {"Manga", "Manga World"},
              {"Galactic Wars", "Galactic Wars World"},
              {"Tropical Paradise", "Tropical Paradise World"},
              {"Portal", "Portal World"},
              {"Music Festival", "Music Festival World"},
              {"Egyptian Pyramids", "Egyptian Pyramids World"},
              {"S'mores", "S'mores World"},
              {"K-POP", "K-POP World"},
              {"Dubai Skyline", "Dubai Skyline World"},
        };

        private static Dictionary<string, string> lucyPrompts = new Dictionary<string, string>() {
              {"Change to Spiderman", "Transform the man to SpiderMan"},
              {"Add a Parrot on the Shoulder", "Add a parrot with bright green feathers sitting on the person's shoulder, tilting its head curiously."},
              {"Change the Hair to Platinum Blonde", "Change the hair color to icy platinum blonde, shimmering with metallic sheen."},
              {"Dress with a Medieval Knight Armor", "Change the uniform to a medieval knight's armor with metallic reflections and engraved details."},
              {"Add a Leather Biker Jacket", "Change the jacket to a leather biker jacket with silver zippers and worn textures, making it look rugged and rebellious under a cloudy sky."},
              {"Transform into a Body Builder", "Transform the human into a bodybuilder."},
              {"Add a Tuxedo", "Change the shirt to a tuxedo."},
              {"Add a Wedding Dress", "change the outfit to a white wedding dress."},
              {"Change to an Elegant Dress", "Dress the person in an elegant black evening dress"},
              {"Transform Everyone to Polar bear", "Transform the person into a cute polar bear."},
              {"Change to a Lizard Man", "make me a lizard man"},
              {"Make everyone be an Anime Character", "Transform the person into a 2D anime character."},
              {"Add a Chef Uniform", "Dress the person in a white chef uniform with hat"},
              {"Make me an Alien", "Transform the person into an alien."},
              {"Add a Summer Dress", "Dress the person in a light floral summer dress"},
        };

        private static int promptIndex = 0;

        public WebRTCManager(string localPeerId, string stunServerAddress, WebRTCConnection connectionObject) {
            this.localPeerId = localPeerId;
            this.stunServerAddress = stunServerAddress;
            this.connectionGameObject = connectionObject;
        }

        public async void Connect(string webSocketUrl, bool isVideoAudioSender, bool isVideoAudioReceiver) {

            IsWebSocketConnectionInProgress = true;
            isLocalPeerVideoAudioSender = isVideoAudioSender;
            isLocalPeerVideoAudioReceiver = isVideoAudioReceiver;


            if(ws != null){
                await ws.Close();
            }
            ws = new WebSocket(webSocketUrl);
            currentWebSocketUrl = webSocketUrl;  // Store for logging
            ws.OnOpen += () => {
                SimpleWebRTCLogger.Log("WebSocket connection opened!");

                IsWebSocketConnected = true;
                IsWebSocketConnectionInProgress = false;

                OnWebSocketConnection?.Invoke(WebSocketState.Open);

                // No initialization message needed for new API - removed initialize_session
                // New API format matches API-Example.html: connect then immediately start WebRTC offer/answer
                try{
                    if(ws != null){
                        CreateNewPeerVideoAudioReceivingResources(sessionId);
                        SetupPeerConnection();
                        SimpleWebRTCLogger.Log($"NEWPEER: Created new peerconnection {localPeerId} on peer {localPeerId}");
                    }
                    else{
                        SimpleWebRTCLogger.LogError("WebSocket is null");
                    }
                }
                catch(Exception e){
                    SimpleWebRTCLogger.LogError("Error in connection setup: " + e.Message);
                }
            };

                ws.OnMessage += HandleMessage;
                ws.OnError += (e) => SimpleWebRTCLogger.LogError("Error! " + e);
                ws.OnClose += (e) => {
                    SimpleWebRTCLogger.Log("WebSocket connection closed!");
                    IsWebSocketConnected = false;
                    IsWebSocketConnectionInProgress = false;
                    OnWebSocketConnection?.Invoke(WebSocketState.Closed);
                };


//              important for video transmission, to restart webrtc update coroutine removed for now
              connectionGameObject.StopWebRTCUpdateCoroutine();
              connectionGameObject.StartWebRTUpdateCoroutine();

                await ws.Connect();
            }




        private void SetupPeerConnection() {
            pc = CreateNewRTCPeerConnection();
            SetupEventHandlers();
        }

        private RTCPeerConnection CreateNewRTCPeerConnection() {
            if (string.IsNullOrEmpty(stunServerAddress)) {
                return new RTCPeerConnection();
            }

            RTCConfiguration config = new RTCConfiguration {
                iceServers = new[] {
                    new RTCIceServer { urls = new[] { stunServerAddress } }
                },
                // Add low-latency settings - using available Unity WebRTC properties
                iceCandidatePoolSize = 10
            };
            return new RTCPeerConnection(ref config);
        }

        private void SetupEventHandlers() {
            pc.OnIceCandidate = candidate => {
                // New API format: match API-Example.html ICE candidate structure
                var candidateMessage = new outboundICECandidateMessage {
                    type = "ice-candidate",
                    candidate = new ICECandidateData {
                        candidate = candidate.Candidate,
                        sdpMid = candidate.SdpMid,
                        sdpMLineIndex = candidate.SdpMLineIndex ?? 0
                    }
                };
                ws.SendText(JsonUtility.ToJson(candidateMessage));
            };

            pc.OnIceConnectionChange = state => {
                SimpleWebRTCLogger.Log($"{localPeerId} connection changed to {state}");

                switch(state) {
                    case RTCIceConnectionState.Completed:
                        connectionGameObject.Connect();
                        OnWebRTCConnection?.Invoke();
                        connectionGameObject.ConnectWebRTC();
                        break;
                    case RTCIceConnectionState.Failed:
                        SimpleWebRTCLogger.LogError("ICE connection failed");
                        break;
                    case RTCIceConnectionState.Disconnected:
                        SimpleWebRTCLogger.LogError("ICE connection disconnected");
                        break;
                }
            };


            pc.OnTrack = e => {
                if (e.Track is VideoStreamTrack video) {
                    OnVideoStreamEstablished?.Invoke();

                    if (connectionGameObject.IsImmersiveSetupActive) {
                        // Drop frames if needed for immediate display - only update if new frame
                        video.OnVideoReceived += tex => {
                            if (ImmersiveVideoTexture != tex) {
                                ImmersiveVideoTexture = tex;
                            }
                        };
                    } else {
                        // Drop frames if needed for immediate display - only update if new frame
                        video.OnVideoReceived += tex => {
                            if (VideoReceiver != null && VideoReceiver.texture != tex) {
                                VideoReceiver.texture = tex;
                            }
                        };
                    }

                    SimpleWebRTCLogger.Log("Receiving video stream with frame dropping for low latency.");
                }
            };

            // not needed, because negotiation is done manually
            // rly? --- we need this, come back later
//            peerConnections[peerId].OnNegotiationNeeded = () => {
//                if (peerConnections.ContainsKey(peerId) && peerConnections[peerId].SignalingState != RTCSignalingState.Stable) {
//                    connectionGameObject.CreateOfferCoroutine();
//                }
//            };
        }

        // Local NativeWebSocket uses this signature
        private void HandleMessage(byte[] bytes) {
            HandleMessageInternal(bytes);
        }

        public void SendCustomPrompt(string customPrompt) {
            var promptMessage = new outboundPromptSendMessage {
                        type = "prompt",
                        prompt = customPrompt,
                        should_enrich = true
                    };
            ws.SendText(JsonUtility.ToJson(promptMessage));
            OnPromptSent?.Invoke(customPrompt);
        }

        public void SetModelType(bool isLucy) {
            isUsingLucyModel = isLucy;
            promptIndex = 0; // Reset to first prompt when switching models
        }

        public void SendNextPrompt(bool forward = true) {
            // Select the correct prompt dictionary based on model type
            var activePrompts = isUsingLucyModel ? lucyPrompts : miragePrompts;

            promptIndex = (promptIndex + (forward ? 1 : -1) + activePrompts.Count) % activePrompts.Count;
            var promptKey = activePrompts.ElementAt(promptIndex).Key;
            var promptValue = activePrompts[promptKey];

            var promptMessage = new outboundPromptSendMessage {
                        type = "prompt",
                        prompt = promptValue,
                        should_enrich = true
                    };
            ws.SendText(JsonUtility.ToJson(promptMessage));
            OnPromptSent?.Invoke(promptKey);
        }

        private void HandleMessageInternal(byte[] bytes, int offset = 0, int length = 0) {
            if (length == 0) length = bytes.Length - offset; // fallback if length is not specified
            var data = Encoding.UTF8.GetString(bytes, offset, length);

            var readyMessage = JsonUtility.FromJson<inboundReadyMessage>(data);
            if(readyMessage.type == "ready"){
                // Ready message received, offer will be sent
            }

            var answerMessage = JsonUtility.FromJson<inboundAnswerMessage>(data);
            if(answerMessage.type == "answer"){
                RTCSessionDescription answerSessionDesc = new RTCSessionDescription() {
                    type = RTCSdpType.Answer,
                    sdp = answerMessage.sdp
                };
                pc.SetRemoteDescription(ref answerSessionDesc);
            }

            // Handle ICE candidate messages manually due to Unity JsonUtility limitations with nested objects
            var iceCandidateMessage = JsonUtility.FromJson<inboundIceCandidateMessage>(data);
            if(iceCandidateMessage.type == "ice-candidate"){
                SimpleWebRTCLogger.Log($"Received ICE candidate message: {data}");
                
                try {
                    // Manual parsing of nested candidate object
                    int candidateObjStart = data.IndexOf("\"candidate\": {");
                    if (candidateObjStart == -1) {
                        candidateObjStart = data.IndexOf("\"candidate\":{");
                    }
                    
                    if (candidateObjStart != -1) {
                        // Find the opening brace of the candidate object
                        int braceStart = data.IndexOf('{', candidateObjStart);
                        int braceEnd = FindMatchingBrace(data, braceStart);
                        
                        if (braceStart != -1 && braceEnd != -1) {
                            string candidateJson = data.Substring(braceStart + 1, braceEnd - braceStart - 1);
                            SimpleWebRTCLogger.Log($"Parsing candidate JSON: {candidateJson}");
                            
                            // Extract candidate string
                            string candidateStr = ExtractJsonValue(candidateJson, "candidate");
                            string sdpMid = ExtractJsonValue(candidateJson, "sdpMid");
                            string sdpMLineIndexStr = ExtractJsonValue(candidateJson, "sdpMLineIndex");
                            
                            SimpleWebRTCLogger.Log($"Extracted values: candidate='{candidateStr}', sdpMid='{sdpMid}', sdpMLineIndex='{sdpMLineIndexStr}'");
                            
                            if (!string.IsNullOrEmpty(candidateStr) && !string.IsNullOrEmpty(sdpMid) && int.TryParse(sdpMLineIndexStr, out int sdpMLineIndex)) {
                                SimpleWebRTCLogger.Log($"Adding remote ICE candidate: sdpMid={sdpMid}, sdpMLineIndex={sdpMLineIndex}, candidate={candidateStr}");
                                
                                RTCIceCandidateInit candidateInit = new RTCIceCandidateInit() {
                                    sdpMid = sdpMid,
                                    sdpMLineIndex = sdpMLineIndex,
                                    candidate = candidateStr
                                };
                                var candidate = new RTCIceCandidate(candidateInit);
                                pc.AddIceCandidate(candidate);
                                SimpleWebRTCLogger.Log("Successfully added remote ICE candidate");
                            } else {
                                SimpleWebRTCLogger.Log("Received ICE candidate gathering end signal or invalid candidate data");
                            }
                        } else {
                            SimpleWebRTCLogger.LogError("Failed to find candidate object braces");
                        }
                    } else {
                        SimpleWebRTCLogger.LogError("Failed to find candidate object in JSON");
                    }
                } catch (System.Exception e) {
                    SimpleWebRTCLogger.LogError($"Error processing ICE candidate: {e.Message}");
                }
            }
        }

        private void CreateNewPeerVideoAudioReceivingResources(string senderPeerId) {
            if (!connectionGameObject.IsImmersiveSetupActive) {
                // create new video receiver gameobject
                connectionGameObject.CreateVideoReceiverGameObject(senderPeerId);
            }
        }


        private void HandleAnswer(string senderPeerId, string answerJson) {

            SimpleWebRTCLogger.Log($"{localPeerId} got ANSWER from {senderPeerId} : {answerJson}");

            var receivedAnswerSessionDesc = SessionDescription.FromJSON(answerJson);
            RTCSessionDescription answerSessionDesc = new RTCSessionDescription() {
                type = RTCSdpType.Answer,
                sdp = receivedAnswerSessionDesc.sdp
            };
            pc.SetRemoteDescription(ref answerSessionDesc);
        }

        private void HandleCandidate(string senderPeerId, string candidateJson) {

            SimpleWebRTCLogger.Log($"{localPeerId} got CANDIDATE from {senderPeerId} : {candidateJson}");

            var candidateInit = CandidateInit.FromJSON(candidateJson);

            if (string.IsNullOrEmpty(candidateInit.candidate)) {
                SimpleWebRTCLogger.Log($"{localPeerId} got CANDIDATE GATHERING END from {senderPeerId}.");
                return;
            }

            SimpleWebRTCLogger.Log($"Adding remote ICE candidate from {senderPeerId}: {candidateInit.candidate}");
            RTCIceCandidateInit init = new RTCIceCandidateInit() {
                sdpMid = candidateInit.sdpMid,
                sdpMLineIndex = candidateInit.sdpMLineIndex,
                candidate = candidateInit.candidate
            };
            var candidate = new RTCIceCandidate(init);
            pc.AddIceCandidate(candidate);
            SimpleWebRTCLogger.Log($"Successfully added remote ICE candidate from {senderPeerId}");
        }

        public void CloseWebRTC() {


            foreach (var videoTrackSender in videoTrackSenders) {
                videoTrackSender.Value.Dispose();
            }

            pc.Close();



            videoTrackSenders.Clear();

        }

        public async void CloseWebSocket() {
            if (ws != null) {
                await ws.Close();

                // reset manually, because ws is not reusable after closing
                ws = null;
            }
        }

        // Helper method to find matching closing brace
        private int FindMatchingBrace(string json, int startIndex) {
            if (startIndex >= json.Length || json[startIndex] != '{') return -1;
            
            int braceCount = 1;
            int index = startIndex + 1;
            
            while (index < json.Length && braceCount > 0) {
                if (json[index] == '{') {
                    braceCount++;
                } else if (json[index] == '}') {
                    braceCount--;
                }
                index++;
            }
            
            return braceCount == 0 ? index - 1 : -1;
        }

        // Helper method to extract JSON values manually
        private string ExtractJsonValue(string json, string key) {
            try {
                string searchKey = $"\"{key}\":";
                int keyIndex = json.IndexOf(searchKey);
                if (keyIndex == -1) {
                    // Try without space
                    searchKey = $"\"{key}\":";
                    keyIndex = json.IndexOf(searchKey);
                    if (keyIndex == -1) return null;
                }

                int valueStart = keyIndex + searchKey.Length;
                
                // Skip whitespace
                while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart])) {
                    valueStart++;
                }
                
                if (valueStart >= json.Length) return null;

                // Handle string values (wrapped in quotes)
                if (json[valueStart] == '"') {
                    valueStart++; // Skip opening quote
                    int valueEnd = json.IndexOf('"', valueStart);
                    if (valueEnd == -1) return null;
                    return json.Substring(valueStart, valueEnd - valueStart);
                }
                // Handle numeric values
                else {
                    int valueEnd = valueStart;
                    while (valueEnd < json.Length && (char.IsDigit(json[valueEnd]) || json[valueEnd] == '-')) {
                        valueEnd++;
                    }
                    if (valueEnd == valueStart) return null;
                    return json.Substring(valueStart, valueEnd - valueStart);
                }
            } catch (System.Exception e) {
                SimpleWebRTCLogger.LogError($"Error extracting JSON value for key '{key}': {e.Message}");
                return null;
            }
        }
//
//        public void InstantiateWebRTC() {
//            connectionGameObject.CreateOfferCoroutine();
//        }

        // Always available for local NativeWebSocket
        public void DispatchMessageQueue() {
            ws?.DispatchMessageQueue();
        }

        public void AddVideoTrack(VideoStreamTrack videoStreamTrack) {
            
            // optional video stream preview
            if (connectionGameObject.OptionalPreviewRawImage != null) {
                connectionGameObject.OptionalPreviewRawImage.texture = videoStreamTrack.Texture;
            }

            videoTrackSenders[sessionId] = pc.AddTrack(videoStreamTrack);
//            connectionGameObject.CreateOfferCoroutine();
        }

        public void RemoveVideoTrack() {
        }




    }
}