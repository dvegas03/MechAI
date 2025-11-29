using UnityEngine;
using Whisper;
using Whisper.Utils;
using System;
using System.Threading.Tasks;
using Meta.WitAi.TTS.Utilities;

public class WhisperToLLMBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WhisperManager whisper;
    [SerializeField] private MicrophoneRecord microphone;
    [SerializeField] private LLMController llmController;
    [SerializeField] private AiBubbleController bubble;
    
    [Header("Text-to-Speech")]
    [SerializeField] private TTSSpeaker ttsSpeaker;
    [SerializeField] private bool enableAmplitudeAnimation = true;
    [SerializeField] private float amplitudeUpdateInterval = 0.05f;

    [Header("Orchestrator Mode")]
    [Tooltip("When true, user speech is forwarded to OnUserSpeechFinalized event instead of internal LLM processing")]
    [SerializeField] private bool orchestratorMode = false;

    /// <summary>
    /// Event fired when user finishes speaking. Subscribe to this for external orchestration.
    /// </summary>
    public event Action<string> OnUserSpeechFinalized;

    /// <summary>
    /// Event fired when VAD detects speech start/stop
    /// </summary>
    public event Action<bool> OnVoiceActivityChanged;

    /// <summary>
    /// Event fired during speech with partial transcript
    /// </summary>
    public event Action<string> OnPartialTranscript;

    public StepData? CurrentStep { get; private set; }

    private WhisperStream _stream;
    private bool _isAssistantSpeaking;
    private bool _isProcessingRequest;
    private Coroutine _amplitudeCoroutine;

    #region Properties

    public bool IsSpeaking => _isAssistantSpeaking;
    public bool IsProcessing => _isProcessingRequest;
    public TTSSpeaker TTS => ttsSpeaker;
    public AiBubbleController Bubble => bubble;
    public LLMController LLM => llmController;
    public MicrophoneRecord Microphone => microphone;

    /// <summary>
    /// Enable or disable orchestrator mode at runtime
    /// </summary>
    public bool OrchestratorMode
    {
        get => orchestratorMode;
        set => orchestratorMode = value;
    }

    #endregion

    private async void OnEnable()
    {
        if (whisper == null || microphone == null || llmController == null || bubble == null)
        {
            Debug.LogError("WhisperToLLMBridge is missing references.");
            return;
        }

        if (ttsSpeaker == null)
        {
            Debug.LogWarning("TTSSpeaker not assigned. Voice output will be disabled.");
        }

        bubble.SetIdle();

        // Subscribe to Whisper events
        microphone.OnVadChanged += OnVadChanged;

        // Subscribe to TTS events
        if (ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnStartSpeaking.AddListener(OnTTSStartSpeaking);
            ttsSpeaker.Events.OnFinishedSpeaking.AddListener(OnTTSFinishedSpeaking);
            ttsSpeaker.Events.OnCancelledSpeaking.AddListener(OnTTSCancelled);
        }

        await InitializeWhisperAndStream();
    }

    private void OnDisable()
    {
        // Unsubscribe from Whisper events
        if (microphone != null)
        {
            microphone.OnVadChanged -= OnVadChanged;
        }

        // Unsubscribe from TTS events
        if (ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnStartSpeaking.RemoveListener(OnTTSStartSpeaking);
            ttsSpeaker.Events.OnFinishedSpeaking.RemoveListener(OnTTSFinishedSpeaking);
            ttsSpeaker.Events.OnCancelledSpeaking.RemoveListener(OnTTSCancelled);
        }

        // Stop amplitude monitoring
        StopAmplitudeMonitoring();

        // Clean up Whisper stream
        if (_stream != null)
        {
            _stream.OnSegmentFinished -= OnSegmentFinished;
            _stream.OnSegmentUpdated -= OnSegmentUpdated;
            _stream.StopStream();
            _stream = null;
        }

        if (microphone != null && microphone.IsRecording)
            microphone.StopRecord();
    }

    private async Task InitializeWhisperAndStream()
    {
        try
        {
            if (!whisper.IsLoaded && !whisper.IsLoading)
                await whisper.InitModel();

            microphone.StartRecord();

            _stream = await whisper.CreateStream(microphone);

            _stream.OnSegmentFinished += OnSegmentFinished;
            _stream.OnSegmentUpdated += OnSegmentUpdated;

            _stream.StartStream();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Whisper initialization failed: {e.Message}");
            bubble.ShowAssistantText("Voice input is unavailable.");
        }
    }

    public void SetCurrentStep(string procedureTitle, string stepTitle, string stepBody)
    {
        CurrentStep = new StepData
        {
            procedureTitle = procedureTitle,
            stepTitle = stepTitle,
            stepBody = stepBody
        };
    }

    public void ClearCurrentStep()
    {
        CurrentStep = null;
    }

    #region Whisper Event Handlers

    private void OnVadChanged(bool isSpeech)
    {
        // Forward VAD event
        OnVoiceActivityChanged?.Invoke(isSpeech);

        // Ignore voice activity while assistant is speaking or processing
        if (_isAssistantSpeaking || _isProcessingRequest) return;

        if (isSpeech)
            bubble.SetListening();
        else
            bubble.SetIdle();
    }

    private void OnSegmentUpdated(WhisperResult segment)
    {
        // Forward partial transcript
        OnPartialTranscript?.Invoke(segment.Result);

        // Don't update UI while assistant is speaking
        if (_isAssistantSpeaking || _isProcessingRequest) return;
        
        bubble.ShowUserText(segment.Result);
    }

    private void OnSegmentFinished(WhisperResult segment)
    {
        // Ignore new input while speaking or processing
        if (_isAssistantSpeaking || _isProcessingRequest) return;

        string userText = segment.Result.Trim();
        if (userText.Length < 3) return;

        bubble.ShowUserText(userText);

        // Fire event for external listeners (orchestrator)
        OnUserSpeechFinalized?.Invoke(userText);
        
        // Only process internally if not in orchestrator mode
        if (!orchestratorMode)
        {
            _ = ProcessUserRequest(userText);
        }
    }

    #endregion

    #region TTS Event Handlers

    private void OnTTSStartSpeaking(TTSSpeaker speaker, string text)
    {
        _isAssistantSpeaking = true;
        bubble.SetSpeaking();
        
        // Start amplitude monitoring for visual feedback
        if (enableAmplitudeAnimation)
        {
            StartAmplitudeMonitoring();
        }
        
        Debug.Log("[TTS] Started speaking");
    }

    private void OnTTSFinishedSpeaking(TTSSpeaker speaker, string text)
    {
        StopAmplitudeMonitoring();
        
        _isAssistantSpeaking = false;
        _isProcessingRequest = false;
        bubble.SetIdle();
        
        Debug.Log("[TTS] Finished speaking");
    }

    private void OnTTSCancelled(TTSSpeaker speaker, string text)
    {
        StopAmplitudeMonitoring();
        
        _isAssistantSpeaking = false;
        _isProcessingRequest = false;
        bubble.SetIdle();
        
        Debug.Log("[TTS] Speaking cancelled");
    }

    #endregion

    #region Request Processing

    private async Task ProcessUserRequest(string userText)
    {
        // Prevent concurrent requests
        if (_isProcessingRequest || _isAssistantSpeaking)
        {
            Debug.LogWarning("Ignoring request - already processing or speaking");
            return;
        }

        _isProcessingRequest = true;
        bubble.SetThinking();

        try
        {
            // Get response from LLM
            string response = await llmController.AskGeneralHelpAsync(userText, CurrentStep);

            // Show text in bubble
            bubble.ShowAssistantText(response);

            // Speak the response via TTS
            if (ttsSpeaker != null && !string.IsNullOrEmpty(response))
            {
                ttsSpeaker.Speak(response);
            }
            else
            {
                // No TTS available - use fallback behavior
                _isAssistantSpeaking = true;
                bubble.SetSpeaking();
                
                float speakingDuration = Mathf.Clamp(response.Length * 0.05f, 2f, 10f);
                await Task.Delay((int)(speakingDuration * 1000));
                
                _isAssistantSpeaking = false;
                _isProcessingRequest = false;
                bubble.SetIdle();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing request: {e.Message}");
            _isProcessingRequest = false;
            _isAssistantSpeaking = false;
            bubble.SetIdle();
            bubble.ShowAssistantText("Sorry, I encountered an error.");
        }
    }

    #endregion

    #region Amplitude Animation

    private void StartAmplitudeMonitoring()
    {
        if (_amplitudeCoroutine != null)
        {
            StopCoroutine(_amplitudeCoroutine);
        }
        _amplitudeCoroutine = StartCoroutine(MonitorAmplitudeCoroutine());
    }

    private void StopAmplitudeMonitoring()
    {
        if (_amplitudeCoroutine != null)
        {
            StopCoroutine(_amplitudeCoroutine);
            _amplitudeCoroutine = null;
        }
        
        bubble.UpdateFromMicLevel(0f);
    }

    private System.Collections.IEnumerator MonitorAmplitudeCoroutine()
    {
        var wait = new WaitForSeconds(amplitudeUpdateInterval);
        
        while (_isAssistantSpeaking && ttsSpeaker != null)
        {
            float amplitude = GetCurrentAmplitude();
            bubble.UpdateFromMicLevel(amplitude);
            yield return wait;
        }
        
        bubble.UpdateFromMicLevel(0f);
    }

    private float GetCurrentAmplitude()
    {
        if (ttsSpeaker == null) return 0f;
        
        var audioSource = ttsSpeaker.AudioSource;
        if (audioSource == null || audioSource.clip == null || !audioSource.isPlaying)
            return 0f;

        float[] samples = new float[256];
        audioSource.GetOutputData(samples, 0);

        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }
        
        float rms = Mathf.Sqrt(sum / samples.Length);
        return Mathf.Clamp01(rms * 4f);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Stop any ongoing TTS playback
    /// </summary>
    public void StopSpeaking()
    {
        if (ttsSpeaker != null && _isAssistantSpeaking)
        {
            ttsSpeaker.Stop();
        }
    }

    /// <summary>
    /// Set processing state (for orchestrator use)
    /// </summary>
    public void SetProcessingState(bool isProcessing)
    {
        _isProcessingRequest = isProcessing;
    }

    /// <summary>
    /// Set speaking state (for orchestrator use)
    /// </summary>
    public void SetSpeakingState(bool isSpeaking)
    {
        _isAssistantSpeaking = isSpeaking;
    }

    /// <summary>
    /// Speak text via TTS (for orchestrator use)
    /// </summary>
    public void SpeakText(string text)
    {
        if (ttsSpeaker != null && !string.IsNullOrEmpty(text))
        {
            bubble.ShowAssistantText(text);
            ttsSpeaker.Speak(text);
        }
    }

    #endregion
}
