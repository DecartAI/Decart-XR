#if !USE_NATIVEWEBSOCKET
using Meta.Net.NativeWebSocket;
#else
using NativeWebSocket;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace SimpleWebRTC {
    public class WebRTCConnection : MonoBehaviour {


        [Serializable]
        public class outboundOfferMessage {
            public string type;
            public string sdp;
        }

        [Serializable]
        public class PromptSentEvent : UnityEvent<string> {}

        public PromptSentEvent PromptNameUpdated = new PromptSentEvent();

        public bool IsWebSocketConnected => webRTCManager.IsWebSocketConnected;
        public bool ConnectionToWebSocketInProgress => webRTCManager.IsWebSocketConnectionInProgress;

        public bool IsWebRTCActive { get; private set; }
        public bool IsVideoTransmissionActive { get; private set; }
        public bool IsAudioTransmissionActive { get; private set; }
        public bool IsImmersiveSetupActive => UseImmersiveSetup;
        public Camera VideoStreamingCamera => StreamingCamera;
        public bool IsSender => IsVideoAudioSender;
        public bool IsReceiver => IsVideoAudioReceiver;
        public bool ExperimentalSupportFor6DOF => experimentalSupportFor6DOF;
        public Transform ExperimentalSpectatorCam6DOF => experimentalSpectatorCam6DOF;

        private bool startWebRTCUpdate;
        private bool stopWebRTCUpdate;

        // Frame rate control for streaming
//        private float frameInterval = 1f / 12f; // 12 fps
//        private float lastFrameTime = 0f;

        // OPTIONAL: Startup optimization variables (commented to match API-Example.html simplicity)
        // private bool isInStartupPhase = false;
        // private float startupTimer = 0f;
        // private const float STARTUP_DURATION = 3f; // 3 seconds of startup optimization


        [Header("Connection Setup")]
        [SerializeField] private string MirageWebSocket = "wss://api3.decart.ai/v1/stream?api_key=G6_q2KFlxTk5UnVX82G_OlY5yMfjStO9a4eQqyV1Vec&model=mirage";
        [SerializeField] private string LucyWebSocket = "wss://api3.decart.ai/v1/stream?api_key=G6_q2KFlxTk5UnVX82G_OlY5yMfjStO9a4eQqyV1Vec&model=lucy_v2v_720p_rt";
        [SerializeField] private bool UseLucyModel = false;
        [SerializeField] private string StunServerAddress = "stun:stun.l.google.com:19302";
        [SerializeField] private string LocalPeerId = "PeerId";
        [SerializeField] private bool IsVideoAudioSender = true;
        [SerializeField] private bool IsVideoAudioReceiver = true;
        [SerializeField] private bool RandomUniquePeerId = true;
        [SerializeField] private bool ShowLogs = true;

        [Header("Immersive Setup")]
        [SerializeField] private bool UseImmersiveSetup = false;
        [SerializeField] private bool experimentalSupportFor6DOF = false;
        [SerializeField] private Transform experimentalSpectatorCam6DOF;
        [Header("Immersive Sender")]
        [SerializeField] private bool RenderStereo = false;
        [SerializeField] private float StereoSeparation = 0.064f;
        [SerializeField] private int OneEyeRenderSide = 1024;
        [SerializeField] private RenderTextureDepth RTDepth = RenderTextureDepth.Depth24;
        [SerializeField] private bool OneFacePerFrame = false;
        [Header("Immersive Receiver")]
        [SerializeField] private RenderTexture receivingRenderTexture;

        [Header("WebSocket Connection")]
        [SerializeField] private bool WebSocketConnectionActive;
        public UnityEvent<WebSocketState> WebSocketConnectionChanged;

        [Header("WebRTC Connection")]
        [SerializeField] private bool WebRTCConnectionActive = false;
        public UnityEvent WebRTCConnected;

        [Header("Video Transmission")]
        [SerializeField] private bool StartStopVideoTransmission = false;
        [SerializeField] private Vector2Int VideoResolution = new Vector2Int(1280, 720); // Matches API-Example.html (changed from 704)
        [SerializeField] private Camera StreamingCamera;
        public RawImage OptionalPreviewRawImage;
        [SerializeField] private RectTransform ReceivingRawImagesParent;
        [SerializeField] private Material streamMaterial;
        public UnityEvent VideoTransmissionReceived;

        private WebRTCManager webRTCManager;
        private VideoStreamTrack videoStreamTrack;
        private AudioStreamTrack audioStreamTrack;

        private RenderTexture cubemapLeftEye;
        private RenderTexture cubemapRightEye;
        private RenderTexture videoEquirect;

        private int faceToRender;
        private int faceMask = 63;

        // handle creation and destruction parts on monobehaviour
        private bool createVideoReceiver;
        private string videoReceiverSenderPeerId;
        private bool createAudioReceiver;
        private string audioReceiverSenderPeerId;

        private List<GameObject> tempDestroyGameObjectRefs = new List<GameObject>();

        private bool createOffer;


        private void Awake() {
            SimpleWebRTCLogger.EnableLogging = ShowLogs;

            webRTCManager = new WebRTCManager(LocalPeerId, StunServerAddress, this);

            // register events for webrtc connection
            webRTCManager.OnWebSocketConnection += WebSocketConnectionChanged.Invoke;
            webRTCManager.OnWebRTCConnection += WebRTCConnected.Invoke;
            webRTCManager.OnVideoStreamEstablished += VideoTransmissionReceived.Invoke;
            webRTCManager.OnPromptSent += PromptNameUpdated.Invoke;

            // setup immersive if selected, temp not in use
            if (UseImmersiveSetup) {
                if (IsVideoAudioSender) {
                    cubemapLeftEye = new RenderTexture(OneEyeRenderSide, OneEyeRenderSide, (int)RTDepth, RenderTextureFormat.BGRA32);
                    cubemapLeftEye.dimension = TextureDimension.Cube;
                    cubemapLeftEye.hideFlags = HideFlags.HideAndDontSave;
                    cubemapLeftEye.Create();

                    if (RenderStereo) {
                        cubemapRightEye = new RenderTexture(OneEyeRenderSide, OneEyeRenderSide, (int)RTDepth, RenderTextureFormat.BGRA32);
                        cubemapRightEye.dimension = TextureDimension.Cube;
                        cubemapRightEye.hideFlags = HideFlags.HideAndDontSave;
                        cubemapRightEye.Create();
                        //equirect height should be twice the height of cubemap if we render in stereo
                        videoEquirect = new RenderTexture(OneEyeRenderSide, OneEyeRenderSide * 2, (int)RTDepth, RenderTextureFormat.BGRA32);
                    } else {
                        videoEquirect = new RenderTexture(OneEyeRenderSide, OneEyeRenderSide, (int)RTDepth, RenderTextureFormat.BGRA32);
                    }
                    videoEquirect.hideFlags = HideFlags.HideAndDontSave;
                    videoEquirect.Create();
                }
            }
        }




        private void Update() {
            // Always dispatch for local NativeWebSocket
            webRTCManager.DispatchMessageQueue();

            if (SimpleWebRTCLogger.EnableLogging != ShowLogs) {
                SimpleWebRTCLogger.EnableLogging = ShowLogs;
            }

            CreateVideoReceiver();
            DestroyCachedGameObjects();
            StartWebRTCUpdate();
            StopWebRTCUpdate();

            // OPTIONAL: Startup optimization phase (commented to match API-Example.html simplicity)
            // if (isInStartupPhase && IsVideoTransmissionActive) {
            //     startupTimer += Time.deltaTime;
            //     if (startupTimer >= STARTUP_DURATION) {
            //         OptimizeEncodingAfterStartup();
            //         isInStartupPhase = false;
            //         SimpleWebRTCLogger.Log("DEBUG: Startup phase completed, optimized encoding parameters");
            //     }
            // }

            ConnectClient();

            if (!WebSocketConnectionActive && IsWebSocketConnected) {
                DisconnectClient();
            }

            if (!IsWebSocketConnected) {
                return;
            }



            if (WebRTCConnectionActive && !IsWebRTCActive) {
                SimpleWebRTCLogger.Log("DEBUG: Starting WebRTC connection");
                IsWebRTCActive = !IsWebRTCActive;
//                webRTCManager.InstantiateWebRTC();
                SimpleWebRTCLogger.Log("DEBUG: WebRTC instantiated");
            }

            if (!WebRTCConnectionActive && IsWebRTCActive) {
                SimpleWebRTCLogger.Log("DEBUG: Stopping WebRTC connection");
                IsWebRTCActive = !IsWebRTCActive;
                webRTCManager.CloseWebRTC();
            }


            if (StartStopVideoTransmission && !IsVideoTransmissionActive && IsVideoAudioSender) {
                IsVideoTransmissionActive = !IsVideoTransmissionActive;
                StartVideoTransmission();
            }

            if (!StartStopVideoTransmission && IsVideoTransmissionActive) {
                IsVideoTransmissionActive = !IsVideoTransmissionActive;
                StopVideoTransmission();
            }


            if (IsImmersiveSetupActive && IsVideoAudioReceiver) {
                if (webRTCManager.ImmersiveVideoTexture != null) {
                    Graphics.Blit(webRTCManager.ImmersiveVideoTexture, receivingRenderTexture);
                }
            }
        }

        private void LateUpdate() {
            if (UseImmersiveSetup && IsVideoAudioSender) {
                if (OneFacePerFrame) {
                    faceToRender = Time.frameCount % 6;
                    faceMask = 1 << faceToRender;
                }
                if (RenderStereo) {
                    // render left and right eye for IPD StereoSeparation
                    StreamingCamera.stereoSeparation = StereoSeparation;

                    // render both eyes for stereo view
                    StreamingCamera.RenderToCubemap(cubemapRightEye, faceMask, Camera.MonoOrStereoscopicEye.Right);
                    StreamingCamera.RenderToCubemap(cubemapLeftEye, faceMask, Camera.MonoOrStereoscopicEye.Left);

                    // convert into equirect rendertexture for streaming
                    cubemapLeftEye.ConvertToEquirect(videoEquirect, Camera.MonoOrStereoscopicEye.Left);
                    cubemapRightEye.ConvertToEquirect(videoEquirect, Camera.MonoOrStereoscopicEye.Right);

                } else {
                    StreamingCamera.RenderToCubemap(cubemapLeftEye, faceMask, Camera.MonoOrStereoscopicEye.Left);
                    cubemapLeftEye.ConvertToEquirect(videoEquirect, Camera.MonoOrStereoscopicEye.Mono);
                }
            }
        }

        private void OnEnable() {
            ConnectClient();
        }

        private void OnDisable() {
            DisconnectClient();
        }

        private void OnDestroy() {
            DisconnectClient();

            // de-register events for connection
            webRTCManager.OnWebSocketConnection -= WebSocketConnectionChanged.Invoke;
            webRTCManager.OnWebRTCConnection -= WebRTCConnected.Invoke;
            webRTCManager.OnVideoStreamEstablished -= VideoTransmissionReceived.Invoke;

            // release rendertextures to free memory
            cubemapLeftEye?.Release();
            if (RenderStereo) {
                cubemapRightEye?.Release();
            }
            videoEquirect?.Release();
        }


        private void ConnectClient() {
            if (WebSocketConnectionActive && !ConnectionToWebSocketInProgress && !IsWebSocketConnected) {
                string selectedEndpoint = UseLucyModel ? LucyWebSocket : MirageWebSocket ;
                webRTCManager.Connect(selectedEndpoint, IsVideoAudioSender, IsVideoAudioReceiver);
            }
        }

        private void DisconnectClient() {
            // stop websocket
            WebSocketConnectionActive = false;

            // stop webRTC
            IsWebRTCActive = false;
            WebRTCConnectionActive = false;

            // stop video
            StartStopVideoTransmission = false;
            IsVideoTransmissionActive = false;
            if (OptionalPreviewRawImage != null) {
                OptionalPreviewRawImage.texture = null;
            }
            if (StreamingCamera != null) {
                StreamingCamera.gameObject.SetActive(IsVideoTransmissionActive);
            }
            webRTCManager.RemoveVideoTrack();


            webRTCManager.CloseWebRTC();
            webRTCManager.CloseWebSocket();

            if (StreamingCamera != null) {
                StreamingCamera.gameObject.SetActive(false);
            }
        }



        public void Connect() {
            WebSocketConnectionActive = true;
        }

        public void ConnectWebRTC() {
            WebRTCConnectionActive = true;
        }

        public void Disconnect() {
            WebSocketConnectionActive = false;
        }

        public void SetModelChoice(bool useLucy) {
            UseLucyModel = useLucy;
            webRTCManager.SetModelType(useLucy);
        }

        public string GetSelectedModelName() {
            return UseLucyModel ? "Lucy" : "Mirage";
        }


        public void StartVideoTransmission() {
            SimpleWebRTCLogger.Log("Inside of start video transmisison now");
            StopCoroutine(StartVideoTransmissionAsync());
            StartCoroutine(StartVideoTransmissionAsync());
        }

        private IEnumerator StartVideoTransmissionAsync() {
            SimpleWebRTCLogger.Log("DEBUG: Starting video transmission async");

            StreamingCamera.gameObject.SetActive(true);
            SimpleWebRTCLogger.Log($"DEBUG: StreamingCamera activated: {StreamingCamera.name}");

            // Wait a couple frames to ensure camera is fully initialized
            yield return null;
            yield return null;

            if (IsVideoTransmissionActive) {
                // for restarting without stopping
                SimpleWebRTCLogger.Log("DEBUG: Restarting video transmission - removing existing track");
                webRTCManager.RemoveVideoTrack();
            }

            if (UseImmersiveSetup) {
                SimpleWebRTCLogger.Log("DEBUG: Using immersive setup for video");
                videoStreamTrack = new VideoStreamTrack(videoEquirect);
            } else {
                SimpleWebRTCLogger.Log($"DEBUG: Using standard camera capture - Resolution: {VideoResolution.x}x{VideoResolution.y}");
                videoStreamTrack = StreamingCamera.CaptureStreamTrack(VideoResolution.x, VideoResolution.y);
            }

            SimpleWebRTCLogger.Log($"DEBUG: VideoStreamTrack created: {videoStreamTrack != null}");
            webRTCManager.AddVideoTrack(videoStreamTrack);
            SimpleWebRTCLogger.Log("DEBUG: Video track added to WebRTC manager");

            StartStopVideoTransmission = true;
            IsVideoTransmissionActive = true;

            // OPTIONAL: Enable startup optimization phase (commented to match API-Example.html simplicity)
            // isInStartupPhase = true;
            // startupTimer = 0f;

            SimpleWebRTCLogger.Log("DEBUG: Video transmission marked as active");

            // Create offer immediately (matches API-Example.html approach)
            StartCoroutine(CreateOfferWithWarmup());
        }

        public IEnumerator CreateOffer() {
                    SimpleWebRTCLogger.Log("Creating offer");

                    // Enforce VP8 codec (matches API-Example.html VP8 preference)
                    var transceivers = webRTCManager.pc.GetTransceivers();
                    foreach (var transceiver in transceivers) {
                        if (transceiver.Sender != null && transceiver.Sender?.Track?.Kind == TrackKind.Video) {
                            var vp8 = RTCRtpSender.GetCapabilities(TrackKind.Video).codecs.Where(c => c.mimeType == "video/VP8").ToArray();
                            transceiver.SetCodecPreferences(vp8);

                            // Manual encoding parameters and FPS control
                             var parameters = transceiver.Sender.GetParameters();
                             foreach (var encoding in parameters.encodings) {
                                 encoding.maxBitrate = 4000000UL;      // 4Mbps max
                                 encoding.minBitrate = 1000000UL;      // 1Mbps min
                                 encoding.maxFramerate = 30U;          // 30fps (changed from 16fps to match API example)
                                 encoding.scaleResolutionDownBy = 1.0; // No downscaling
                             }
                             transceiver.Sender.SetParameters(parameters);
                             SimpleWebRTCLogger.Log("Set manual encoding parameters");
                        }
                    }

                    var offer = webRTCManager.pc.CreateOffer();
                    yield return offer;
                    SimpleWebRTCLogger.Log("CREATE OFFER IS: " + offer.Desc.sdp);

                    if (!offer.IsError) {

                        var offerDesc = offer.Desc;
                        SimpleWebRTCLogger.Log("OFFER DESC IS: " + offerDesc);
                        var localDescOp = webRTCManager.pc.SetLocalDescription(ref offerDesc);

                        var offerSessionDesc = new SessionDescription {
                            type = webRTCManager.pc.LocalDescription.type.ToString().ToLower(),
                            sdp = webRTCManager.pc.LocalDescription.sdp
                        };
                        var offerMessage = new outboundOfferMessage {
                            type = "offer",
                            sdp = offerSessionDesc.sdp
                        };
                        webRTCManager.ws.SendText(JsonUtility.ToJson(offerMessage));
                        // EnqueueWebSocketMessage(SignalingMessageType.OFFER, localPeerId, peerConnection.Key, offerSessionDesc.ConvertToJSON());
                    } else {
                        Debug.LogError($" Failed create offer . {offer.Error.message}");
                    }
                }

        private IEnumerator CreateOfferAsync() {
            // Yield control to avoid blocking the main thread during offer creation
            yield return null;
            yield return StartCoroutine(CreateOffer());
        }

        private IEnumerator CreateOfferWithWarmup() {
            // OPTIONAL: Encoder warmup (commented to match API-Example.html immediate approach)
            // SimpleWebRTCLogger.Log("DEBUG: Quick encoder warmup...");
            // yield return new WaitForSeconds(0.05f); // Just 50ms for immediate response

            // // Force a few frames to be encoded to prime the encoder
            // if (videoStreamTrack != null && StreamingCamera != null) {
            //     // Trigger several immediate renders to warm up the encoder pipeline
            //     for (int i = 0; i < 3; i++) {
            //         StreamingCamera.Render();
            //         yield return null;
            //     }
            //     SimpleWebRTCLogger.Log("DEBUG: Encoder primed with initial frames");
            // }

            // Create offer immediately (matches API-Example.html)
            yield return StartCoroutine(CreateOffer());
        }


        public void StopVideoTransmission() {

            StopCoroutine(StartVideoTransmissionAsync());

            StreamingCamera.gameObject.SetActive(false);

            videoStreamTrack?.Stop();
            webRTCManager.RemoveVideoTrack();

            videoStreamTrack?.Dispose();
            videoStreamTrack = null;

            StartStopVideoTransmission = false;
            IsVideoTransmissionActive = false;
        }

        public void CreateVideoReceiverGameObject(string senderPeerId) {
            videoReceiverSenderPeerId = senderPeerId;
            createVideoReceiver = true;
        }

        private void CreateVideoReceiver() {
            if (createVideoReceiver) {
                createVideoReceiver = false;

                // create new video receiver gameobject
                var receivingRawImage = new GameObject().AddComponent<RawImage>();
                receivingRawImage.name = $"{videoReceiverSenderPeerId}-Receiving-RawImage";
                receivingRawImage.rectTransform.SetParent(ReceivingRawImagesParent, false);
                receivingRawImage.rectTransform.localScale = Vector3.one;
                receivingRawImage.rectTransform.anchorMin = Vector2.zero;
                receivingRawImage.rectTransform.anchorMax = Vector2.one;
                receivingRawImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                receivingRawImage.rectTransform.sizeDelta = Vector2.zero;
                
                // Apply stream material if available
                if (streamMaterial != null) {
                    receivingRawImage.material = streamMaterial;
                    SimpleWebRTCLogger.Log("Applied StreamMaterial to receiving RawImage");
                }
                
                webRTCManager.VideoReceiver = receivingRawImage;
            }
        }

        public void DestroyVideoReceiverGameObject(string senderPeerId, bool removeFromReceivers = false) {
//            tempDestroyGameObjectRefs.Add(webRTCManager.VideoReceivers[senderPeerId].gameObject);
//            if (removeFromReceivers) {
                //webRTCManager.VideoReceivers.Remove(senderPeerId);
//            }
        }

        // OPTIONAL: Post-startup encoding optimization (commented to match API-Example.html simplicity)
        // private void OptimizeEncodingAfterStartup() {
        //     if (webRTCManager?.pc == null) return;

        //     var transceivers = webRTCManager.pc.GetTransceivers();
        //     foreach (var transceiver in transceivers) {
        //         if (transceiver.Sender != null && transceiver.Sender?.Track?.Kind == TrackKind.Video) {
        //             var parameters = transceiver.Sender.GetParameters();
        //             foreach (var encoding in parameters.encodings) {
        //                 // Reduce to stable bitrate after startup (2Mbps max, 500Kbps min)
        //                 encoding.maxBitrate = 2000000UL;
        //                 encoding.minBitrate = 500000UL;
        //                 // Allow WebRTC to adapt resolution if needed
        //                 encoding.scaleResolutionDownBy = null;
        //             }
        //             transceiver.Sender.SetParameters(parameters);
        //             SimpleWebRTCLogger.Log("Optimized encoding parameters after startup phase");
        //         }
        //     }
        // }

        public void SendNextPrompt(bool forward) {
            if (webRTCManager != null) {
                webRTCManager.SendNextPrompt(forward);
            } else {
                Debug.LogError("WebRTCConnection: webRTCManager is null!");
            }
        }

        public void SendCustomPrompt(string customPrompt) {
            if (webRTCManager != null) {
                Debug.Log("passing custom prompt to _webRTCManager" + customPrompt);
                webRTCManager.SendCustomPrompt(customPrompt);
            } else {
                Debug.LogError("WebRTCConnection: webRTCManager is null!");
            }
        }

        private void StartWebRTCUpdate() {
            if (startWebRTCUpdate) {
                startWebRTCUpdate = false;
                StartCoroutine(WebRTC.Update());
            }
        }

        private void StopWebRTCUpdate() {
            if (stopWebRTCUpdate) {
                stopWebRTCUpdate = false;
                StopCoroutine(WebRTC.Update());
            }
        }


        public void StartWebRTUpdateCoroutine() {
            startWebRTCUpdate = true;
        }

        public void StopWebRTCUpdateCoroutine() {
            stopWebRTCUpdate = true;
        }

        private void DestroyCachedGameObjects() {
            if (tempDestroyGameObjectRefs.Count > 0) {
                foreach (var cachedGameObject in tempDestroyGameObjectRefs) {
                    if (cachedGameObject != null) {
                        Destroy(cachedGameObject);
                    }
                }
            }
        }



    }
}