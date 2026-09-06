using TMPro;
using UnityEngine;
using Oculus.Voice;
using QuestCameraKit.WebRTC;

public class VoiceIntentController : MonoBehaviour
{
    [Header("Voice Service")]
    [SerializeField] private AppVoiceExperience appVoiceExperience;

    [Header("WebRTC")]
    [SerializeField] private WebRTCController webRTCController;

    [Header("UI")]
    [SerializeField] private TMP_Text fullTranscriptText;
    [SerializeField] private TMP_Text partialTranscriptText;

    private void Awake()
    {
        if (appVoiceExperience == null || webRTCController == null)
        {
            Debug.LogError("VoiceIntentController requires both voice and WebRTC references.");
            enabled = false;
            return;
        }

        appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener((transcription) => {
            webRTCController.QueueCustomPrompt(transcription);
            Debug.Log("Sent transcription to WebRTC: " + transcription);
            if (fullTranscriptText != null)
            {
                fullTranscriptText.text = transcription;
            }
        });

        appVoiceExperience.VoiceEvents.OnPartialTranscription.AddListener((transcription) => {
            if (partialTranscriptText != null)
            {
                partialTranscriptText.text = transcription;
            }
        });
    }

    private void Update() {
        if (appVoiceExperience != null && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)) {
            appVoiceExperience.Activate();
        }
    }
}