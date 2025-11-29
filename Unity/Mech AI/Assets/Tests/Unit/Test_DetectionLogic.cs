using NUnit.Framework;
using UnityEngine;
using Unity.Barracuda;
using System.Collections.Generic;
using System.Reflection;

public class Test_DetectionLogic
{
    private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    [Test]
    public void ProcessOutput_CorrectlyIdentifiesBestClass()
    {
        var config = ScriptableObject.CreateInstance<Config>();
        var detectorGO = new GameObject();
        detectorGO.SetActive(false); // Prevent OnEnable from running
        var detector = detectorGO.AddComponent<Detector>();
        
        SetPrivateField(detector, "yoloConfig", config);
        SetPrivateField(config, "classNames", new List<string> { "classA", "classB", "classC" });
        SetPrivateField(config, "confidenceThreshold", 0.5f);
        SetPrivateField(config, "inputWidth", 640);
        SetPrivateField(config, "inputHeight", 640);

        var shape = new TensorShape(1, 1, 5 + 3); // batch, boxes, features (4 box + 1 obj + 3 classes)
        using (var tensor = new Tensor(shape))
        {
            var data = tensor.AsFloats();
            // Box data
            data[0] = .5f; // cx
            data[1] = .5f; // cy
            data[2] = .2f; // w
            data[3] = .2f; // h
            // Objectness
            data[4] = .9f; // objectness
            // Class scores
            data[5] = .1f; // classA
            data[6] = .8f; // classB
            data[7] = .2f; // classC

            var processOutputMethod = typeof(Detector).GetMethod("ProcessOutput", NonPublicInstance);
            var result = (List<Detection>)processOutputMethod.Invoke(detector, new object[] { tensor });

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("classB", result[0].ClassName);
            Assert.AreEqual(.8f * .9f, result[0].Confidence, .001f);
        }
        Object.DestroyImmediate(detectorGO);
        Object.DestroyImmediate(config);
    }

    [Test]
    public void ProcessOutput_SetsNormalizedCenter()
    {
        var config = ScriptableObject.CreateInstance<Config>();
        var detectorGO = new GameObject();
        detectorGO.SetActive(false);
        var detector = detectorGO.AddComponent<Detector>();
        
        SetPrivateField(detector, "yoloConfig", config);
        SetPrivateField(config, "classNames", new List<string> { "testClass" });
        SetPrivateField(config, "confidenceThreshold", 0.5f);
        SetPrivateField(config, "inputWidth", 640);
        SetPrivateField(config, "inputHeight", 640);

        var shape = new TensorShape(1, 1, 6); // 4 box + 1 obj + 1 class
        using (var tensor = new Tensor(shape))
        {
            var data = tensor.AsFloats();
            data[0] = 0.25f; // cx (normalized)
            data[1] = 0.75f; // cy (normalized)
            data[2] = 0.1f;  // w
            data[3] = 0.1f;  // h
            data[4] = 0.9f;  // objectness
            data[5] = 0.9f;  // class score

            var processOutputMethod = typeof(Detector).GetMethod("ProcessOutput", NonPublicInstance);
            var result = (List<Detection>)processOutputMethod.Invoke(detector, new object[] { tensor });

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(0.25f, result[0].NormalizedCenter.x, 0.001f, "NormalizedCenter.x should match cx");
            Assert.AreEqual(0.75f, result[0].NormalizedCenter.y, 0.001f, "NormalizedCenter.y should match cy");
            Assert.IsFalse(result[0].HasWorldPosition, "HasWorldPosition should be false before projection");
        }
        
        Object.DestroyImmediate(detectorGO);
        Object.DestroyImmediate(config);
    }

    [Test]
    public void ProcessOutput_CalculatesBoundingBoxCorrectly()
    {
        var config = ScriptableObject.CreateInstance<Config>();
        var detectorGO = new GameObject();
        detectorGO.SetActive(false);
        var detector = detectorGO.AddComponent<Detector>();
        
        SetPrivateField(detector, "yoloConfig", config);
        SetPrivateField(config, "classNames", new List<string> { "testClass" });
        SetPrivateField(config, "confidenceThreshold", 0.5f);
        SetPrivateField(config, "inputWidth", 640);
        SetPrivateField(config, "inputHeight", 480);

        var shape = new TensorShape(1, 1, 6);
        using (var tensor = new Tensor(shape))
        {
            var data = tensor.AsFloats();
            data[0] = 0.5f;  // cx = center at 320px
            data[1] = 0.5f;  // cy = center at 240px
            data[2] = 0.25f; // w = 160px (0.25 * 640)
            data[3] = 0.25f; // h = 120px (0.25 * 480)
            data[4] = 0.9f;
            data[5] = 0.9f;

            var processOutputMethod = typeof(Detector).GetMethod("ProcessOutput", NonPublicInstance);
            var result = (List<Detection>)processOutputMethod.Invoke(detector, new object[] { tensor });

            Assert.AreEqual(1, result.Count);
            
            var bbox = result[0].BoundingBox;
            // x = (cx - w/2) * inputWidth = (0.5 - 0.125) * 640 = 240
            Assert.AreEqual(240f, bbox.x, 1f, "BoundingBox.x should be correct");
            // y = (cy - h/2) * inputHeight = (0.5 - 0.125) * 480 = 180
            Assert.AreEqual(180f, bbox.y, 1f, "BoundingBox.y should be correct");
            // width = w * inputWidth = 0.25 * 640 = 160
            Assert.AreEqual(160f, bbox.width, 1f, "BoundingBox.width should be correct");
            // height = h * inputHeight = 0.25 * 480 = 120
            Assert.AreEqual(120f, bbox.height, 1f, "BoundingBox.height should be correct");
        }
        
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
    }
}
