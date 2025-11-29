using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extended step data with additional demo-specific properties
/// </summary>
[Serializable]
public class DemoStep
{
    [Header("Step Information")]
    public string procedureTitle;
    public string stepTitle;
    [TextArea(3, 6)]
    public string stepBody;
    
    [Header("Assistant Script")]
    [TextArea(2, 4)]
    [Tooltip("What the assistant says when presenting this step")]
    public string assistantScript;
    
    [Header("Vision Integration")]
    [Tooltip("YOLO class name to highlight during this step (optional)")]
    public string associatedYoloClass;
    
    [Header("Timing")]
    [Tooltip("Minimum time to wait before allowing next step (seconds)")]
    public float minDurationSeconds = 5f;
    
    [Tooltip("If true, wait for user confirmation before proceeding")]
    public bool requiresUserConfirmation = true;

    /// <summary>
    /// Convert to StepData for LLM calls
    /// </summary>
    public StepData ToStepData()
    {
        return new StepData
        {
            procedureTitle = procedureTitle,
            stepTitle = stepTitle,
            stepBody = stepBody
        };
    }
}

/// <summary>
/// ScriptableObject containing a sequence of demo steps for the assistant
/// </summary>
[CreateAssetMenu(fileName = "DemoStepsConfig", menuName = "MechAI/Demo Steps Config", order = 1)]
public class DemoStepsConfig : ScriptableObject
{
    [Header("Demo Information")]
    public string demoTitle = "Car Maintenance Demo";
    [TextArea(2, 4)]
    public string demoDescription = "Step-by-step guide for basic car maintenance";

    [Header("Greeting")]
    [TextArea(2, 4)]
    public string greetingMessage = "Hey! How are you? What do you need help with today?";

    [Header("First Response (After User's First Input)")]
    [TextArea(3, 6)]
    public string predefinedFirstResponse = "Sure! Let's first start by identifying your car's make and model. You can also check your VIN number — it's usually located on the windshield plate or driver door frame.";

    [Header("Demo Steps")]
    public List<DemoStep> steps = new List<DemoStep>();

    [Header("Completion")]
    [TextArea(2, 4)]
    public string completionMessage = "That's it! Let me know if you have any questions.";

    [Header("Error Messages")]
    [TextArea(2, 3)]
    public string errorMessage = "Sorry, I encountered an issue. Let's try that again.";

    /// <summary>
    /// Get step at index, or null if out of bounds
    /// </summary>
    public DemoStep GetStep(int index)
    {
        if (index >= 0 && index < steps.Count)
            return steps[index];
        return null;
    }

    /// <summary>
    /// Total number of steps
    /// </summary>
    public int StepCount => steps.Count;
}

