using NUnit.Framework;
using UnityEngine;
using System;
using System.Reflection;

/// <summary>
/// Tests for the orchestrator-related additions to WhisperToLLMBridge
/// </summary>
public class Test_WhisperToLLMBridgeOrchestrator
{
    private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    [Test]
    public void WhisperBridge_OrchestratorMode_DefaultsToFalse()
    {
        var go = new GameObject();
        go.SetActive(false);
        var bridge = go.AddComponent<WhisperToLLMBridge>();

        Assert.IsFalse(bridge.OrchestratorMode);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void WhisperBridge_OrchestratorMode_CanBeSet()
    {
        var go = new GameObject();
        go.SetActive(false);
        var bridge = go.AddComponent<WhisperToLLMBridge>();

        bridge.OrchestratorMode = true;
        Assert.IsTrue(bridge.OrchestratorMode);

        bridge.OrchestratorMode = false;
        Assert.IsFalse(bridge.OrchestratorMode);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void WhisperBridge_OnUserSpeechFinalized_EventCanBeSubscribed()
    {
        var go = new GameObject();
        go.SetActive(false);
        var bridge = go.AddComponent<WhisperToLLMBridge>();

        bool eventFired = false;
        string receivedText = null;

        bridge.OnUserSpeechFinalized += (text) =>
        {
            eventFired = true;
            receivedText = text;
        };

        // Manually invoke the event through the private field
        var eventField = typeof(WhisperToLLMBridge).GetField("OnUserSpeechFinalized", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        var eventDelegate = eventField?.GetValue(bridge) as Action<string>;
        eventDelegate?.Invoke("Test utterance");

        Assert.IsTrue(eventFired, "Event should have fired");
        Assert.AreEqual("Test utterance", receivedText);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void WhisperBridge_OnVoiceActivityChanged_EventCanBeSubscribed()
    {
        var go = new GameObject();
        go.SetActive(false);
        var bridge = go.AddComponent<WhisperToLLMBridge>();

        bool eventFired = false;
        bool? receivedState = null;

        bridge.OnVoiceActivityChanged += (isSpeaking) =>
        {
            eventFired = true;
            receivedState = isSpeaking;
        };

        var eventField = typeof(WhisperToLLMBridge).GetField("OnVoiceActivityChanged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        var eventDelegate = eventField?.GetValue(bridge) as Action<bool>;
        eventDelegate?.Invoke(true);

        Assert.IsTrue(eventFired);
        Assert.IsTrue(receivedState.Value);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void WhisperBridge_SetProcessingState_UpdatesState()
    {
        var go = new GameObject();
        go.SetActive(false);
        var bridge = go.AddComponent<WhisperToLLMBridge>();

        Assert.IsFalse(bridge.IsProcessing);

        bridge.SetProcessingState(true);
        Assert.IsTrue(bridge.IsProcessing);

        bridge.SetProcessingState(false);
        Assert.IsFalse(bridge.IsProcessing);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void WhisperBridge_SetSpeakingState_UpdatesState()
    {
        var go = new GameObject();
        go.SetActive(false);
        var bridge = go.AddComponent<WhisperToLLMBridge>();

        Assert.IsFalse(bridge.IsSpeaking);

        bridge.SetSpeakingState(true);
        Assert.IsTrue(bridge.IsSpeaking);

        bridge.SetSpeakingState(false);
        Assert.IsFalse(bridge.IsSpeaking);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void WhisperBridge_SetCurrentStep_SetsStepData()
    {
        var go = new GameObject();
        go.SetActive(false);
        var bridge = go.AddComponent<WhisperToLLMBridge>();

        Assert.IsFalse(bridge.CurrentStep.HasValue);

        bridge.SetCurrentStep("Oil Change", "Drain Oil", "Remove the drain plug.");

        Assert.IsTrue(bridge.CurrentStep.HasValue);
        Assert.AreEqual("Oil Change", bridge.CurrentStep.Value.procedureTitle);
        Assert.AreEqual("Drain Oil", bridge.CurrentStep.Value.stepTitle);
        Assert.AreEqual("Remove the drain plug.", bridge.CurrentStep.Value.stepBody);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void WhisperBridge_ClearCurrentStep_ClearsStepData()
    {
        var go = new GameObject();
        go.SetActive(false);
        var bridge = go.AddComponent<WhisperToLLMBridge>();

        bridge.SetCurrentStep("Test", "Test Step", "Test Body");
        Assert.IsTrue(bridge.CurrentStep.HasValue);

        bridge.ClearCurrentStep();
        Assert.IsFalse(bridge.CurrentStep.HasValue);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void WhisperBridge_Properties_ExposeComponents()
    {
        var go = new GameObject();
        go.SetActive(false);
        var bridge = go.AddComponent<WhisperToLLMBridge>();

        // These should not throw, just return null when not assigned
        Assert.DoesNotThrow(() => { var _ = bridge.TTS; });
        Assert.DoesNotThrow(() => { var _ = bridge.Bubble; });
        Assert.DoesNotThrow(() => { var _ = bridge.LLM; });
        Assert.DoesNotThrow(() => { var _ = bridge.Microphone; });

        UnityEngine.Object.DestroyImmediate(go);
    }
}

