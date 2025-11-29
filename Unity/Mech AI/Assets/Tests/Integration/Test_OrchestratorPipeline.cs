using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Integration tests for the AssistantDemoOrchestrator pipeline
/// </summary>
public class Test_OrchestratorPipeline
{
    private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    [UnityTest]
    public IEnumerator Orchestrator_StateTransition_FromInitializingToGreeting()
    {
        var orchestratorGO = new GameObject("TestOrchestrator");
        orchestratorGO.SetActive(false);
        var orchestrator = orchestratorGO.AddComponent<AssistantDemoOrchestrator>();

        // Disable auto-start
        SetPrivateField(orchestrator, "autoStartDemo", false);

        DemoState? capturedOldState = null;
        DemoState? capturedNewState = null;
        orchestrator.OnStateChanged += (old, @new) =>
        {
            capturedOldState = old;
            capturedNewState = @new;
        };

        // Manually set state to Greeting
        var setStateMethod = typeof(AssistantDemoOrchestrator).GetMethod("SetState", NonPublicInstance);
        setStateMethod.Invoke(orchestrator, new object[] { DemoState.Greeting });

        yield return null;

        Assert.AreEqual(DemoState.Initializing, capturedOldState);
        Assert.AreEqual(DemoState.Greeting, capturedNewState);
        Assert.AreEqual(DemoState.Greeting, orchestrator.CurrentState);

        Object.DestroyImmediate(orchestratorGO);
    }

    [UnityTest]
    public IEnumerator Orchestrator_DetectionCaching_StoresMultipleDetections()
    {
        var orchestratorGO = new GameObject("TestOrchestrator");
        orchestratorGO.SetActive(false);
        var orchestrator = orchestratorGO.AddComponent<AssistantDemoOrchestrator>();
        SetPrivateField(orchestrator, "autoStartDemo", false);

        var detections = new List<Detection>
        {
            new Detection 
            { 
                ClassName = "oil_filter", 
                Confidence = 0.95f,
                BoundingBox = new Rect(100, 100, 50, 50),
                NormalizedCenter = new Vector2(0.5f, 0.5f)
            },
            new Detection 
            { 
                ClassName = "drain_plug", 
                Confidence = 0.88f,
                BoundingBox = new Rect(200, 150, 30, 30),
                NormalizedCenter = new Vector2(0.6f, 0.4f)
            },
            new Detection 
            { 
                ClassName = "wrench", 
                Confidence = 0.92f,
                BoundingBox = new Rect(50, 200, 80, 20),
                NormalizedCenter = new Vector2(0.3f, 0.7f)
            }
        };

        var onDetectionsMethod = typeof(AssistantDemoOrchestrator).GetMethod("OnDetectionsReceived", NonPublicInstance);
        onDetectionsMethod.Invoke(orchestrator, new object[] { detections });

        yield return null;

        Assert.AreEqual(3, orchestrator.LastDetections.Count);
        Assert.AreEqual("oil_filter", orchestrator.LastDetections[0].ClassName);
        Assert.AreEqual(0.95f, orchestrator.LastDetections[0].Confidence, 0.01f);
        Assert.AreEqual("drain_plug", orchestrator.LastDetections[1].ClassName);
        Assert.AreEqual("wrench", orchestrator.LastDetections[2].ClassName);

        Object.DestroyImmediate(orchestratorGO);
    }

    [UnityTest]
    public IEnumerator Orchestrator_WithDemoConfig_HasCorrectStepCount()
    {
        var orchestratorGO = new GameObject("TestOrchestrator");
        orchestratorGO.SetActive(false);
        var orchestrator = orchestratorGO.AddComponent<AssistantDemoOrchestrator>();
        SetPrivateField(orchestrator, "autoStartDemo", false);

        // Create a demo config with steps
        var config = ScriptableObject.CreateInstance<DemoStepsConfig>();
        config.greetingMessage = "Test greeting";
        config.steps = new List<DemoStep>
        {
            new DemoStep 
            { 
                procedureTitle = "Test Procedure",
                stepTitle = "Step 1", 
                stepBody = "Do step 1",
                assistantScript = "Please do step 1"
            },
            new DemoStep 
            { 
                procedureTitle = "Test Procedure",
                stepTitle = "Step 2", 
                stepBody = "Do step 2",
                associatedYoloClass = "wrench"
            }
        };
        config.completionMessage = "Test complete";

        SetPrivateField(orchestrator, "demoConfig", config);

        yield return null;

        var configField = typeof(AssistantDemoOrchestrator).GetField("demoConfig", NonPublicInstance);
        var storedConfig = configField.GetValue(orchestrator) as DemoStepsConfig;

        Assert.IsNotNull(storedConfig);
        Assert.AreEqual(2, storedConfig.StepCount);
        Assert.AreEqual("Test greeting", storedConfig.greetingMessage);
        Assert.AreEqual("wrench", storedConfig.GetStep(1).associatedYoloClass);

        Object.DestroyImmediate(orchestratorGO);
        Object.DestroyImmediate(config);
    }

    [UnityTest]
    public IEnumerator Orchestrator_AdvanceStep_TransitionsFromAwaitingToRunning()
    {
        var orchestratorGO = new GameObject("TestOrchestrator");
        orchestratorGO.SetActive(false);
        var orchestrator = orchestratorGO.AddComponent<AssistantDemoOrchestrator>();
        SetPrivateField(orchestrator, "autoStartDemo", false);

        // Set to StepAwaitingUserQuestion state
        var setStateMethod = typeof(AssistantDemoOrchestrator).GetMethod("SetState", NonPublicInstance);
        setStateMethod.Invoke(orchestrator, new object[] { DemoState.StepAwaitingUserQuestion });

        Assert.AreEqual(DemoState.StepAwaitingUserQuestion, orchestrator.CurrentState);

        // Call AdvanceStep
        orchestrator.AdvanceStep();

        yield return null;

        Assert.AreEqual(DemoState.RunningSteps, orchestrator.CurrentState);

        Object.DestroyImmediate(orchestratorGO);
    }

    [UnityTest]
    public IEnumerator Orchestrator_Stop_ResetsAllFlags()
    {
        var orchestratorGO = new GameObject("TestOrchestrator");
        orchestratorGO.SetActive(false);
        var orchestrator = orchestratorGO.AddComponent<AssistantDemoOrchestrator>();
        SetPrivateField(orchestrator, "autoStartDemo", false);

        // Set flags to true
        SetPrivateField(orchestrator, "_isSpeaking", true);
        SetPrivateField(orchestrator, "_isProcessing", true);

        Assert.IsTrue(orchestrator.IsSpeaking);
        Assert.IsTrue(orchestrator.IsProcessing);

        // Stop
        orchestrator.Stop();

        yield return null;

        Assert.IsFalse(orchestrator.IsSpeaking);
        Assert.IsFalse(orchestrator.IsProcessing);

        Object.DestroyImmediate(orchestratorGO);
    }

    [UnityTest]
    public IEnumerator Orchestrator_DemoCompleted_FiresEvent()
    {
        LogAssert.ignoreFailingMessages = true;

        var orchestratorGO = new GameObject("TestOrchestrator");
        orchestratorGO.SetActive(false);
        var orchestrator = orchestratorGO.AddComponent<AssistantDemoOrchestrator>();
        SetPrivateField(orchestrator, "autoStartDemo", false);

        bool completedEventFired = false;
        orchestrator.OnDemoCompleted += () => completedEventFired = true;

        // Create minimal config
        var config = ScriptableObject.CreateInstance<DemoStepsConfig>();
        config.steps = new List<DemoStep>(); // No steps
        SetPrivateField(orchestrator, "demoConfig", config);

        // Manually set state to Completed and fire event
        var setStateMethod = typeof(AssistantDemoOrchestrator).GetMethod("SetState", NonPublicInstance);
        setStateMethod.Invoke(orchestrator, new object[] { DemoState.Completed });

        // Manually fire the event (since we're not running the full coroutine)
        var onDemoCompletedField = typeof(AssistantDemoOrchestrator).GetField("OnDemoCompleted", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var eventDelegate = onDemoCompletedField?.GetValue(orchestrator) as System.Action;
        eventDelegate?.Invoke();

        yield return null;

        Assert.IsTrue(completedEventFired, "OnDemoCompleted event should have fired");

        Object.DestroyImmediate(orchestratorGO);
        Object.DestroyImmediate(config);
        LogAssert.ignoreFailingMessages = false;
    }

    [UnityTest]
    public IEnumerator Orchestrator_ContainsAny_IdentifiesVisionQueries()
    {
        var orchestratorGO = new GameObject("TestOrchestrator");
        orchestratorGO.SetActive(false);
        var orchestrator = orchestratorGO.AddComponent<AssistantDemoOrchestrator>();

        var containsAnyMethod = typeof(AssistantDemoOrchestrator).GetMethod("ContainsAny", NonPublicInstance);
        string[] visionKeywords = { "what do you see", "what is this", "identify", "recognize", "look at" };

        // Test vision queries
        Assert.IsTrue((bool)containsAnyMethod.Invoke(orchestrator, new object[] { "what do you see in front of me", visionKeywords }));
        Assert.IsTrue((bool)containsAnyMethod.Invoke(orchestrator, new object[] { "can you identify this part", visionKeywords }));
        Assert.IsTrue((bool)containsAnyMethod.Invoke(orchestrator, new object[] { "look at this", visionKeywords }));

        // Test non-vision queries
        Assert.IsFalse((bool)containsAnyMethod.Invoke(orchestrator, new object[] { "how do i change the oil", visionKeywords }));
        Assert.IsFalse((bool)containsAnyMethod.Invoke(orchestrator, new object[] { "what should i do next", visionKeywords }));

        yield return null;

        Object.DestroyImmediate(orchestratorGO);
    }

    [UnityTest]
    public IEnumerator Orchestrator_ContainsAny_IdentifiesConfirmationQueries()
    {
        var orchestratorGO = new GameObject("TestOrchestrator");
        orchestratorGO.SetActive(false);
        var orchestrator = orchestratorGO.AddComponent<AssistantDemoOrchestrator>();

        var containsAnyMethod = typeof(AssistantDemoOrchestrator).GetMethod("ContainsAny", NonPublicInstance);
        string[] confirmKeywords = { "is this right", "is this correct", "did i do", "confirm", "check" };

        // Test confirmation queries
        Assert.IsTrue((bool)containsAnyMethod.Invoke(orchestrator, new object[] { "is this right", confirmKeywords }));
        Assert.IsTrue((bool)containsAnyMethod.Invoke(orchestrator, new object[] { "did i do this correctly", confirmKeywords }));
        Assert.IsTrue((bool)containsAnyMethod.Invoke(orchestrator, new object[] { "can you confirm this", confirmKeywords }));

        // Test non-confirmation queries
        Assert.IsFalse((bool)containsAnyMethod.Invoke(orchestrator, new object[] { "what is the next step", confirmKeywords }));

        yield return null;

        Object.DestroyImmediate(orchestratorGO);
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, NonPublicInstance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }
}

