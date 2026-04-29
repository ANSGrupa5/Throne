using UnityEngine;

[CreateAssetMenu(fileName = "Movement", menuName = "Vehicle/Movement Data")]
public class Movement : ScriptableObject
{
    [Header("Speed")]
    [Min(0f)] public float maxSpeed = 15f;
    [Min(0f)] public float acceleration = 1500f;

    [Header("Steering")]
    [Min(0f)] public float turnSpeed = 30f;

    [Header("Braking")]
    [Min(0f)] public float brakeForce = 3000f;
    // Higher values reduce rear wheel lockup and vehicle spin under braking
    [Range(0f, 1f), Tooltip("0 = tylko tylne, 1 = tylko przednie. 0.7 zapobiega obracaniu.")]
    public float brakeFrontBias = 0.7f;

    [Header("Physics")]
    [Tooltip("Controls how quickly the vehicle stops rotating")]
    [Min(0f)] public float angularDrag = 5f;
    // Applied as a local offset from the Rigidbody's default center; negative Y lowers the center of mass
    [Tooltip("Obniż środek masy aby zapobiec wywracaniu (np. 0,-0.5,0)")]
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.5f, 0f);

    [Header("Visual Lean")]
    // Y offset for the pivot point where lean rotation occurs; negative values lower the rotation axis
    [Tooltip("Y offset of the lean pivot point; use negative values like -1 to rotate around the lower part of the vehicle")]
    public float leanPivotOffsetY = 0f;
}
