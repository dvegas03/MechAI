using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class Test_DemoStepsConfig
{
    [Test]
    public void DemoStepsConfig_CreatesWithDefaults()
    {
        var config = ScriptableObject.CreateInstance<DemoStepsConfig>();

        Assert.IsNotNull(config);
        Assert.IsNotNull(config.steps);
        Assert.AreEqual(0, config.StepCount);

        Object.DestroyImmediate(config);
    }

    [Test]
    public void DemoStepsConfig_GetStep_ReturnsCorrectStep()
    {
        var config = ScriptableObject.CreateInstance<DemoStepsConfig>();
        config.steps = new List<DemoStep>
        {
            new DemoStep { stepTitle = "Step 1", stepBody = "Body 1" },
            new DemoStep { stepTitle = "Step 2", stepBody = "Body 2" },
            new DemoStep { stepTitle = "Step 3", stepBody = "Body 3" }
        };

        Assert.AreEqual(3, config.StepCount);
        Assert.AreEqual("Step 1", config.GetStep(0).stepTitle);
        Assert.AreEqual("Step 2", config.GetStep(1).stepTitle);
        Assert.AreEqual("Step 3", config.GetStep(2).stepTitle);

        Object.DestroyImmediate(config);
    }

    [Test]
    public void DemoStepsConfig_GetStep_ReturnsNullForInvalidIndex()
    {
        var config = ScriptableObject.CreateInstance<DemoStepsConfig>();
        config.steps = new List<DemoStep>
        {
            new DemoStep { stepTitle = "Step 1" }
        };

        Assert.IsNull(config.GetStep(-1), "Negative index should return null");
        Assert.IsNull(config.GetStep(1), "Out of bounds index should return null");
        Assert.IsNull(config.GetStep(100), "Large index should return null");

        Object.DestroyImmediate(config);
    }

    [Test]
    public void DemoStep_ToStepData_ConvertsCorrectly()
    {
        var demoStep = new DemoStep
        {
            procedureTitle = "Oil Change",
            stepTitle = "Drain Oil",
            stepBody = "Remove the drain plug and let oil drain completely."
        };

        var stepData = demoStep.ToStepData();

        Assert.AreEqual("Oil Change", stepData.procedureTitle);
        Assert.AreEqual("Drain Oil", stepData.stepTitle);
        Assert.AreEqual("Remove the drain plug and let oil drain completely.", stepData.stepBody);
    }

    [Test]
    public void DemoStep_DefaultValues_AreCorrect()
    {
        var demoStep = new DemoStep();

        Assert.AreEqual(5f, demoStep.minDurationSeconds, "Default min duration should be 5 seconds");
        Assert.IsTrue(demoStep.requiresUserConfirmation, "Default should require user confirmation");
        Assert.IsNull(demoStep.associatedYoloClass);
        Assert.IsNull(demoStep.assistantScript);
    }

    [Test]
    public void DemoStepsConfig_MessagesAreConfigurable()
    {
        var config = ScriptableObject.CreateInstance<DemoStepsConfig>();
        
        config.greetingMessage = "Custom greeting";
        config.predefinedFirstResponse = "Custom first response";
        config.completionMessage = "Custom completion";
        config.errorMessage = "Custom error";

        Assert.AreEqual("Custom greeting", config.greetingMessage);
        Assert.AreEqual("Custom first response", config.predefinedFirstResponse);
        Assert.AreEqual("Custom completion", config.completionMessage);
        Assert.AreEqual("Custom error", config.errorMessage);

        Object.DestroyImmediate(config);
    }

    [Test]
    public void DemoStepsConfig_StepCount_ReturnsCorrectCount()
    {
        var config = ScriptableObject.CreateInstance<DemoStepsConfig>();
        
        Assert.AreEqual(0, config.StepCount);

        config.steps = new List<DemoStep> { new DemoStep(), new DemoStep() };
        Assert.AreEqual(2, config.StepCount);

        config.steps.Add(new DemoStep());
        Assert.AreEqual(3, config.StepCount);

        Object.DestroyImmediate(config);
    }
}

