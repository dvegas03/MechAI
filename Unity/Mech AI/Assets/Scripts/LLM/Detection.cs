using UnityEngine;

public struct Detection
{
    public string ClassName;
    public float Confidence;
    public Rect BoundingBox;
    
    /// <summary>
    /// Center of the bounding box in normalized screen coordinates (0-1)
    /// </summary>
    public Vector2 NormalizedCenter;
    
    /// <summary>
    /// Estimated world position of the detection (if XR projection is available)
    /// </summary>
    public Vector3 WorldPosition;
    
    /// <summary>
    /// Whether WorldPosition has been calculated
    /// </summary>
    public bool HasWorldPosition;
}
