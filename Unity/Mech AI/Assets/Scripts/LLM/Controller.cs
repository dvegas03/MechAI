using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// Placeholder for application-specific data structures.
public struct StepData { public string procedureTitle; public string stepTitle; public string stepBody; }

public class LLMController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float minImageCallIntervalSeconds = 30.0f;

    private LLMService llmService;
    private float lastImageCallTime = -1f;

    private const string SystemPrompt =
        "You are a car maintenance assistant for a step-by-step mechanic XR app. " +
        "You only rely on the step description and user's description. " +
        "You never invent new repair procedures. " +
        "You keep answers short, clear, and specific to the current step. " +
        "You do not override the app's instructions. " +
        "If you are not sure, you say you are not sure. " +
        "You never give medical advice or high-risk safety instructions. " +
        "You treat all your answers as suggestions, not commands.";

    private void Awake()
    {
        llmService = new LLMService();
    }

    public async Task<string> AskGeneralHelpAsync(string userQuestion, StepData? currentStep = null)
    {
        var promptBuilder = new StringBuilder();

        if (currentStep.HasValue)
        {
            promptBuilder.AppendLine($"Current procedure: {currentStep.Value.procedureTitle}");
            promptBuilder.AppendLine($"Current step: {currentStep.Value.stepTitle}");
            promptBuilder.AppendLine($"Step description: {currentStep.Value.stepBody}");
        }
        else
        {
            promptBuilder.AppendLine("The user is working on general car maintenance.");
        }

        promptBuilder.AppendLine($"User question: {userQuestion}");
        promptBuilder.Append("Answer in 3–5 sentences maximum, using simple language.");

        var response = await llmService.SendTextRequestAsync(SystemPrompt, promptBuilder.ToString());
        return ExtractTextFromResponse(response);
    }

    public async Task<string> ConfirmStepAsync(StepData currentStep, string userDescription = "")
    {
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("You are checking if a car maintenance step is reasonably complete based on the user’s description.");
        promptBuilder.AppendLine($"Step title: {currentStep.stepTitle}");
        promptBuilder.AppendLine($"Step description: {currentStep.stepBody}");
        promptBuilder.AppendLine($"User description of what they did: {(string.IsNullOrEmpty(userDescription) ? "No description provided." : userDescription)}");
        promptBuilder.Append("Respond in one short sentence starting with either ‘OK:’ or ‘WAIT:’.");

        var response = await llmService.SendTextRequestAsync(SystemPrompt, promptBuilder.ToString());
        return ExtractTextFromResponse(response);
    }

    public async Task<string> CheckImageAsync(Texture2D image, string userQuestion = "", StepData? currentStep = null, List<Detection> detections = null)
    {
        if (Time.time - lastImageCallTime < minImageCallIntervalSeconds)
        {
            return "Image check is temporarily disabled to reduce costs. Please try again later.";
        }

        lastImageCallTime = Time.time;

        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("You see an image from a car maintenance scene.");

        if (currentStep.HasValue)
        {
            promptBuilder.AppendLine($"Current procedure: {currentStep.Value.procedureTitle}");
            promptBuilder.AppendLine($"Current step: {currentStep.Value.stepTitle}");
            promptBuilder.AppendLine($"Step description: {currentStep.Value.stepBody}");
        }

        string detectedObjects = "none";
        if (detections != null && detections.Any())
        {
            detectedObjects = string.Join(", ", detections.Select(d => d.ClassName).Distinct());
        }
        promptBuilder.AppendLine($"Detected tools/objects (from vision model, may be incomplete): {detectedObjects}");

        string question = string.IsNullOrEmpty(userQuestion)
            ? "Check if this scene looks correct for this step."
            : userQuestion;
        promptBuilder.AppendLine($"User question: {question}");
        promptBuilder.Append("Answer in 2–4 sentences. If you are unsure, clearly say you are unsure.");

        byte[] imageBytes = image.EncodeToPNG();

        var response = await llmService.SendTextAndImageRequestAsync(SystemPrompt, promptBuilder.ToString(), imageBytes);
        return ExtractTextFromResponse(response);
    }

    private string ExtractTextFromResponse(OpenAIResponse response)
    {
        if (response?.output == null || !response.output.Any())
        {
            return "I’m unable to respond right now.";
        }

        var messageContent = response.output?.FirstOrDefault(o => o.type == "message")?.content;
        var textPart = messageContent?.FirstOrDefault(c => c.type == "output_text");

        return textPart?.text?.Trim() ?? "I received a response, but it was empty.";
    }
}