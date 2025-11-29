using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Whisper;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Integration tests for the WhisperToLLM pipeline.
/// Tests the flow from voice input detection through LLM response.
/// Note: Animator state tests are in Unit tests since they require Editor-only AnimatorController.
/// </summary>
public class Test_WhisperToLLM_Pipeline
{
    private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    [UnityTest]
    public IEnumerator LLMController_AskGeneralHelp_ReturnsResponse()
    {
        LogAssert.ignoreFailingMessages = true; // Ignore OPENAI_API_KEY warning
        
        var llmGO = new GameObject("TestLLM");
        var llmController = llmGO.AddComponent<LLMController>();
        
        // Set mock AFTER Awake has run (Awake creates a real LLMService)
        var mockLlmService = new MockLLMService();
        mockLlmService.MockResponse = CreateMockResponse("This is the AI response.");
        SetPrivateField(llmController, "llmService", mockLlmService);

        yield return null;

        // Call the async method
        string response = null;
        var task = llmController.AskGeneralHelpAsync("What is this?", null);
        
        // Wait for task to complete
        while (!task.IsCompleted)
        {
            yield return null;
        }

        response = task.Result;

        Assert.IsNotNull(response, "Response should not be null");
        Assert.AreEqual("This is the AI response.", response, "Response should match mock");
        Assert.IsNotNull(mockLlmService.LastInput, "LLM service should have received input");

        Object.DestroyImmediate(llmGO);
        LogAssert.ignoreFailingMessages = false;
    }

    [UnityTest]
    public IEnumerator LLMController_WithStepContext_IncludesStepInPrompt()
    {
        LogAssert.ignoreFailingMessages = true;
        
        var llmGO = new GameObject("TestLLM");
        var llmController = llmGO.AddComponent<LLMController>();
        
        // Set mock AFTER Awake has run
        var mockLlmService = new MockLLMService();
        mockLlmService.MockResponse = CreateMockResponse("Step guidance response");
        SetPrivateField(llmController, "llmService", mockLlmService);

        yield return null;

        var step = new StepData
        {
            procedureTitle = "Oil Change",
            stepTitle = "Drain Oil",
            stepBody = "Locate and remove the drain plug."
        };

        var task = llmController.AskGeneralHelpAsync("Where is the drain plug?", step);
        
        while (!task.IsCompleted)
        {
            yield return null;
        }

        var sentInput = mockLlmService.LastInput as string;
        Assert.IsNotNull(sentInput, "Input should have been sent to LLM");
        Assert.IsTrue(sentInput.Contains("Oil Change"), "Prompt should include procedure title");
        Assert.IsTrue(sentInput.Contains("Drain Oil"), "Prompt should include step title");
        Assert.IsTrue(sentInput.Contains("drain plug"), "Prompt should include step body");
        Assert.IsTrue(sentInput.Contains("Where is the drain plug"), "Prompt should include user question");

        Object.DestroyImmediate(llmGO);
        LogAssert.ignoreFailingMessages = false;
    }

    [UnityTest]
    public IEnumerator LLMController_ConfirmStep_ReturnsValidation()
    {
        LogAssert.ignoreFailingMessages = true;
        
        var llmGO = new GameObject("TestLLM");
        var llmController = llmGO.AddComponent<LLMController>();
        
        // Set mock AFTER Awake has run
        var mockLlmService = new MockLLMService();
        mockLlmService.MockResponse = CreateMockResponse("OK: The step appears to be complete.");
        SetPrivateField(llmController, "llmService", mockLlmService);

        yield return null;

        var step = new StepData
        {
            procedureTitle = "Oil Change",
            stepTitle = "Drain Oil",
            stepBody = "Remove the drain plug and let oil drain."
        };

        var task = llmController.ConfirmStepAsync(step, "I removed the plug and the oil is draining");
        
        while (!task.IsCompleted)
        {
            yield return null;
        }

        var response = task.Result;
        Assert.IsNotNull(response, "Response should not be null");
        Assert.IsTrue(response.StartsWith("OK:"), "Response should start with OK: for confirmed step");

        Object.DestroyImmediate(llmGO);
        LogAssert.ignoreFailingMessages = false;
    }

    [UnityTest]
    public IEnumerator MockMicrophone_VadEvents_FireCorrectly()
    {
        var micGO = new GameObject("TestMic");
        var mockMic = micGO.AddComponent<MockMicrophoneRecord>();

        bool vadEventFired = false;
        bool lastVadState = false;

        mockMic.OnVadChanged += (isSpeech) =>
        {
            vadEventFired = true;
            lastVadState = isSpeech;
        };

        yield return null;

        // Test speech start
        mockMic.TriggerVadChanged(true);
        Assert.IsTrue(vadEventFired, "VAD event should have fired");
        Assert.IsTrue(lastVadState, "VAD state should be true (speaking)");

        // Reset and test speech stop
        vadEventFired = false;
        mockMic.TriggerVadChanged(false);
        Assert.IsTrue(vadEventFired, "VAD event should have fired again");
        Assert.IsFalse(lastVadState, "VAD state should be false (not speaking)");

        Object.DestroyImmediate(micGO);
    }

    [UnityTest]
    public IEnumerator MockWhisperStream_SegmentFinished_FiresWithCorrectData()
    {
        var mockStream = new MockWhisperStream();

        WhisperResult receivedResult = null;
        mockStream.OnSegmentFinished += (result) =>
        {
            receivedResult = result;
        };

        yield return null;

        mockStream.TriggerSegmentFinished("Hello, this is a test transcription.");

        Assert.IsNotNull(receivedResult, "Should have received a WhisperResult");
        Assert.AreEqual("Hello, this is a test transcription.", receivedResult.Result, "Transcription text should match");

        yield return null;
    }

    [UnityTest]
    public IEnumerator LLMController_HandlesMissingResponse_Gracefully()
    {
        LogAssert.ignoreFailingMessages = true;
        
        var llmGO = new GameObject("TestLLM");
        var llmController = llmGO.AddComponent<LLMController>();
        
        // Set mock AFTER Awake has run
        var mockLlmService = new MockLLMService();
        mockLlmService.MockResponse = null; // Simulate failed API call
        SetPrivateField(llmController, "llmService", mockLlmService);

        yield return null;

        var task = llmController.AskGeneralHelpAsync("Test question", null);
        
        while (!task.IsCompleted)
        {
            yield return null;
        }

        var response = task.Result;
        Assert.IsNotNull(response, "Should return fallback response, not null");
        Assert.IsTrue(response.Contains("unable") || response.Contains("empty"), 
            "Should indicate inability to respond");

        Object.DestroyImmediate(llmGO);
        LogAssert.ignoreFailingMessages = false;
    }

    private OpenAIResponse CreateMockResponse(string text)
    {
        return new OpenAIResponse
        {
            output = new List<ResponseItem>
            {
                new ResponseItem
                {
                    type = "message",
                    content = new List<ResponseContent>
                    {
                        new ResponseContent { type = "output_text", text = text }
                    }
                }
            }
        };
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, NonPublicInstance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogError($"Field '{fieldName}' not found on {obj.GetType().Name}");
        }
    }
}
