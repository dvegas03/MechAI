using UnityEngine;
using TMPro;

public class AiBubbleController : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    [Header("UI References (Optional)")]
    [SerializeField]
    private TextMeshProUGUI userTextLabel;
    [SerializeField]
    private TextMeshProUGUI assistantTextLabel;

    [Header("Amplitude Animation (Optional)")]
    [SerializeField]
    private Transform scaleTarget;
    [SerializeField]
    private float minScale = 1f;
    [SerializeField]
    private float maxScale = 1.3f;
    [SerializeField]
    private float scaleSmoothing = 10f;

    private float _targetScale = 1f;
    private float _currentScale = 1f;

    private void Update()
    {
        // Smooth scale animation based on amplitude
        if (scaleTarget != null)
        {
            _currentScale = Mathf.Lerp(_currentScale, _targetScale, Time.deltaTime * scaleSmoothing);
            scaleTarget.localScale = Vector3.one * _currentScale;
        }
    }

    public void SetIdle()
    {
        if (animator == null) return;
        animator.SetBool("isIdle", true);
        animator.SetBool("isListening", false);
        animator.SetBool("isSpeaking", false);
        animator.SetBool("isThinking", false);
        
        _targetScale = minScale;
    }

    public void SetListening()
    {
        if (animator == null) return;
        animator.SetBool("isIdle", false);
        animator.SetBool("isListening", true);
        animator.SetBool("isSpeaking", false);
        animator.SetBool("isThinking", false);
    }

    public void SetSpeaking()
    {
        if (animator == null) return;
        animator.SetBool("isIdle", false);
        animator.SetBool("isListening", false);
        animator.SetBool("isSpeaking", true);
        animator.SetBool("isThinking", false);
    }

    /// <summary>
    /// Set the "thinking" state while waiting for LLM response
    /// </summary>
    public void SetThinking()
    {
        if (animator == null) return;
        animator.SetBool("isIdle", false);
        animator.SetBool("isListening", false);
        animator.SetBool("isSpeaking", false);
        animator.SetBool("isThinking", true);
    }

    public void ShowUserText(string text)
    {
        if (userTextLabel != null) userTextLabel.text = text;
    }

    public void ShowAssistantText(string text)
    {
        if (assistantTextLabel != null) assistantTextLabel.text = text;
    }

    /// <summary>
    /// Update visual feedback based on audio amplitude (0-1 range)
    /// Used for lip-sync or pulse animation while TTS is speaking
    /// </summary>
    /// <param name="amplitude">Normalized amplitude value (0-1)</param>
    public void UpdateFromMicLevel(float amplitude)
    {
        // Update scale target based on amplitude
        _targetScale = Mathf.Lerp(minScale, maxScale, amplitude);
        
        // Optionally set animator float for more complex animations
        if (animator != null)
        {
            animator.SetFloat("speakingAmplitude", amplitude);
        }
    }

    /// <summary>
    /// Clear all displayed text
    /// </summary>
    public void ClearText()
    {
        if (userTextLabel != null) userTextLabel.text = "";
        if (assistantTextLabel != null) assistantTextLabel.text = "";
    }
}
