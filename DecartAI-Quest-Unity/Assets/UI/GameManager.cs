using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using DG.Tweening;
using SimpleWebRTC;

public class GameManager : MonoBehaviour
{
    public Button startButton;
    public PortalController portal;
    public Transform streamParent;
    public UIFader menuFader;
    public UIFader textFader;
    public OVRPassthroughLayer passthroughLayer;
    public GameObject WebRTCWebcamCanvas;
    public Camera recordingCamera;
    public GameObject effectCone;
    public AudioClip[] forceFieldSounds;
    public AudioSource audioSource;
    public Material introEffectMaterial;
    public WebRTCConnection webRTCConnection;

    private bool didStart = false;
    private bool videoTransmissionPending = false;
    private bool playingForceFieldSounds = false;
    private int currentSoundIndex = 0;
    private bool animatingIntroEffect = false;
    private bool waitingForModelSelection = false;
    private bool modelSelected = false;
    
    private void Start()
    {
        ShowModelSelectionPrompt(); // Auto-start model selection immediately

        #if !UNITY_EDITOR
        WebRTCWebcamCanvas.gameObject.SetActive(true);
        RemoveSkybox();
        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 2f;
        (GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset).renderScale = 2f;
        #endif
    }

    private void ShowModelSelectionPrompt()
    {
        waitingForModelSelection = true;
        Debug.Log("Model selection: Press A for Mirage or B for Lucy");
        // Menu stays visible, waiting for A/B button input
    }
    
    private void StartExperience()
    {
        didStart = true;
        menuFader.FadeOut();
        portal.Show();
        effectCone.gameObject.SetActive(true);

        if (passthroughLayer != null)
        {
            DOTween.To(
                () => passthroughLayer.textureOpacity,
                x => passthroughLayer.textureOpacity = x,
                0.25f, 1f);
        }

        if (videoTransmissionPending)
        {
            DOVirtual.DelayedCall(3f, OnVideoTransmissionReceived);
        }

        StartForceFieldSounds();
        StartIntroEffect();
    }

    public void OnVideoTransmissionReceived()
    {
        if (!didStart)
        {
            videoTransmissionPending = true;
            return;
        }

        videoTransmissionPending = false;
        StopForceFieldSounds();
        StopIntroEffect();

        streamParent.gameObject.SetActive(true);
        portal.Expand();
        effectCone.gameObject.SetActive(false);

        // Show text and apply portal material when not in wide FoV
        textFader.FadeIn(delay: 3f);

        RawImage[] rawImages = streamParent.GetComponentsInChildren<RawImage>();
        foreach (RawImage rawImage in rawImages)
        {
            rawImage.material = portal.portalMaterial;
        }
    }
    
    private void SelectModelAndStart(bool useLucy)
    {
        modelSelected = true;
        waitingForModelSelection = false;

        // Set the model choice on WebRTC connection
        if (webRTCConnection != null)
        {
            webRTCConnection.SetModelChoice(useLucy);
            Debug.Log($"Selected model: {webRTCConnection.GetSelectedModelName()}");

            // Trigger the WebSocket connection with selected endpoint
            webRTCConnection.Connect();
        }
        else
        {
            Debug.LogError("WebRTCConnection reference is missing!");
        }

        // Start the experience
        StartExperience();
    }

    private void Update()
    {
        // Model selection phase (before experience starts)
        if (waitingForModelSelection && !modelSelected)
        {
            if (OVRInput.GetDown(OVRInput.Button.One)) // A button = Mirage
            {
                SelectModelAndStart(useLucy: false);
            }
            else if (OVRInput.GetDown(OVRInput.Button.Two)) // B button = Lucy
            {
                SelectModelAndStart(useLucy: true);
            }
            return; // Don't process other button logic during selection
        }

        // Original logic (after experience started)
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            if (!didStart) ShowModelSelectionPrompt();
            else ShowWave();
        }

        if (didStart && OVRInput.GetDown(OVRInput.Button.Two))
        {
            ShowWave();
        }
    }

    private void ShowWave()
    {
        effectCone.gameObject.SetActive(true);
        StartCoroutine(DisableEffectConeAfterDelay());
        audioSource.clip = forceFieldSounds[currentSoundIndex];
        audioSource.pitch = 0.75f;
        audioSource.Play();
        currentSoundIndex = (currentSoundIndex + 1) % forceFieldSounds.Length;

        if (introEffectMaterial != null)
        {
            StartIntroEffect();
        }
    }

    private IEnumerator DisableEffectConeAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        StopIntroEffect();
        effectCone.gameObject.SetActive(false);
    }
    
    private void StartForceFieldSounds()
    {
        if (forceFieldSounds == null || forceFieldSounds.Length == 0 || audioSource == null)
            return;

        playingForceFieldSounds = true;
        currentSoundIndex = 0;
        PlayNextForceFieldSound();
    }

    private void StopForceFieldSounds()
    {
        playingForceFieldSounds = false;
    }

    private void PlayNextForceFieldSound()
    {
        if (!playingForceFieldSounds || forceFieldSounds == null || forceFieldSounds.Length == 0)
            return;

        audioSource.clip = forceFieldSounds[currentSoundIndex];
        audioSource.pitch = 0.75f;
        audioSource.Play();

        float clipLength = 1.5f;
        DOVirtual.DelayedCall(clipLength, () =>
        {
            if (playingForceFieldSounds)
            {
                currentSoundIndex = (currentSoundIndex + 1) % forceFieldSounds.Length;
                PlayNextForceFieldSound();
            }
        });
    }

    private void StartIntroEffect()
    {
        if (introEffectMaterial == null)
            return;

        animatingIntroEffect = true;
        introEffectMaterial.SetFloat("_CustomTime", 0f);
        StartCoroutine(UpdateIntroEffectTime());
    }

    private void StopIntroEffect()
    {
        animatingIntroEffect = false;
    }

    private IEnumerator UpdateIntroEffectTime()
    {
        float startTime = Time.time;
        while (animatingIntroEffect)
        {
            float elapsedTime = Time.time - startTime;
            introEffectMaterial.SetFloat("_CustomTime", elapsedTime);
            yield return null;
        }
    }

    private void RemoveSkybox()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        DynamicGI.UpdateEnvironment();
    }
}
