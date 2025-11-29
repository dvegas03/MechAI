using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Meta.WitAi.TTS.Utilities;

/// <summary>
/// State machine for the demo orchestrator
/// </summary>
public enum DemoState
{
    Initializing,
    Greeting,
    WaitingForFirstUserInput,
    DeliveringPredefinedInstruction,
    ConfirmingPredefinedInstruction,
    RunningSteps,
    StepAwaitingUserQuestion,
    ProcessingUserQuestion,
    Completed,
    IdleQnA
}

/// <summary>
/// Unified orchestrator for the XR mechanic assistant demo.
/// Controls the full pipeline: Whisper → LLM → TTS → Bubble → YOLO integration.
/// </summary>
public class AssistantDemoOrchestrator : MonoBehaviour
{
    #region Serialized Fields

    [Header("Core References")]
    [SerializeField] private WhisperToLLMBridge whisperBridge;
    [SerializeField] private LLMController llmController;
    [SerializeField] private TTSSpeaker ttsSpeaker;
    [SerializeField] private AiBubbleController bubble;
    [SerializeField] private Detector visionDetector;

    [Header("XR")]
    [SerializeField] private Camera xrCamera;

    [Header("Demo Configuration")]
    [SerializeField] private DemoStepsConfig demoConfig;
    [SerializeField] private bool autoStartDemo = true;
    [SerializeField] private float greetingDelaySeconds = 1.0f;

    [Header("Vision")]
    [SerializeField] private bool enableVisionQueries = true;
    [SerializeField] private RenderTexture cameraRenderTexture;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    #endregion

    #region Private State

    private DemoState _currentState = DemoState.Initializing;
    private int _currentStepIndex = -1;
    private List<Detection> _lastDetections = new List<Detection>();
    private Texture2D _lastCameraFrame;
    private bool _isSpeaking = false;
    private bool _isProcessing = false;
    private DemoStep _currentStep;
    private TaskCompletionSource<bool> _ttsCompletionSource;

    #endregion

    #region Properties

    public DemoState CurrentState => _currentState;
    public int CurrentStepIndex => _currentStepIndex;
    public DemoStep CurrentStep => _currentStep;
    public List<Detection> LastDetections => _lastDetections;
    public bool IsSpeaking => _isSpeaking;
    public bool IsProcessing => _isProcessing;

    #endregion

    #region Events

    /// <summary>
    /// Fired when demo state changes
    /// </summary>
    public event Action<DemoState, DemoState> OnStateChanged;

    /// <summary>
    /// Fired when a step is completed
    /// </summary>
    public event Action<int, DemoStep> OnStepCompleted;

    /// <summary>
    /// Fired when the entire demo is completed
    /// </summary>
    public event Action OnDemoCompleted;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        ValidateReferences();
    }

    private void Start()
    {
        SubscribeToEvents();

        if (autoStartDemo)
        {
            StartCoroutine(StartDemoAfterDelay(greetingDelaySeconds));
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        
        if (_lastCameraFrame != null)
        {
            Destroy(_lastCameraFrame);
        }
    }

    #endregion

    #region Initialization

    private void ValidateReferences()
    {
        if (whisperBridge == null) Debug.LogError("[Orchestrator] WhisperToLLMBridge not assigned!");
        if (llmController == null) Debug.LogError("[Orchestrator] LLMController not assigned!");
        if (ttsSpeaker == null) Debug.LogError("[Orchestrator] TTSSpeaker not assigned!");
        if (bubble == null) Debug.LogError("[Orchestrator] AiBubbleController not assigned!");
        if (demoConfig == null) Debug.LogWarning("[Orchestrator] DemoStepsConfig not assigned - using defaults.");
    }

    private void SubscribeToEvents()
    {
        // Subscribe to Whisper events
        if (whisperBridge != null)
        {
            whisperBridge.OrchestratorMode = true; // Enable orchestrator mode
            whisperBridge.OnUserSpeechFinalized += OnUserUtterance;
            whisperBridge.OnVoiceActivityChanged += OnVoiceActivityChanged;
        }

        // Subscribe to YOLO detector
        if (visionDetector != null)
        {
            visionDetector.OnDetectionsReady += OnDetectionsReceived;
        }

        // Subscribe to TTS events
        if (ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnStartSpeaking.AddListener(OnTTSStarted);
            ttsSpeaker.Events.OnFinishedSpeaking.AddListener(OnTTSFinished);
            ttsSpeaker.Events.OnCancelledSpeaking.AddListener(OnTTSCancelled);
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (whisperBridge != null)
        {
            whisperBridge.OnUserSpeechFinalized -= OnUserUtterance;
            whisperBridge.OnVoiceActivityChanged -= OnVoiceActivityChanged;
        }

        if (visionDetector != null)
        {
            visionDetector.OnDetectionsReady -= OnDetectionsReceived;
        }

        if (ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnStartSpeaking.RemoveListener(OnTTSStarted);
            ttsSpeaker.Events.OnFinishedSpeaking.RemoveListener(OnTTSFinished);
            ttsSpeaker.Events.OnCancelledSpeaking.RemoveListener(OnTTSCancelled);
        }
    }

    #endregion

    #region Demo Flow Control

    private IEnumerator StartDemoAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartDemo();
    }

    /// <summary>
    /// Start the demo from the beginning
    /// </summary>
    public void StartDemo()
    {
        Log("Starting demo...");
        _currentStepIndex = -1;
        _currentStep = null;
        
        StartCoroutine(RunGreeting());
    }

    /// <summary>
    /// Reset and restart the demo
    /// </summary>
    public void RestartDemo()
    {
        StopAllCoroutines();
        _isSpeaking = false;
        _isProcessing = false;
        StartDemo();
    }

    /// <summary>
    /// Skip to a specific step
    /// </summary>
    public void SkipToStep(int stepIndex)
    {
        if (demoConfig == null || stepIndex < 0 || stepIndex >= demoConfig.StepCount)
        {
            Debug.LogWarning($"[Orchestrator] Invalid step index: {stepIndex}");
            return;
        }

        _currentStepIndex = stepIndex - 1;
        StartCoroutine(RunNextStep());
    }

    #endregion

    #region Phase 0: Greeting

    private IEnumerator RunGreeting()
    {
        SetState(DemoState.Greeting);

        string greeting = demoConfig != null ? demoConfig.greetingMessage : 
            "Hey! How are you? What do you need help with today?";

        yield return SpeakAndWait(greeting);

        SetState(DemoState.WaitingForFirstUserInput);
        Log("Waiting for first user input...");
    }

    #endregion

    #region Phase 1: First User Input & Predefined Response

    private IEnumerator RunPredefinedInstructionFlow()
    {
        SetState(DemoState.DeliveringPredefinedInstruction);

        string instruction = demoConfig != null ? demoConfig.predefinedFirstResponse :
            "Sure! Let's first start by identifying your car's make and model.";

        yield return SpeakAndWait(instruction);

        // Move to confirmation phase
        StartCoroutine(RunConfirmationFlow());
    }

    #endregion

    #region Phase 2: LLM Confirmation

    private IEnumerator RunConfirmationFlow()
    {
        SetState(DemoState.ConfirmingPredefinedInstruction);
        
        bubble.SetThinking();
        _isProcessing = true;

        // Create a step for the identification phase
        var identificationStep = new StepData
        {
            procedureTitle = "Vehicle Identification",
            stepTitle = "Identify Vehicle",
            stepBody = "User should identify their vehicle make, model, and optionally VIN number."
        };

        string confirmationResponse = null;
        var task = llmController.ConfirmStepAsync(identificationStep, "User acknowledged the instruction");
        
        yield return new WaitUntil(() => task.IsCompleted);
        
        _isProcessing = false;

        if (task.Exception != null)
        {
            Debug.LogError($"[Orchestrator] Confirmation failed: {task.Exception.Message}");
            confirmationResponse = "Let's proceed with the steps.";
        }
        else
        {
            confirmationResponse = task.Result;
        }

        yield return SpeakAndWait(confirmationResponse);

        // Begin step-by-step flow
        StartCoroutine(RunDemoSteps());
    }

    #endregion

    #region Phase 3: Step-by-Step Demo Flow

    private IEnumerator RunDemoSteps()
    {
        SetState(DemoState.RunningSteps);

        if (demoConfig == null || demoConfig.StepCount == 0)
        {
            Log("No demo steps configured, moving to completion.");
            StartCoroutine(CompleteDemo());
            yield break;
        }

        while (_currentStepIndex < demoConfig.StepCount - 1)
        {
            yield return RunNextStep();
        }

        // All steps completed
        StartCoroutine(CompleteDemo());
    }

    private IEnumerator RunNextStep()
    {
        _currentStepIndex++;
        _currentStep = demoConfig.GetStep(_currentStepIndex);

        if (_currentStep == null)
        {
            Log($"Step {_currentStepIndex} is null, skipping.");
            yield break;
        }

        Log($"Running step {_currentStepIndex + 1}/{demoConfig.StepCount}: {_currentStep.stepTitle}");

        // Update WhisperBridge with current step context
        whisperBridge?.SetCurrentStep(
            _currentStep.procedureTitle,
            _currentStep.stepTitle,
            _currentStep.stepBody
        );

        // Highlight YOLO class if specified
        if (!string.IsNullOrEmpty(_currentStep.associatedYoloClass))
        {
            HighlightYOLOClass(_currentStep.associatedYoloClass);
        }

        // Speak the step instruction
        string script = !string.IsNullOrEmpty(_currentStep.assistantScript) 
            ? _currentStep.assistantScript 
            : _currentStep.stepBody;

        yield return SpeakAndWait(script);

        // Wait minimum duration
        if (_currentStep.minDurationSeconds > 0)
        {
            yield return new WaitForSeconds(_currentStep.minDurationSeconds);
        }

        // If step requires confirmation, wait for user
        if (_currentStep.requiresUserConfirmation)
        {
            SetState(DemoState.StepAwaitingUserQuestion);
            Log($"Step {_currentStepIndex + 1} awaiting user input or confirmation...");
            
            // Wait until user says something or a timeout
            // For now, we'll wait indefinitely until user speaks
            yield return new WaitUntil(() => _currentState != DemoState.StepAwaitingUserQuestion);
        }

        // Fire step completed event
        OnStepCompleted?.Invoke(_currentStepIndex, _currentStep);
    }

    #endregion

    #region Phase 4: Ongoing Q&A

    private void OnUserUtterance(string userText)
    {
        Log($"User said: \"{userText}\"");

        // Ignore input while speaking or processing
        if (_isSpeaking || _isProcessing)
        {
            Log("Ignoring input - currently speaking or processing.");
            return;
        }

        switch (_currentState)
        {
            case DemoState.WaitingForFirstUserInput:
                // First user input triggers predefined instruction
                StartCoroutine(RunPredefinedInstructionFlow());
                break;

            case DemoState.StepAwaitingUserQuestion:
            case DemoState.IdleQnA:
            case DemoState.Completed:
                // Route based on user intent
                StartCoroutine(HandleUserQuestion(userText));
                break;

            default:
                Log($"Ignoring input in state: {_currentState}");
                break;
        }
    }

    private IEnumerator HandleUserQuestion(string userText)
    {
        var previousState = _currentState;
        SetState(DemoState.ProcessingUserQuestion);
        
        bubble.SetThinking();
        _isProcessing = true;

        string lowerText = userText.ToLower();
        
        // Use a holder to capture the result from sub-coroutines
        var resultHolder = new CoroutineResult<string>();

        // Determine query type based on keywords
        if (ContainsAny(lowerText, "what do you see", "what is this", "identify", "recognize", "look at"))
        {
            // Vision query
            Log("Routing to vision query...");
            yield return HandleVisionQuery(userText, resultHolder);
        }
        else if (ContainsAny(lowerText, "is this right", "is this correct", "did i do", "confirm", "check"))
        {
            // Confirmation query
            Log("Routing to confirmation query...");
            yield return HandleConfirmationQuery(userText, resultHolder);
        }
        else
        {
            // General help query
            Log("Routing to general help query...");
            yield return HandleGeneralQuery(userText, resultHolder);
        }
        
        // Get response with fallback
        string response = resultHolder.Value;
        if (string.IsNullOrEmpty(response))
        {
            response = demoConfig != null ? demoConfig.errorMessage : "Sorry, I encountered an issue.";
        }

        _isProcessing = false;

        if (!string.IsNullOrEmpty(response))
        {
            yield return SpeakAndWait(response);
        }

        // Return to previous state or move to next step
        if (previousState == DemoState.StepAwaitingUserQuestion)
        {
            // Continue to next step
            SetState(DemoState.RunningSteps);
        }
        else
        {
            SetState(previousState == DemoState.Completed ? DemoState.IdleQnA : previousState);
        }
    }

    private IEnumerator HandleVisionQuery(string userText, CoroutineResult<string> result)
    {
        if (!enableVisionQueries || xrCamera == null)
        {
            result.Value = "I'm sorry, vision queries are not available right now.";
            yield break;
        }

        // Capture current camera frame
        CaptureCameraFrame();

        if (_lastCameraFrame == null)
        {
            result.Value = "I couldn't capture the camera view. Please try again.";
            yield break;
        }

        var stepData = _currentStep?.ToStepData();
        var task = llmController.CheckImageAsync(_lastCameraFrame, userText, stepData, _lastDetections);
        
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"[Orchestrator] Vision query failed: {task.Exception.Message}");
            result.Value = "I had trouble analyzing the image. Please try again.";
        }
        else
        {
            result.Value = task.Result;
        }
    }

    private IEnumerator HandleConfirmationQuery(string userText, CoroutineResult<string> result)
    {
        if (_currentStep == null)
        {
            result.Value = "There's no active step to confirm right now.";
            yield break;
        }

        var task = llmController.ConfirmStepAsync(_currentStep.ToStepData(), userText);
        
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"[Orchestrator] Confirmation query failed: {task.Exception.Message}");
            result.Value = "I couldn't verify that. Please describe what you did.";
        }
        else
        {
            result.Value = task.Result;
        }
    }

    private IEnumerator HandleGeneralQuery(string userText, CoroutineResult<string> result)
    {
        var stepData = _currentStep?.ToStepData();
        var task = llmController.AskGeneralHelpAsync(userText, stepData);
        
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"[Orchestrator] General query failed: {task.Exception.Message}");
            result.Value = "I'm sorry, I couldn't process that question.";
        }
        else
        {
            result.Value = task.Result;
        }
    }

    #endregion

    #region Phase 5: Completion

    private IEnumerator CompleteDemo()
    {
        SetState(DemoState.Completed);

        string completionMsg = demoConfig != null ? demoConfig.completionMessage :
            "That's it! Let me know if you have any questions.";

        yield return SpeakAndWait(completionMsg);

        // Clear step context
        whisperBridge?.ClearCurrentStep();
        _currentStep = null;

        OnDemoCompleted?.Invoke();
        Log("Demo completed. Entering idle Q&A mode.");

        SetState(DemoState.IdleQnA);
    }

    #endregion

    #region TTS Helpers

    private IEnumerator SpeakAndWait(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        bubble.ShowAssistantText(text);
        
        if (ttsSpeaker != null)
        {
            _ttsCompletionSource = new TaskCompletionSource<bool>();
            ttsSpeaker.Speak(text);
            
            // Wait for TTS to finish
            yield return new WaitUntil(() => _ttsCompletionSource.Task.IsCompleted || !_isSpeaking);
            _ttsCompletionSource = null;
        }
        else
        {
            // Fallback: estimate duration
            bubble.SetSpeaking();
            _isSpeaking = true;
            float duration = Mathf.Clamp(text.Length * 0.06f, 2f, 15f);
            yield return new WaitForSeconds(duration);
            _isSpeaking = false;
            bubble.SetIdle();
        }
    }

    private void OnTTSStarted(TTSSpeaker speaker, string text)
    {
        _isSpeaking = true;
        bubble.SetSpeaking();
        Log("[TTS] Started speaking");
    }

    private void OnTTSFinished(TTSSpeaker speaker, string text)
    {
        _isSpeaking = false;
        bubble.SetIdle();
        _ttsCompletionSource?.TrySetResult(true);
        Log("[TTS] Finished speaking");
    }

    private void OnTTSCancelled(TTSSpeaker speaker, string text)
    {
        _isSpeaking = false;
        bubble.SetIdle();
        _ttsCompletionSource?.TrySetResult(false);
        Log("[TTS] Speaking cancelled");
    }

    #endregion

    #region Vision Helpers

    private void OnDetectionsReceived(List<Detection> detections)
    {
        _lastDetections = detections ?? new List<Detection>();
        Log($"Received {_lastDetections.Count} detections");
    }

    private void OnVoiceActivityChanged(bool isSpeaking)
    {
        // Update bubble state if not already in a controlled state
        if (!_isSpeaking && !_isProcessing)
        {
            if (isSpeaking)
                bubble.SetListening();
            else
                bubble.SetIdle();
        }
    }

    private void CaptureCameraFrame()
    {
        if (xrCamera == null)
        {
            Debug.LogWarning("[Orchestrator] No XR camera assigned for frame capture.");
            return;
        }

        try
        {
            // Get render texture from camera or create one
            RenderTexture rt = cameraRenderTexture;
            bool createdTemp = false;

            if (rt == null)
            {
                rt = RenderTexture.GetTemporary(640, 480, 24);
                createdTemp = true;
            }

            // Render camera to texture
            var previousTarget = xrCamera.targetTexture;
            xrCamera.targetTexture = rt;
            xrCamera.Render();
            xrCamera.targetTexture = previousTarget;

            // Read pixels to Texture2D
            if (_lastCameraFrame == null)
            {
                _lastCameraFrame = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            }
            else if (_lastCameraFrame.width != rt.width || _lastCameraFrame.height != rt.height)
            {
                Destroy(_lastCameraFrame);
                _lastCameraFrame = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            }

            RenderTexture.active = rt;
            _lastCameraFrame.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            _lastCameraFrame.Apply();
            RenderTexture.active = null;

            if (createdTemp)
            {
                RenderTexture.ReleaseTemporary(rt);
            }

            Log("Camera frame captured successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Orchestrator] Failed to capture camera frame: {e.Message}");
            _lastCameraFrame = null;
        }
    }

    /// <summary>
    /// Highlight detections matching a specific YOLO class
    /// </summary>
    public void HighlightYOLOClass(string className)
    {
        if (string.IsNullOrEmpty(className) || _lastDetections == null)
            return;

        var matchingDetections = _lastDetections.FindAll(d => 
            d.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase));

        if (matchingDetections.Count > 0)
        {
            Log($"Found {matchingDetections.Count} detection(s) for class '{className}'");
            // The Detector already handles visualization via SpawnLabels
            // Additional highlighting logic can be added here if needed
        }
        else
        {
            Log($"No detections found for class '{className}'");
        }
    }

    #endregion

    #region State Management

    private void SetState(DemoState newState)
    {
        if (_currentState == newState) return;

        var oldState = _currentState;
        _currentState = newState;
        
        Log($"State: {oldState} → {newState}");
        OnStateChanged?.Invoke(oldState, newState);
    }

    #endregion

    #region Utility

    private bool ContainsAny(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (text.Contains(keyword))
                return true;
        }
        return false;
    }

    private void Log(string message)
    {
        if (debugLogging)
        {
            Debug.Log($"[Orchestrator] {message}");
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Manually trigger a response (for testing)
    /// </summary>
    public void TriggerTestResponse(string text)
    {
        StartCoroutine(SpeakAndWait(text));
    }

    /// <summary>
    /// Stop all ongoing operations
    /// </summary>
    public void Stop()
    {
        StopAllCoroutines();
        ttsSpeaker?.Stop();
        _isSpeaking = false;
        _isProcessing = false;
        bubble?.SetIdle();
    }

    /// <summary>
    /// Manually advance to next step
    /// </summary>
    public void AdvanceStep()
    {
        if (_currentState == DemoState.StepAwaitingUserQuestion)
        {
            SetState(DemoState.RunningSteps);
        }
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Helper class to capture results from coroutines
    /// </summary>
    private class CoroutineResult<T>
    {
        public T Value { get; set; }
    }

    #endregion
}

