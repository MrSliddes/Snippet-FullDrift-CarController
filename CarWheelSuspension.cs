using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores data for the wheel suspension
/// </summary>
public class CarWheelSuspension
{
    /// <summary>
    /// How far the suspension is compressed
    /// </summary>
    /// <remarks>Range 0.0 - 1.0</remarks>
    public float compressionRatio;
    /// <summary>
    /// Stored vector3 position of the raycast hit.point
    /// </summary>
    public Vector3 surfaceImpactPoint;
    /// <summary>
    /// The normal of the raycast hit.normal
    /// </summary>
    public Vector3 surfaceImpactNormal;
}
