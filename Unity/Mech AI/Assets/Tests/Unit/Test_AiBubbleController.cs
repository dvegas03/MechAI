using NUnit.Framework;
using UnityEngine;
using UnityEditor.Animations;

public class Test_AiBubbleController
{
    private AnimatorController CreateTestAnimatorController()
    {
        var animatorController = new AnimatorController();
        animatorController.AddParameter("isIdle", AnimatorControllerParameterType.Bool);
        animatorController.AddParameter("isListening", AnimatorControllerParameterType.Bool);
        animatorController.AddParameter("isSpeaking", AnimatorControllerParameterType.Bool);
        animatorController.AddParameter("isThinking", AnimatorControllerParameterType.Bool);
        animatorController.AddParameter("speakingAmplitude", AnimatorControllerParameterType.Float);
        animatorController.AddLayer("Base Layer");
        return animatorController;
    }

    [Test]
    public void SetListening_SetsCorrectAnimatorBools()
    {
        var animatorController = CreateTestAnimatorController();

        var go = new GameObject();
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = animatorController;
        
        var controller = go.AddComponent<AiBubbleController>();
        var animatorField = typeof(AiBubbleController).GetField("animator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        animatorField.SetValue(controller, animator);

        controller.SetListening();

        Assert.IsFalse(animator.GetBool("isIdle"), "isIdle should be false when listening");
        Assert.IsTrue(animator.GetBool("isListening"), "isListening should be true when listening");
        Assert.IsFalse(animator.GetBool("isSpeaking"), "isSpeaking should be false when listening");
        Assert.IsFalse(animator.GetBool("isThinking"), "isThinking should be false when listening");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void SetIdle_SetsCorrectAnimatorBools()
    {
        var animatorController = CreateTestAnimatorController();

        var go = new GameObject();
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = animatorController;
        
        var controller = go.AddComponent<AiBubbleController>();
        var animatorField = typeof(AiBubbleController).GetField("animator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        animatorField.SetValue(controller, animator);

        controller.SetIdle();

        Assert.IsTrue(animator.GetBool("isIdle"), "isIdle should be true when idle");
        Assert.IsFalse(animator.GetBool("isListening"), "isListening should be false when idle");
        Assert.IsFalse(animator.GetBool("isSpeaking"), "isSpeaking should be false when idle");
        Assert.IsFalse(animator.GetBool("isThinking"), "isThinking should be false when idle");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void SetSpeaking_SetsCorrectAnimatorBools()
    {
        var animatorController = CreateTestAnimatorController();

        var go = new GameObject();
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = animatorController;
        
        var controller = go.AddComponent<AiBubbleController>();
        var animatorField = typeof(AiBubbleController).GetField("animator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        animatorField.SetValue(controller, animator);

        controller.SetSpeaking();

        Assert.IsFalse(animator.GetBool("isIdle"), "isIdle should be false when speaking");
        Assert.IsFalse(animator.GetBool("isListening"), "isListening should be false when speaking");
        Assert.IsTrue(animator.GetBool("isSpeaking"), "isSpeaking should be true when speaking");
        Assert.IsFalse(animator.GetBool("isThinking"), "isThinking should be false when speaking");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void SetThinking_SetsCorrectAnimatorBools()
    {
        var animatorController = CreateTestAnimatorController();

        var go = new GameObject();
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = animatorController;
        
        var controller = go.AddComponent<AiBubbleController>();
        var animatorField = typeof(AiBubbleController).GetField("animator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        animatorField.SetValue(controller, animator);

        controller.SetThinking();

        Assert.IsFalse(animator.GetBool("isIdle"), "isIdle should be false when thinking");
        Assert.IsFalse(animator.GetBool("isListening"), "isListening should be false when thinking");
        Assert.IsFalse(animator.GetBool("isSpeaking"), "isSpeaking should be false when thinking");
        Assert.IsTrue(animator.GetBool("isThinking"), "isThinking should be true when thinking");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void UpdateFromMicLevel_SetsAmplitudeParameter()
    {
        var animatorController = CreateTestAnimatorController();

        var go = new GameObject();
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = animatorController;
        
        var controller = go.AddComponent<AiBubbleController>();
        var animatorField = typeof(AiBubbleController).GetField("animator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        animatorField.SetValue(controller, animator);

        controller.UpdateFromMicLevel(0.75f);

        Assert.AreEqual(0.75f, animator.GetFloat("speakingAmplitude"), 0.001f, "speakingAmplitude should be set to the provided value");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void SetListening_WithNullAnimator_DoesNotThrow()
    {
        var go = new GameObject();
        var controller = go.AddComponent<AiBubbleController>();
        // animator field is null by default

        Assert.DoesNotThrow(() => controller.SetListening());
        Assert.DoesNotThrow(() => controller.SetIdle());
        Assert.DoesNotThrow(() => controller.SetSpeaking());
        Assert.DoesNotThrow(() => controller.SetThinking());
        Assert.DoesNotThrow(() => controller.UpdateFromMicLevel(0.5f));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void StateTransitions_MutuallyExclusive()
    {
        var animatorController = CreateTestAnimatorController();

        var go = new GameObject();
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = animatorController;
        
        var controller = go.AddComponent<AiBubbleController>();
        var animatorField = typeof(AiBubbleController).GetField("animator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        animatorField.SetValue(controller, animator);

        // Test full cycle: Idle -> Listening -> Thinking -> Speaking -> Idle
        controller.SetIdle();
        Assert.IsTrue(animator.GetBool("isIdle"));

        controller.SetListening();
        Assert.IsFalse(animator.GetBool("isIdle"));
        Assert.IsTrue(animator.GetBool("isListening"));

        controller.SetThinking();
        Assert.IsFalse(animator.GetBool("isListening"));
        Assert.IsTrue(animator.GetBool("isThinking"));

        controller.SetSpeaking();
        Assert.IsFalse(animator.GetBool("isThinking"));
        Assert.IsTrue(animator.GetBool("isSpeaking"));

        controller.SetIdle();
        Assert.IsFalse(animator.GetBool("isSpeaking"));
        Assert.IsTrue(animator.GetBool("isIdle"));

        Object.DestroyImmediate(go);
    }
}
