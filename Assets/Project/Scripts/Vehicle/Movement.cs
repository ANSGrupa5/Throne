using UnityEngine;

[CreateAssetMenu(fileName = "Movement", menuName = "Vehicle/Movement Data")]
public class Movement : ScriptableObject
{
    [Header("Speed")]
    [Min(0f)] public float maxSpeed = 15f;
    [Min(0f)] public float acceleration = 1500f;
    [Tooltip("Time to reach maxSpeed from 0 (seconds)")]
    [Min(0.1f)] public float timeToMaxSpeed = 1f;
    [Tooltip("Max forward acceleration (m/s^2). 0 = derived from maxSpeed/timeToMaxSpeed")]
    [Min(0f)] public float maxForwardAcceleration = 0f;

    [Header("Steering")]
    [Min(0f)] public float turnSpeed = 30f;
    [Tooltip("Extra yaw torque to make turns sharper at speed")]
    [Min(0f)] public float turnAssistTorque = 200f;
    [Tooltip("Target max yaw rate (rad/s) for turn assist")]
    [Min(0f)] public float turnAssistMaxYawRate = 2.5f;
    [Tooltip("Extra damping for yaw to prevent over-rotation")]
    [Min(0f)] public float turnAssistDamping = 1.5f;
    [Tooltip("Steer scale at max speed (0-1). Lower value reduces over-rotation at high speed")]
    [Range(0f, 1f)] public float highSpeedSteerFactor = 0.4f;
    [Tooltip("Speed fraction (0-1) where steering starts to be reduced")]
    [Range(0f, 1f)] public float steerReductionStart = 0.35f;
    [Tooltip("Speed (m/s) where full steering becomes available")]
    [Min(0f)] public float minSteerSpeed = 2f;
    [Tooltip("Speed (m/s) required to enable turn assist")]
    [Min(0f)] public float turnAssistMinSpeed = 2f;

    [Header("Physics")]
    [Tooltip("Controls how quickly the vehicle stops rotating")]
    [Min(0f)] public float angularDrag = 1f;
    [Tooltip("How quickly lateral (sideways) velocity is damped (1/s)")]
    [Min(0f)] public float lateralDamping = 10f;
    [Tooltip("Clamp sideways speed to this fraction of forward speed (0-1). 0 = disabled")]
    [Range(0f, 1f)] public float maxLateralSpeedRatio = 0.05f;
    [Tooltip("Clamp yaw rate to prevent spin at low speed (rad/s). 0 = disabled")]
    [Min(0f)] public float maxYawRate = 2f;
    [Tooltip("Extra yaw damping to reduce long-turn oscillations (1/s)")]
    [Min(0f)] public float yawDamping = 0.6f;
    [Tooltip("Yaw error deadzone (rad/s) to avoid micro-oscillations")]
    [Min(0f)] public float yawDeadzone = 0.05f;
    [Tooltip("Applied as a local offset from the Rigidbody's default center; negative Y lowers the center of mass for improved stability")]
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.5f, 0f);

    [Header("Visual Lean")]
    // Y offset for the pivot point where lean rotation occurs; negative values lower the rotation axis
    [Tooltip("Y offset of the lean pivot point; use negative values like -1 to rotate around the lower part of the vehicle")]
    public float leanPivotOffsetY = 0f;
}
