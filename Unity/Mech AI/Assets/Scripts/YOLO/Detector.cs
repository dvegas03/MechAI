using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Barracuda;

public class Detector : MonoBehaviour
{
    [SerializeField]
    private Config yoloConfig;
    [SerializeField]
    private WebCamTexture webCamTexture;
    
    [Header("XR Passthrough (Optional)")]
    [SerializeField]
    private Texture xrPassthroughTexture;
    [SerializeField]
    private float inferenceInterval = 1.0f;
    
    [Header("XR Projection")]
    [Tooltip("The XR camera used for world space projection. If null, Camera.main will be used.")]
    [SerializeField]
    private Camera xrCamera;
    [Tooltip("Default distance from camera for detection placement when depth is unknown")]
    [SerializeField]
    private float defaultDetectionDistance = 2.0f;
    
    [Header("Visualization")]
    [Tooltip("Enable/disable label visualization")]
    public bool shouldVisualize = true;
    [Tooltip("Prefab for detection labels. Should contain TextMeshPro or similar text component.")]
    [SerializeField]
    private GameObject labelPrefab;
    [Tooltip("Parent transform for spawned labels (optional, helps keep hierarchy clean)")]
    [SerializeField]
    private Transform labelParent;
    [Tooltip("Offset from world position for label placement")]
    [SerializeField]
    private Vector3 labelOffset = new Vector3(0, 0.1f, 0);

    public event Action<List<Detection>> OnDetectionsReady;

    private YoloInference yoloInference;
    private float timeSinceLastInference;
    private bool isProcessing;
    private List<GameObject> spawnedLabels = new List<GameObject>();
    private Camera activeCamera;

    private void OnEnable()
    {
        yoloInference = new YoloInference(yoloConfig);
        webCamTexture?.Play();
        
        // Cache the camera reference
        activeCamera = xrCamera != null ? xrCamera : Camera.main;
        
        if (activeCamera == null)
        {
            Debug.LogWarning("Detector: No camera assigned and Camera.main is null. World projection will be disabled.");
        }
    }

    private void OnDestroy()
    {
        yoloInference?.Dispose();
        
        if (webCamTexture != null)
        {
            Destroy(webCamTexture);
        }
        
        // Clean up any remaining labels
        ClearLabels();
    }

    private void Update()
    {
        timeSinceLastInference += Time.deltaTime;

        if (CanProcess())
        {
            timeSinceLastInference = 0f;
            isProcessing = true;

            Texture sourceTexture = xrPassthroughTexture != null ? xrPassthroughTexture : webCamTexture;

            var renderTexture = RenderTexture.GetTemporary(yoloConfig.InputWidth, yoloConfig.InputHeight, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(sourceTexture, renderTexture);

            var output = yoloInference.PerformInference(renderTexture);
            var detections = ProcessOutput(output);
            
            // Calculate world positions for all detections
            CalculateWorldPositions(detections);
            
            OnDetectionsReady?.Invoke(detections);

            if (shouldVisualize)
            {
                SpawnLabels(detections);
            }

            RenderTexture.ReleaseTemporary(renderTexture);
            isProcessing = false;
        }
    }

    private bool CanProcess()
    {
        if (yoloInference == null || isProcessing || timeSinceLastInference < inferenceInterval) return false;

        if (xrPassthroughTexture != null) return true;

        return webCamTexture != null && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame;
    }

    /// <summary>
    /// Calculate world positions for all detections using the XR camera
    /// </summary>
    private void CalculateWorldPositions(List<Detection> detections)
    {
        if (activeCamera == null) return;

        for (int i = 0; i < detections.Count; i++)
        {
            var detection = detections[i];
            
            // Convert normalized center to screen coordinates
            Vector3 screenPoint = new Vector3(
                detection.NormalizedCenter.x * Screen.width,
                (1f - detection.NormalizedCenter.y) * Screen.height, // Flip Y for screen coords
                defaultDetectionDistance
            );

            // Project to world space using the camera
            // Note: In a real XR app, you might use raycasting against spatial mesh for accurate depth
            Vector3 worldPos = activeCamera.ScreenToWorldPoint(screenPoint);
            
            detection.WorldPosition = worldPos;
            detection.HasWorldPosition = true;
            
            detections[i] = detection;
        }
    }

    /// <summary>
    /// Spawn 3D labels for each detection in world space
    /// </summary>
    private void SpawnLabels(List<Detection> detections)
    {
        // Clear previous labels before spawning new ones
        ClearLabels();

        if (labelPrefab == null)
        {
            Debug.LogWarning("Detector: labelPrefab is not assigned. Cannot visualize detections.");
            return;
        }

        foreach (var detection in detections)
        {
            Vector3 spawnPosition;
            
            if (detection.HasWorldPosition)
            {
                spawnPosition = detection.WorldPosition + labelOffset;
            }
            else if (activeCamera != null)
            {
                // Fallback: place in front of camera at default distance
                spawnPosition = activeCamera.transform.position + 
                               activeCamera.transform.forward * defaultDetectionDistance + 
                               labelOffset;
            }
            else
            {
                // No camera available, skip this detection
                continue;
            }

            // Spawn the label
            GameObject label = Instantiate(labelPrefab, spawnPosition, Quaternion.identity, labelParent);
            
            // Make label face the camera (billboard effect)
            if (activeCamera != null)
            {
                label.transform.LookAt(activeCamera.transform);
                label.transform.Rotate(0, 180, 0); // Flip to face camera
            }

            // Try to set the label text
            SetLabelText(label, detection);
            
            spawnedLabels.Add(label);
        }
    }

    /// <summary>
    /// Set the text content of a spawned label
    /// </summary>
    private void SetLabelText(GameObject label, Detection detection)
    {
        // Try TextMeshPro first
        var tmpText = label.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = $"{detection.ClassName}\n{detection.Confidence:P0}";
            return;
        }

        var tmp3D = label.GetComponentInChildren<TMPro.TextMeshPro>();
        if (tmp3D != null)
        {
            tmp3D.text = $"{detection.ClassName}\n{detection.Confidence:P0}";
            return;
        }

        // Fallback to legacy Text component
        var legacyText = label.GetComponentInChildren<UnityEngine.UI.Text>();
        if (legacyText != null)
        {
            legacyText.text = $"{detection.ClassName}\n{detection.Confidence:P0}";
            return;
        }

        Debug.LogWarning($"Detector: Label prefab has no text component. Detection: {detection.ClassName}");
    }

    /// <summary>
    /// Remove all spawned labels from the scene
    /// </summary>
    private void ClearLabels()
    {
        foreach (var label in spawnedLabels)
        {
            if (label != null)
            {
                Destroy(label);
            }
        }
        spawnedLabels.Clear();
    }

    /// <summary>
    /// Manually clear all labels (can be called from external scripts)
    /// </summary>
    public void ClearVisualization()
    {
        ClearLabels();
    }

    private List<Detection> ProcessOutput(Tensor output)
    {
        var rawDetections = new List<Detection>();
        var classCount = yoloConfig.ClassNames.Count;
        var boxCount = output.shape[1];
        var predictionSize = output.shape[2];
        var tensorToRead = output;
        var predictions = tensorToRead.AsFloats();

        for (var i = 0; i < boxCount; i++)
        {
            var objectness = predictions[i * predictionSize + 4];
            if (objectness < yoloConfig.ConfidenceThreshold) continue;

            var bestClassIndex = 0;
            var bestClassScore = 0f;
            for (var j = 0; j < classCount; j++)
            {
                var classScore = predictions[i * predictionSize + 5 + j];
                if (classScore > bestClassScore)
                {
                    bestClassScore = classScore;
                    bestClassIndex = j;
                }
            }

            var confidence = bestClassScore * objectness;
            if (confidence < yoloConfig.ConfidenceThreshold) continue;

            string detectedClassName = yoloConfig.ClassNames[bestClassIndex];
            if (!string.IsNullOrEmpty(yoloConfig.TargetClass) && detectedClassName != yoloConfig.TargetClass) continue;

            // Raw YOLO output values (normalized 0-1)
            var cx = predictions[i * predictionSize + 0];
            var cy = predictions[i * predictionSize + 1];
            var w = predictions[i * predictionSize + 2];
            var h = predictions[i * predictionSize + 3];

            // Convert to pixel coordinates for bounding box
            var x = (cx - w / 2) * yoloConfig.InputWidth;
            var y = (cy - h / 2) * yoloConfig.InputHeight;

            rawDetections.Add(new Detection
            {
                BoundingBox = new Rect(x, y, w * yoloConfig.InputWidth, h * yoloConfig.InputHeight),
                ClassName = detectedClassName,
                Confidence = confidence,
                NormalizedCenter = new Vector2(cx, cy),
                WorldPosition = Vector3.zero,
                HasWorldPosition = false
            });
        }

        output.Dispose();

        var finalDetections = NonMaxSuppression(rawDetections);
        return finalDetections;
    }

    private List<Detection> NonMaxSuppression(List<Detection> detections)
    {
        var finalDetections = new List<Detection>();
        var sortedDetections = detections.OrderByDescending(d => d.Confidence).ToList();

        while (sortedDetections.Count > 0)
        {
            var bestDetection = sortedDetections[0];
            finalDetections.Add(bestDetection);
            sortedDetections.RemoveAt(0);

            for (int i = sortedDetections.Count - 1; i >= 0; i--)
            {
                var iou = CalculateIoU(bestDetection.BoundingBox, sortedDetections[i].BoundingBox);
                if (iou > yoloConfig.IouThreshold)
                {
                    sortedDetections.RemoveAt(i);
                }
            }
        }

        return finalDetections.Take(yoloConfig.MaxDetections).ToList();
    }

    private float CalculateIoU(Rect a, Rect b)
    {
        var xA = Math.Max(a.xMin, b.xMin);
        var yA = Math.Max(a.yMin, b.yMin);
        var xB = Math.Min(a.xMax, b.xMax);
        var yB = Math.Min(a.yMax, b.yMax);

        var intersectionArea = Math.Max(0, xB - xA) * Math.Max(0, yB - yA);
        var unionArea = a.width * a.height + b.width * b.height - intersectionArea;

        if (unionArea <= 0f) return 0f;

        return intersectionArea / unionArea;
    }
}
