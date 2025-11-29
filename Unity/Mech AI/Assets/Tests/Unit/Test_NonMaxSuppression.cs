using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Test_NonMaxSuppression
{
    [Test]
    public void NonMaxSuppression_FiltersOverlappingBoxes()
    {
        var config = ScriptableObject.CreateInstance<Config>();
        var iouField = typeof(Config).GetField("iouThreshold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        iouField.SetValue(config, .5f);

        var detectorGO = new GameObject();
        var detector = detectorGO.AddComponent<Detector>();
        var detectorType = typeof(Detector);
        var configField = detectorType.GetField("yoloConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        configField.SetValue(detector, config);

        var detections = new List<Detection>
        {
            new Detection { BoundingBox = new Rect(0, 0, 100, 100), Confidence = .9f, ClassName = "A" }, // Keep
            new Detection { BoundingBox = new Rect(10, 10, 100, 100), Confidence = .8f, ClassName = "A" }, // Suppress (IoU > 0.5)
            new Detection { BoundingBox = new Rect(200, 200, 50, 50), Confidence = .85f, ClassName = "B" } // Keep
        };

        var nmsMethod = detectorType.GetMethod("NonMaxSuppression", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (List<Detection>)nmsMethod.Invoke(detector, new object[] { detections });

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Any(d => d.Confidence == .9f));
        Assert.IsTrue(result.Any(d => d.Confidence == .85f));

        Object.DestroyImmediate(detectorGO);
        Object.DestroyImmediate(config);
    }
}