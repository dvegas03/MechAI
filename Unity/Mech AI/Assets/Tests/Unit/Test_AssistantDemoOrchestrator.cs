using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

public class Test_AssistantDemoOrchestrator
{
    private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    #region State Machine Tests

    [Test]
    public void Orchestrator_InitialState_IsInitializing()
    {
        var go = new GameObject();
        go.SetActive(false);
        var orchestrator = go.AddComponent<AssistantDemoOrchestrator>();

        Assert.AreEqual(DemoState.Initializing, orchestrator.CurrentState);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Orchestrator_SetState_ChangesState()
    {
        var go = new GameObject();
        go.SetActive(false);
        var orchestrator = go.AddComponent<AssistantDemoOrchestrator>();

        var setStateMethod = typeof(AssistantDemoOrchestrator).GetMethod("SetState", NonPublicInstance);
        
        setStateMethod.Invoke(orchestrator, new object[] { DemoState.Greeting });
        Assert.AreEqual(DemoState.Greeting, orchestrator.CurrentState);

        setStateMethod.Invoke(orchestrator, new object[] { DemoState.WaitingForFirstUserInput });
        Assert.AreEqual(DemoState.WaitingForFirstUserInput, orchestrator.CurrentState);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Orchestrator_SetState_FiresEvent()
    {
        var go = new GameObject();
        go.SetActive(false);
        var orchestrator = go.AddComponent<AssistantDemoOrchestrator>();

        DemoState? oldState = null;
        DemoState? newState = null;
        orchestrator.OnStateChanged += (old, @new) =>
        {
            oldState = old;
            newState = @new;
        };

        var setStateMethod = typeof(AssistantDemoOrchestrator).GetMethod("SetState", NonPublicInstance);
        setStateMethod.Invoke(orchestrator, new object[] { DemoState.Greeting });

        Assert.AreEqual(DemoState.Initializing, oldState);
        Assert.AreEqual(DemoState.Greeting, newState);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Orchestrator_SetState_DoesNotFireEventForSameState()
    {
        var go = new GameObject();
        go.SetActive(false);
        var orchestrator = go.AddComponent<AssistantDemoOrchestrator>();

        int eventCount = 0;
        orchestrator.OnStateChanged += (old, @new) => eventCount++;

        var setStateMethod = typeof(AssistantDemoOrchestrator).GetMethod("SetState", NonPublicInstance);
        
        // Change to Greeting
        setStateMethod.Invoke(orchestrator, new object[] { DemoState.Greeting });
        Assert.AreEqual(1, eventCount);

        // Set same state again - should not fire
        setStateMethod.Invoke(orchestrator, new object[] { DemoState.Greeting });
        Assert.AreEqual(1, eventCount, "Event should not fire when state doesn't change");

        Object.DestroyImmediate(go);
    }

    #endregion

    #region Property Tests

    [Test]
    public void Orchestrator_CurrentStepIndex_StartsAtNegativeOne()
    {
        var go = new GameObject();
        go.SetActive(false);
        var orchestrator = go.AddComponent<AssistantDemoOrchestrator>();

        Assert.AreEqual(-1, orchestrator.CurrentStepIndex);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Orchestrator_CurrentStep_IsNullInitially()
    {
        var go = new GameObject();
        go.SetActive(false);
        var orchestrator = go.AddComponent<AssistantDemoOrchestrator>();

        Assert.IsNull(orchestrator.CurrentStep);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Orchestrator_LastDetections_InitializesEmpty()
    {
        var go = new GameObject();
        go.SetActive(false);
        var orchestrator = go.AddComponent<AssistantDemoOrchestrator>();

        Assert.IsNotNull(orchestrator.LastDetections);
        Assert.AreEqual(0, orchestrator.LastDetections.Count);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Orchestrator_IsSpeaking_IsFalseInitially()
    {
        var go = new GameObject();
        go.SetActive(false);
        var orchestrator = go.AddComponent<AssistantDemoOrchestrator>();

        Assert.IsFalse(orchestrator.IsSpeaking);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Orchestrator_IsProcessing_IsFalseInitially()
    {
        var go = new GameObject();
        go.SetActive(false);
        var orchestrator = go.AddComponent<AssistantDemoOrchestrator>();

        Assert.IsFalse(orchestrator.IsProcessing);

        Object.DestroyImmediate(go);
    }

    #endregion

    #region Utility Method Tests

    [Test]
    public void Orchestrator_ContainsAny_FindsKeywords()
    {
        var go = new GameObject();
        go.SetActive(false);
        var orchestrator = go.AddComponent<AssistantDemoOrchestrator>();

        var containsAnyMethod = typeof(AssistantDemoOrchestrator).GetMethod("ContainsAny", NonPublicInstance);

        // Test positive matches
        var result1 = (bool)containsAnyMethod.Invoke(orchestrator, new object[] { "what do you see here", new string[] { "what do you see", "identify" } });
        Assert.IsTrue(result1, "Should find 'what do you see'");

        var result2 = (bool)containsAnyMethod.Invoke(orchestrator, new object[] { "can you identify this", new string[] { "what do you see", "identify" } });
        Assert.IsTrue(result2, "Should find 'identify'");

        // Test negative match
        var result3 = (bool)containsAnyMethod.Invoke(orchestrator, new object[] { "hello world", new string[] { "what do you see", "identify" } });
        Assert.IsFalse(result3, "Should not find any keyword");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Orchestrator_HighlightYOLOClass_HandlesNullClassName()
    {
        var go = new GameObject();
        go.SetActive(false);
        var orchestrator = go.AddComponent<AssistantDemoOrchestrator>();

        // Should not throw
        Assert.DoesNotThrow(() => orchestrator.HighlightYOLOClass(null));
        Assert.DoesNotThrow(() => orchestrator.HighlightYOLOClass(""));

        Object.DestroyImmediate(go);
    }

    #endregion

    #region Public API Tests

    [Test]
    public void Orchestrator_Stop_ResetsSpeakingAndProcessing()
    {
        var go = new GameObject();
        go.SetActive(false);
        var orchestrator = go.AddComponent<AssistantDemoOrchestrator>();

        // Set internal state
        SetPrivateField(orchestrator, "_isSpeaking", true);
        SetPrivateField(orchestrator, "_isProcessing", true);

        orchestrator.Stop();

        Assert.IsFalse(orchestrator.IsSpeaking);
        Assert.IsFalse(orchestrator.IsProcessing);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Orchestrator_AdvanceStep_ChangesStateFromAwaiting()
    {
        var go = new GameObject();
        go.SetActive(false);
        var orchestrator = go.AddComponent<AssistantDemoOrchestrator>();

        // Set state to StepAwaitingUserQuestion
        var setStateMethod = typeof(AssistantDemoOrchestrator).GetMethod("SetState", NonPublicInstance);
        setStateMethod.Invoke(orchestrator, new object[] { DemoState.StepAwaitingUserQuestion });

        orchestrator.AdvanceStep();

        Assert.AreEqual(DemoState.RunningSteps, orchestrator.CurrentState);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Orchestrator_AdvanceStep_DoesNothingInOtherStates()
    {
        var go = new GameObject();
        go.SetActive(false);
        var orchestrator = go.AddComponent<AssistantDemoOrchestrator>();

        // Set state to Greeting
        var setStateMethod = typeof(AssistantDemoOrchestrator).GetMethod("SetState", NonPublicInstance);
        setStateMethod.Invoke(orchestrator, new object[] { DemoState.Greeting });

        orchestrator.AdvanceStep();

        // State should remain Greeting
        Assert.AreEqual(DemoState.Greeting, orchestrator.CurrentState);

        Object.DestroyImmediate(go);
    }

    #endregion

    #region Detection Handling Tests

    [Test]
    public void Orchestrator_OnDetectionsReceived_CachesDetections()
    {
        var go = new GameObject();
        go.SetActive(false);
        var orchestrator = go.AddComponent<AssistantDemoOrchestrator>();

        var detections = new List<Detection>
        {
            new Detection { ClassName = "wrench", Confidence = 0.9f },
            new Detection { ClassName = "screwdriver", Confidence = 0.85f }
        };

        var onDetectionsMethod = typeof(AssistantDemoOrchestrator).GetMethod("OnDetectionsReceived", NonPublicInstance);
        onDetectionsMethod.Invoke(orchestrator, new object[] { detections });

        Assert.AreEqual(2, orchestrator.LastDetections.Count);
        Assert.AreEqual("wrench", orchestrator.LastDetections[0].ClassName);
        Assert.AreEqual("screwdriver", orchestrator.LastDetections[1].ClassName);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Orchestrator_OnDetectionsReceived_HandlesNullList()
    {
        var go = new GameObject();
        go.SetActive(false);
        var orchestrator = go.AddComponent<AssistantDemoOrchestrator>();

        var onDetectionsMethod = typeof(AssistantDemoOrchestrator).GetMethod("OnDetectionsReceived", NonPublicInstance);
        
        Assert.DoesNotThrow(() => onDetectionsMethod.Invoke(orchestrator, new object[] { null }));
        Assert.IsNotNull(orchestrator.LastDetections);
        Assert.AreEqual(0, orchestrator.LastDetections.Count);

        Object.DestroyImmediate(go);
    }

    #endregion

    #region Helper Methods

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, NonPublicInstance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }

    #endregion
}

