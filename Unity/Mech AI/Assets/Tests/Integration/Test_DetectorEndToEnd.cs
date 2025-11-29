using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Barracuda;

/// <summary>
/// Integration tests for the Detector component.
/// These tests verify the detection pipeline processes tensor output correctly.
/// </summary>
public class Test_DetectorEndToEnd
{
    private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    [UnityTest]
    public IEnumerator Detector_ProcessOutput_ReturnsCorrectDetections()
    {
        // Create config with test class names
        var config = ScriptableObject.CreateInstance<Config>();
        SetPrivateField(config, "classNames", new List<string> { "test_class" });
        SetPrivateField(config, "confidenceThreshold", 0.5f);
        SetPrivateField(config, "iouThreshold", 0.45f);
        SetPrivateField(config, "maxDetections", 100);
        SetPrivateField(config, "inputWidth", 640);
        SetPrivateField(config, "inputHeight", 640);

        // Create a detector without triggering OnEnable's model loading
        var detectorGO = new GameObject();
        detectorGO.SetActive(false); // Prevent OnEnable from running
        var detector = detectorGO.AddComponent<Detector>();
        SetPrivateField(detector, "yoloConfig", config);
        
        // Create mock tensor output (simulating YOLO output)
        // Shape: [1, num_boxes, 5 + num_classes] = [1, 1, 6] for 1 box, 1 class
        var shape = new TensorShape(1, 1, 6);
        using (var tensor = new Tensor(shape))
        {
            var data = tensor.AsFloats();
            // Box center and size (normalized)
            data[0] = 0.5f;  // cx
            data[1] = 0.5f;  // cy
            data[2] = 0.2f;  // width
            data[3] = 0.2f;  // height
            data[4] = 0.9f;  // objectness
            data[5] = 0.95f; // class score for test_class

            // Call ProcessOutput directly via reflection
            var processMethod = typeof(Detector).GetMethod("ProcessOutput", NonPublicInstance);
            var detections = (List<Detection>)processMethod.Invoke(detector, new object[] { tensor });

            // Verify detection was created correctly
            Assert.AreEqual(1, detections.Count, "Should have exactly 1 detection");
            Assert.AreEqual("test_class", detections[0].ClassName, "Class name should match");
            Assert.Greater(detections[0].Confidence, 0.8f, "Confidence should be objectness * class_score");
        }

        yield return null;

        // Cleanup
        Object.DestroyImmediate(detectorGO);
        Object.DestroyImmediate(config);
    }

    [UnityTest]
    public IEnumerator Detector_ProcessOutput_FiltersLowConfidence()
    {
        var config = ScriptableObject.CreateInstance<Config>();
        SetPrivateField(config, "classNames", new List<string> { "test_class" });
        SetPrivateField(config, "confidenceThreshold", 0.8f); // High threshold
        SetPrivateField(config, "iouThreshold", 0.45f);
        SetPrivateField(config, "maxDetections", 100);
        SetPrivateField(config, "inputWidth", 640);
        SetPrivateField(config, "inputHeight", 640);

        var detectorGO = new GameObject();
        detectorGO.SetActive(false);
        var detector = detectorGO.AddComponent<Detector>();
        SetPrivateField(detector, "yoloConfig", config);

        var shape = new TensorShape(1, 1, 6);
        using (var tensor = new Tensor(shape))
        {
            var data = tensor.AsFloats();
            data[0] = 0.5f;
            data[1] = 0.5f;
            data[2] = 0.2f;
            data[3] = 0.2f;
            data[4] = 0.5f;  // Low objectness
            data[5] = 0.5f;  // Low class score -> combined = 0.25, below threshold

            var processMethod = typeof(Detector).GetMethod("ProcessOutput", NonPublicInstance);
            var detections = (List<Detection>)processMethod.Invoke(detector, new object[] { tensor });

            Assert.AreEqual(0, detections.Count, "Low confidence detections should be filtered");
        }

        yield return null;

        Object.DestroyImmediate(detectorGO);
        Object.DestroyImmediate(config);
    }

    [UnityTest]
    public IEnumerator Detector_OnDetectionsReady_CanSubscribe()
    {
        var config = ScriptableObject.CreateInstance<Config>();
        SetPrivateField(config, "classNames", new List<string> { "test_class" });
        SetPrivateField(config, "confidenceThreshold", 0.5f);
        SetPrivateField(config, "iouThreshold", 0.45f);
        SetPrivateField(config, "maxDetections", 100);
        SetPrivateField(config, "inputWidth", 640);
        SetPrivateField(config, "inputHeight", 640);

        var detectorGO = new GameObject();
        detectorGO.SetActive(false);
        var detector = detectorGO.AddComponent<Detector>();
        SetPrivateField(detector, "yoloConfig", config);

        // Verify we can subscribe to the event without errors
        bool subscribed = false;
        System.Action<List<Detection>> handler = (detections) =>
        {
            // Handler would process detections here
            Assert.IsNotNull(detections);
        };

        Assert.DoesNotThrow(() =>
        {
            detector.OnDetectionsReady += handler;
            subscribed = true;
        }, "Should be able to subscribe to OnDetectionsReady event");

        Assert.IsTrue(subscribed, "Event subscription should succeed");

        // Verify ProcessOutput still works and returns valid data
        var shape = new TensorShape(1, 1, 6);
        using (var tensor = new Tensor(shape))
        {
            var data = tensor.AsFloats();
            data[0] = 0.5f;
            data[1] = 0.5f;
            data[2] = 0.2f;
            data[3] = 0.2f;
            data[4] = 0.9f;
            data[5] = 0.9f;

            var processMethod = typeof(Detector).GetMethod("ProcessOutput", NonPublicInstance);
            var detections = (List<Detection>)processMethod.Invoke(detector, new object[] { tensor });
            
            Assert.IsNotNull(detections, "ProcessOutput should return detections");
            Assert.AreEqual(1, detections.Count, "Should have 1 detection");
            Assert.AreEqual("test_class", detections[0].ClassName);
        }

        // Clean unsubscribe
        Assert.DoesNotThrow(() =>
        {
            detector.OnDetectionsReady -= handler;
        }, "Should be able to unsubscribe from OnDetectionsReady event");

        yield return null;

        Object.DestroyImmediate(detectorGO);
        Object.DestroyImmediate(config);
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
