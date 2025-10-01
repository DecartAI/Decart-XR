using PassthroughCameraSamples;
using SimpleWebRTC;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuestCameraKit.WebRTC {
    public class WebRTCController : MonoBehaviour {
        [SerializeField] private WebCamTextureManager passthroughCameraManager;
        [SerializeField] private RawImage canvasRawImage;
        [SerializeField] private GameObject connectionGameObject;
        [SerializeField] private RawImage receivedVideoImage;
        [SerializeField] private TMP_Text promptNameText;
        private WebCamTexture _webcamTexture;
        private WebRTCConnection _webRTCConnection;
        public string _pendingCustomPrompt = null;
        private bool videoReceivedAndReady = false;

        private IEnumerator Start() {
            yield return new WaitUntil(() => passthroughCameraManager.WebCamTexture != null && passthroughCameraManager.WebCamTexture.isPlaying);

            _webRTCConnection = connectionGameObject.GetComponent<WebRTCConnection>();
            _webcamTexture = passthroughCameraManager.WebCamTexture;
            canvasRawImage.texture = _webcamTexture;

            // Subscribe to video received event and prompt change
            if (_webRTCConnection != null) {
                _webRTCConnection.VideoTransmissionReceived.AddListener(OnVideoReceived);
                _webRTCConnection.PromptNameUpdated.AddListener(UpdatePromptName);

            }
        }
        
        private void OnVideoReceived() {
            Debug.Log("Video transmission received!");
            videoReceivedAndReady = true; // Enable prompt cycling
            // The WebRTC system will automatically create RawImage components for received video
            // We need to find and copy the texture to our receivedVideoImage
            StartCoroutine(FindReceivedVideo());
        }

        private void UpdatePromptName(string promptKey) {
            Debug.Log("Initiate Update prompt name");
            if (promptNameText != null) {
                promptNameText.text = promptKey;
                Debug.Log("Updated");
            }
            Debug.Log("Didnt update");
        }

        private IEnumerator FindReceivedVideo() {
            yield return new WaitForSeconds(0.5f); // Give time for video receiver to be created
            
            // Look for automatically created video receiver objects
            var receivingObjects = GameObject.FindObjectsByType<RawImage>(FindObjectsSortMode.None);
            foreach (var rawImage in receivingObjects) {
                if (rawImage.name.Contains("Receiving-RawImage") && rawImage.texture != null) {
                    Debug.Log($"Found received video: {rawImage.name}");
                    if (receivedVideoImage != null) {
                        receivedVideoImage.texture = rawImage.texture;
                    }
                    break;
                }
            }
        }

//        public void SendCustomPrompt(string customPrompt){
//            Debug.Log("_webRTCConnection is: " + (_webRTCConnection != null));
//            Debug.Log("passing custom prompt to _webRTCConnection " + customPrompt);
//            _webRTCConnection.SendCustomPrompt(customPrompt);
//        }

        public void QueueCustomPrompt(string prompt) {
                _pendingCustomPrompt = prompt;
            }

        private void Update() {
            // Only handle prompt cycling if video has been received
            if (!videoReceivedAndReady) {
                return; // Let GameManager handle button input during model selection
            }

            // Quest controller inputs - Prompt cycling (only after video received)
            if (OVRInput.GetDown(OVRInput.Button.One)) {
                Debug.Log("WebRTC: A button pressed - Sending next prompt");
                if (_webRTCConnection != null) {
                    _webRTCConnection.SendNextPrompt(true);
                    Debug.Log("WebRTC: SendNextPrompt(true) called successfully");
                } else {
                    Debug.LogError("WebRTC: _webRTCConnection is null!");
                }
            }

            if (OVRInput.GetDown(OVRInput.Button.Two)) {
                Debug.Log("WebRTC: B button pressed - Sending previous prompt");
                if (_webRTCConnection != null) {
                    _webRTCConnection.SendNextPrompt(false);
                    Debug.Log("WebRTC: SendNextPrompt(false) called successfully");
                } else {
                    Debug.LogError("WebRTC: _webRTCConnection is null!");
                }
            }

            if (!string.IsNullOrEmpty(_pendingCustomPrompt)) {
                Debug.Log("got new custom prompt: " + _pendingCustomPrompt);
                if (_webRTCConnection != null){
                    Debug.Log("ws isnt null, sending custom prompt " + _pendingCustomPrompt);
                    _webRTCConnection.SendCustomPrompt(_pendingCustomPrompt);
                    _pendingCustomPrompt = null;
                }
            }

        }
    }

}