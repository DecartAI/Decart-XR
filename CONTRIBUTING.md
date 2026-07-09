# Contributing to Quest WebRTC AI Video Processing System

Thank you for your interest in contributing to this open source research project! This guide will help you get started with contributing to our real-time AI video processing system for Meta Quest.

## 🎯 Project Vision

This project serves as an educational resource demonstrating the intersection of VR, computer vision, and AI. We welcome contributions that:

- Improve the educational value of the codebase
- Enhance performance and reliability
- Add new AI style transformations
- Expand platform compatibility
- Improve documentation and examples

## 🚀 Getting Started

### Prerequisites

Before contributing, ensure you have:
- Meta Quest 3 or Quest 3S device
- Unity 6 (6000.0.34f1 or later)
- Android SDK with API Level 29+
- Git and GitHub account

### Setting Up Your Development Environment

1. **Fork and Clone**
   ```bash
   git clone https://github.com/yourusername/QuestCameraKit.git
   cd QuestCameraKit
   ```

2. **Open in Unity**
   - Launch Unity Hub
   - Add project from `DecartAI-Quest-Unity/` folder
   - Open with Unity 6

3. **Load Main Scene**
   ```
   DecartAI-Quest-Unity/Assets/Samples/DecartAI-Quest/DecartAI-Main.unity
   ```

4. **Test on Device**
   - Build for Android (Quest platform)
   - Install APK on Quest headset
   - Verify camera permissions and AI processing work

## 📝 How to Contribute

### 🐛 Bug Reports

When reporting bugs, please include:
- **Device Information**: Quest model, Horizon OS version
- **Unity Version**: Exact Unity version used
- **Network Conditions**: Connection speed and latency
- **Reproduction Steps**: Clear steps to reproduce the issue
- **Expected vs Actual Behavior**: What should happen vs what happens
- **Logs**: Unity console output and Quest device logs if available

### ✨ Feature Requests

For new features, please:
- Check existing issues to avoid duplicates
- Clearly describe the proposed functionality
- Explain how it benefits the research/educational goals
- Consider implementation complexity and compatibility

### 🔧 Code Contributions

#### Areas We Welcome Contributions

1. **AI Style Enhancements**
   - Add new prompts to appropriate model in `WebRTCManager.cs`:
     - `lucyRestyle2Prompts` (lines 89-151): World transformations like Cyberpunk, Frozen, Lego
     - `lucyPrompts` (lines 153-169): Person transformations like Spiderman, Medieval Knight
   - Test custom prompts with `SendCustomPrompt()`
   - Document style descriptions for optimal AI results

2. **Performance Optimization**
   - Unity Profiler analysis and optimizations
   - Memory usage improvements
   - Network bandwidth efficiency
   - Thermal management enhancements

3. **Platform Expansion**
   - Support for new Quest devices
   - Camera discovery improvements
   - Android API compatibility

4. **Documentation Improvements**
   - Code comments and inline documentation
   - Tutorial enhancements
   - Troubleshooting guides
   - Performance benchmarking

#### Code Standards

**C# Conventions:**
- Follow Unity's C# coding standards
- Use PascalCase for public methods and properties
- Use camelCase for private fields and local variables
- Add XML documentation comments for public APIs
- Keep methods focused and under 50 lines when possible

**Unity Specific:**
- Use `[SerializeField]` for inspector-visible private fields
- Implement proper cleanup in `OnDisable()`/`OnDestroy()`
- Use coroutines for async operations
- Handle null references gracefully

**WebRTC & Networking:**
- Always check connection state before sending data
- Implement proper error handling for network failures
- Log connection events for debugging
- Handle graceful reconnection

#### Pull Request Process

1. **Create a Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make Your Changes**
   - Write clean, documented code
   - Test thoroughly on Quest hardware
   - Follow existing code patterns

3. **Test Requirements**
   - Verify functionality on actual Quest 3/3S device
   - Test with both Lucy-Restyle-2 and Lucy AI models
   - Test with different network conditions
   - Ensure no regression in existing features
   - Check memory usage doesn't increase significantly

4. **Commit Guidelines**
   - Use clear, descriptive commit messages
   - Reference related issues with `#issue-number`
   - Keep commits atomic and focused

5. **Submit Pull Request**
   - Provide clear description of changes
   - Include testing performed
   - Link to related issues
   - Add screenshots/videos if UI changes

## 🧪 Testing Guidelines

### Required Testing

- **Hardware Testing**: Must test on actual Quest hardware
- **Model Testing**: Test with both Lucy-Restyle-2 and Lucy models to ensure compatibility
- **Network Testing**: Test with various connection speeds
- **Performance Testing**: Monitor CPU, GPU, memory usage
- **Compatibility Testing**: Test with different Unity versions

### Testing Tools

- Unity Profiler for performance analysis
- Quest Developer Hub for device monitoring
- ADB for logging: `adb logcat -s Unity:* WebRTC:* Camera:*`

## 📚 Development Resources

### Key Files to Understand

| File | Purpose |
|------|---------|
| `WebRTCController.cs` | Main application controller |
| `WebRTCManager.cs` | Core WebRTC logic with dual AI prompt banks (61 Lucy-Restyle-2 + 15 Lucy) |
| `WebCamTextureManager.cs` | Quest camera integration |
| `PassthroughCameraUtils.cs` | Android Camera2 API |

## 🤝 Community Guidelines

### Communication

- **GitHub Issues**: For bugs, features, and technical discussions
- **Email**: tom@decart.ai for research collaboration
- **Pull Requests**: For code review and technical feedback

### Code of Conduct

- Be respectful and constructive in all interactions
- Focus on technical merit and educational value
- Help newcomers understand the codebase
- Share knowledge and document discoveries
- Respect intellectual property and licensing

## 🎓 Educational Focus

This project prioritizes educational value. When contributing:

- **Explain Complex Concepts**: Add comments explaining WebRTC flows, camera APIs, etc.
- **Document Challenges**: Share solutions to tricky implementation problems
- **Provide Examples**: Include usage examples for new features
- **Performance Insights**: Document optimization discoveries

## ❓ Getting Help

- **Getting Started**: Review the setup documentation in README.md
- **Implementation Questions**: Search existing GitHub issues
- **Research Collaboration**: Contact tom@decart.ai

## 📄 Legal Considerations

- All contributions must be compatible with MIT licensing
- Respect third-party licenses (Meta SDK, Unity, etc.)
- Don't include proprietary code or copyrighted assets
- Contributions become part of the open source project

---

**Thank you for contributing to this research project!** Your contributions help advance the understanding of real-time AI processing in VR environments and benefit the entire developer community.