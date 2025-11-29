using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;

[CreateAssetMenu(fileName = "YoloConfig", menuName = "YOLO/Model Configuration", order = 1)]
public class Config : ScriptableObject
{
    [SerializeField] private NNModel onnxModel;
    [SerializeField] private int inputWidth = 640;
    [SerializeField] private int inputHeight = 640;
    [SerializeField] private List<string> classNames = new List<string>();
    [SerializeField] private float confidenceThreshold = 0.5f;
    [SerializeField] private float iouThreshold = 0.45f;
    [SerializeField] private int maxDetections = 100;
    [SerializeField] private string targetClass;

    public NNModel OnnxModel => onnxModel;
    public int InputWidth => inputWidth;
    public int InputHeight => inputHeight;
    public IReadOnlyList<string> ClassNames => classNames.AsReadOnly();
    public float ConfidenceThreshold => confidenceThreshold;
    public float IouThreshold => iouThreshold;
    public int MaxDetections => maxDetections;
    public string TargetClass => targetClass;
}
