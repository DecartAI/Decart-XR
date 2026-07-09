# Coding Standards

This document outlines the coding conventions and standards used in the Quest WebRTC AI Video Processing System.

## C# Coding Standards

### Naming Conventions

**Classes and Methods (PascalCase)**
```csharp
public class WebRTCController : MonoBehaviour
public void StartVideoTransmission()
public bool IsConnected { get; set; }
```

**Private Fields (camelCase with underscore prefix for backing fields)**
```csharp
private WebCamTexture _webcamTexture;
private bool isProcessing;
[SerializeField] private RawImage canvasRawImage;
```

**Constants (PascalCase)**
```csharp
public const int MaxRetryAttempts = 3;
private const string DefaultWebSocketUrl = "wss://api3.decart.ai/v1/stream?api_key=YOUR_API_KEY&model=lucy-restyle-2";
```

**Local Variables (camelCase)**
```csharp
var connectionState = pc.IceConnectionState;
string promptName = GetCurrentPromptName();
```

### Unity-Specific Conventions

**Serialized Fields**
- Use `[SerializeField]` for inspector-visible private fields
- Always provide clear, descriptive names for inspector fields
- Group related fields with `[Header("Category Name")]`

```csharp
[Header("Camera Configuration")]
[SerializeField] private PassthroughCameraEye cameraEye = PassthroughCameraEye.Left;
[SerializeField] private Vector2Int requestedResolution = new Vector2Int(1280, 704);

[Header("UI References")]
[SerializeField] private RawImage receivedVideoImage;
[SerializeField] private TMP_Text promptNameText;
```

**Component References**
- Cache component references in `Awake()` or `Start()`
- Use null checks before accessing components
- Prefer dependency injection through inspector assignment

```csharp
[SerializeField] private WebCamTextureManager passthroughCameraManager;

private void Start() {
    if (passthroughCameraManager == null) {
        passthroughCameraManager = FindObjectOfType<WebCamTextureManager>();
    }
}
```

### Error Handling and Logging

**Exception Handling**
```csharp
try {
    await StartCamera();
} catch (System.Exception e) {
    Debug.LogError($"Failed to start camera: {e.Message}");
    ShowErrorUI("Camera initialization failed");
    return;
}
```

**Logging Standards**
```csharp
// Use appropriate log levels
Debug.Log("Connection established successfully");
Debug.LogWarning("Retrying connection due to timeout");
Debug.LogError("Critical failure in WebRTC setup");

// Include context in log messages
Debug.Log($"Camera resolution set to {resolution.x}x{resolution.y}");
Debug.LogError($"Permission denied for {permission}");
```

### Memory Management and Cleanup

**IDisposable Pattern**
```csharp
void OnDestroy() {
    // Always cleanup resources
    if (webCamTexture != null) {
        webCamTexture.Stop();
        webCamTexture = null;
    }

    webRTCManager?.Disconnect();

    // Unsubscribe from events
    if (appVoiceExperience != null) {
        appVoiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
    }
}
```

**Null Reference Safety**
```csharp
// Use null-conditional operators
receivedVideoImage?.gameObject.SetActive(true);

// Check before array access
if (devices != null && devices.Length > 0) {
    selectedDevice = devices[0];
}

// Validate parameters
public void SetPrompt(string prompt) {
    if (string.IsNullOrEmpty(prompt)) {
        Debug.LogWarning("Cannot set empty prompt");
        return;
    }

    currentPrompt = prompt;
}
```

## WebRTC and Networking Standards

### Async/Await Patterns

**Proper Async Methods**
```csharp
public async Task<bool> ConnectAsync() {
    try {
        await EstablishConnection();
        return IsConnected;
    } catch (Exception e) {
        Debug.LogError($"Connection failed: {e.Message}");
        return false;
    }
}
```

**Unity Coroutines (when async/await not suitable)**
```csharp
private IEnumerator InitializeCameraCoroutine() {
    // Wait for proper initialization timing
    yield return null;
    yield return new WaitForSeconds(1f);

    if (webCamTexture != null) {
        webCamTexture.Play();
    }
}
```

### Event Handling

**Unity Events**
```csharp
[System.Serializable]
public class ConnectionStateEvent : UnityEvent<bool> { }

[Header("Events")]
public ConnectionStateEvent OnConnectionChanged;

// Invoke with null checks
OnConnectionChanged?.Invoke(isConnected);
```

**C# Events**
```csharp
public event System.Action<string> OnPromptChanged;

private void SetCurrentPrompt(string prompt) {
    currentPrompt = prompt;
    OnPromptChanged?.Invoke(prompt);
}
```

## Documentation Standards

### XML Documentation Comments

**Classes**
```csharp
/// <summary>
/// Manages Quest passthrough camera access via Unity's WebCamTexture API.
/// Handles permissions, device discovery, and camera initialization.
/// </summary>
public class WebCamTextureManager : MonoBehaviour
```

**Methods**
```csharp
/// <summary>
/// Starts the Quest passthrough camera with specified configuration.
/// </summary>
/// <param name="eye">Camera eye selection (Left/Right)</param>
/// <param name="resolution">Requested camera resolution</param>
/// <returns>True if camera started successfully</returns>
public async Task<bool> StartCamera(PassthroughCameraEye eye, Vector2Int resolution)
```

**Public Properties**
```csharp
/// <summary>
/// Gets the current WebCamTexture instance. Null if camera not initialized.
/// </summary>
public WebCamTexture WebCamTexture { get; private set; }
```

### Inline Comments

**Complex Logic**
```csharp
// VP8 encoder warmup - prevents initial connection lag
// Start with higher bitrate for 3 seconds, then reduce to sustainable rate
var parameters = transceiver.Sender.GetParameters();
foreach (var encoding in parameters.encodings) {
    encoding.maxBitrate = isWarmupPhase ? 4000000UL : 2000000UL;
    encoding.maxFramerate = 16U;  // Match AI processing speed
}
```

**Unity-Specific Workarounds**
```csharp
// Unity WebCamTexture bug workaround - must wait before calling Play()
yield return null;
yield return new WaitForSeconds(1f);
webCamTexture.Play();
```

## Performance Standards

### Unity Performance Best Practices

**Avoid Allocations in Update**
```csharp
// Cache frequently used components
private void Start() {
    cachedTransform = transform;
    cachedRenderer = GetComponent<Renderer>();
}

private void Update() {
    // Use cached references instead of GetComponent calls
    cachedTransform.position = newPosition;
}
```

**Object Pooling for Frequent Instantiation**
```csharp
// For frequently created/destroyed objects
public class ObjectPool<T> where T : MonoBehaviour {
    private Queue<T> pool = new Queue<T>();

    public T Get() {
        return pool.Count > 0 ? pool.Dequeue() : CreateNew();
    }

    public void Return(T item) {
        item.gameObject.SetActive(false);
        pool.Enqueue(item);
    }
}
```

### WebRTC Performance

**Bitrate Management**
```csharp
// Adaptive bitrate based on connection quality
private void AdjustBitrate(RTCStatsReport stats) {
    var targetBitrate = CalculateOptimalBitrate(stats);

    // Clamp to reasonable bounds
    targetBitrate = Mathf.Clamp(targetBitrate, 1000000, 4000000);  // 1-4 Mbps

    SetEncodingBitrate(targetBitrate);
}
```

## Testing Standards

### Unit Test Structure

```csharp
[Test]
public void WebRTCConnection_WhenValidUrl_ShouldConnect() {
    // Arrange
    var connection = new WebRTCConnection();
    var validUrl = "wss://test.example.com/ws";

    // Act
    var result = connection.Connect(validUrl);

    // Assert
    Assert.IsTrue(result);
    Assert.AreEqual(ConnectionState.Connected, connection.State);
}
```

### Integration Test Guidelines

```csharp
[UnityTest]
public IEnumerator CameraManager_WhenPermissionsGranted_ShouldInitialize() {
    // Arrange
    var cameraManager = new GameObject().AddComponent<WebCamTextureManager>();
    MockPermissionGranted();

    // Act
    cameraManager.StartCamera();
    yield return new WaitForSeconds(2f);

    // Assert
    Assert.IsNotNull(cameraManager.WebCamTexture);
    Assert.IsTrue(cameraManager.WebCamTexture.isPlaying);
}
```

## File Organization

### Directory Structure
```
Assets/
├── Samples/DecartAI-Quest/
│   ├── Scripts/
│   │   ├── Editor/               # Editor-only scripts
│   │   ├── Voice/                # Voice control components
│   │   ├── WebRTCController.cs   # Main controller
│   │   └── DecartAIQuest.asmdef  # Assembly definition
│   ├── Scenes/
│   └── Prefabs/
└── PassthroughCameraApiSamples/  # Camera integration
```

### Script Organization

**One class per file** with matching filename
```csharp
// File: WebRTCController.cs
public class WebRTCController : MonoBehaviour
```

**Related classes can share file** if tightly coupled
```csharp
// File: WebRTCMessages.cs
public class OutboundInitMessage { }
public class InboundAnswerMessage { }
public class IceCandidateMessage { }
```

## Code Review Checklist

### Functionality
- [ ] Code compiles without warnings
- [ ] All public methods have XML documentation
- [ ] Error handling covers expected failure cases
- [ ] Memory cleanup implemented (OnDestroy, using statements)

### Unity Specific
- [ ] SerializeField used instead of public fields
- [ ] Component references cached appropriately
- [ ] No GetComponent calls in Update/FixedUpdate
- [ ] Proper coroutine cleanup on disable/destroy

### WebRTC/Networking
- [ ] Connection state properly monitored
- [ ] Retry logic implemented for failures
- [ ] Bandwidth usage optimized
- [ ] Security considerations addressed

### Performance
- [ ] No unnecessary allocations in frequently called methods
- [ ] Appropriate use of object pooling
- [ ] Texture memory properly managed
- [ ] Frame rate impact minimized

## Version Control Standards

### Commit Messages
```
type: Short description (50 chars or less)

Longer explanation if necessary. Wrap at 72 characters.
Reference issues with #123.

Types: feat, fix, docs, style, refactor, test, chore
```

### Branch Naming
- `feature/webrtc-optimization`
- `fix/camera-permission-bug`
- `docs/api-reference-update`

---

**These standards help maintain code quality and consistency across the Quest WebRTC AI Video Processing System. All contributors should follow these guidelines to ensure the codebase remains maintainable and professional.**