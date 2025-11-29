using NUnit.Framework;
using UnityEngine;
using System.Threading.Tasks;

public class Test_LLMControllerPrompts
{
    [Test]
    public async Task AskGeneralHelpAsync_WithStep_BuildsCorrectPrompt()
    {
        var go = new GameObject();
        var controller = go.AddComponent<LLMController>();
        var mockService = new MockLLMService();
        var llmServiceField = typeof(LLMController).GetField("llmService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        llmServiceField.SetValue(controller, mockService);

        var step = new StepData { procedureTitle = "Oil Change", stepTitle = "Drain Oil", stepBody = "Locate drain plug." };
        await controller.AskGeneralHelpAsync("where is it?", step);

        var prompt = mockService.LastInput as string;
        Assert.IsTrue(prompt.Contains("Current procedure: Oil Change"));
        Assert.IsTrue(prompt.Contains("Current step: Drain Oil"));
        Assert.IsTrue(prompt.Contains("User question: where is it?"));

        Object.DestroyImmediate(go);
    }
}