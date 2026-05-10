using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [SerializeField] private Movement movementData;

    [Header("Wheel Colliders")]
    [SerializeField] private WheelCollider wheelFront;
    [SerializeField] private WheelCollider wheelBack;

    [Header("Visual Lean")]
    [SerializeField] private Transform visualModel;
    [SerializeField, Min(0f)] private float maxLeanAngle = 18f;
    [SerializeField, Min(0f)] private float leanSmooth = 8f;
    [Header("Boost")]
    [SerializeField, Min(1f)] private float boostSpeedMultiplier = 1.35f;
    [SerializeField, Min(1f)] private float boostAccelerationMultiplier = 1.2f;

    private Rigidbody _rb;
    private float _inputTurn;
    private bool _boostActive;
    private IVehicleCommandSource _commandSource;
    private float _currentLeanAngle;
    private Vector3 _visualBaseLocalPosition;

    // Initializes Rigidbody setup and validates movement configuration.
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        if (movementData == null)
        {
            Debug.LogError("[VehicleController] movementData is NULL! Podepnij asset Movement w Inspectorze.", this);
            enabled = false;
            return;
        }

        _rb.angularDamping = movementData.angularDrag;
        // Prevent the vehicle from tipping over sideways; X rotation is allowed for terrain following
        _rb.constraints = RigidbodyConstraints.FreezeRotationZ;
        // Lowering the center of mass improves stability during braking and sharp turns
        _rb.centerOfMass = movementData.centerOfMassOffset;

        if (visualModel != null)
        {
            _visualBaseLocalPosition = visualModel.localPosition;
        }
    }

    // Reads the current command source every frame.
    private void Update()
    {
        if (_commandSource == null)
            _commandSource = GetComponent<IVehicleCommandSource>();

        VehicleCommand command = _commandSource != null ? _commandSource.GetCommand() : VehicleCommand.Neutral;
        _inputTurn = command.turn;
        _boostActive = command.boost;

        ApplyVisualLean();
    }

    // Applies movement physics on a fixed timestep.
    private void FixedUpdate()
    {
        // Guard against NaN from physics breakdown
        if (!IsValidRigidbodyState())
        {
            Debug.LogWarning("[VehicleController] Detected NaN in rigidbody state, skipping physics update.");
            return;
        }

        ApplyMotor();
        ApplySteering();
        ApplyTurnAssist();
        ApplyYawStabilization();
        ApplyLateralStabilization();
        ApplyBrake();
    }

    // Detects NaN values in rigidbody state
    private bool IsValidRigidbodyState()
    {
        if (_rb == null) return false;
        if (!IsValidVector3(_rb.linearVelocity)) return false;
        if (!IsValidVector3(_rb.angularVelocity)) return false;
        return true;
    }

    // Helper to check if a vector contains NaN
    private bool IsValidVector3(Vector3 v)
    {
        return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z);
    }

    // Applies drive torque with a smooth falloff near max speed.
    private void ApplyMotor()
    {
        float forwardSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
        float targetSpeed = movementData.maxSpeed * (_boostActive ? boostSpeedMultiplier : 1f);
        float accel = movementData.maxForwardAcceleration > 0f
            ? movementData.maxForwardAcceleration
            : targetSpeed / Mathf.Max(0.01f, movementData.timeToMaxSpeed);
        if (_boostActive)
            accel *= boostAccelerationMultiplier;

        float newForwardSpeed = Mathf.MoveTowards(
            forwardSpeed,
            targetSpeed,
            accel * Time.fixedDeltaTime);

        Vector3 vel = _rb.linearVelocity;
        Vector3 lateral = Vector3.Project(vel, transform.right);
        Vector3 vertical = Vector3.Project(vel, transform.up);
        
        Vector3 newVel = transform.forward * newForwardSpeed + lateral + vertical;
        if (IsValidVector3(newVel))
        {
            _rb.linearVelocity = newVel;
        }

        // Keep wheel colliders free-rolling; visual rotation comes from ground contact.
        wheelBack.motorTorque = 0f;
        wheelFront.motorTorque = 0f;
    }

    // Turns the front wheel based on horizontal input.
    private void ApplySteering()
    {
        float speed = _rb.linearVelocity.magnitude;
        float speedFactor = Mathf.Clamp01(speed / Mathf.Max(0.1f, movementData.maxSpeed));
        float steerFactor = 1f;
        if (speedFactor > movementData.steerReductionStart)
        {
            float t = Mathf.InverseLerp(movementData.steerReductionStart, 1f, speedFactor);
            steerFactor = Mathf.Lerp(1f, movementData.highSpeedSteerFactor, t);
        }
        float lowSpeedFactor = movementData.minSteerSpeed > 0f
            ? Mathf.Clamp01(speed / movementData.minSteerSpeed)
            : 1f;
        float steer = _inputTurn * movementData.turnSpeed * steerFactor * lowSpeedFactor;
        wheelFront.steerAngle = steer;
    }

    // Adds yaw torque to make turns sharper and more responsive.
    private void ApplyTurnAssist()
    {
        if (movementData.turnAssistTorque <= 0f)
            return;

        if (Mathf.Abs(_inputTurn) < 0.001f)
            return;

        if (wheelFront != null && wheelBack != null && !wheelFront.isGrounded && !wheelBack.isGrounded)
            return;

        float speed = _rb.linearVelocity.magnitude;
        if (speed < Mathf.Max(0.1f, movementData.turnAssistMinSpeed))
            return;

        float speedFactor = Mathf.Clamp01(speed / Mathf.Max(0.1f, movementData.maxSpeed));
        float yawVel = Vector3.Dot(_rb.angularVelocity, transform.up);
        float targetYaw = _inputTurn * movementData.turnAssistMaxYawRate * speedFactor;
        float yawError = targetYaw - yawVel;
        if (movementData.yawDeadzone > 0f && Mathf.Abs(yawError) < movementData.yawDeadzone)
            return;
        float torque = Mathf.Clamp(yawError * movementData.turnAssistTorque,
            -movementData.turnAssistTorque,
            movementData.turnAssistTorque);
        torque -= yawVel * movementData.turnAssistDamping;
        
        if (!float.IsNaN(torque))
        {
            _rb.AddTorque(transform.up * torque, ForceMode.Acceleration);
        }
    }

    // Prevents sudden spin and reduces long-turn yaw jitter.
    private void ApplyYawStabilization()
    {
        if (movementData.maxYawRate <= 0f && movementData.yawDamping <= 0f)
            return;

        Vector3 ang = _rb.angularVelocity;
        float yaw = Vector3.Dot(ang, transform.up);
        float targetYaw = yaw;
        bool turning = Mathf.Abs(_inputTurn) > 0.001f;

        if (movementData.maxYawRate > 0f)
            targetYaw = Mathf.Clamp(targetYaw, -movementData.maxYawRate, movementData.maxYawRate);

        if (movementData.yawDamping > 0f && !turning)
        {
            float t = Mathf.Clamp01(movementData.yawDamping * Time.fixedDeltaTime);
            targetYaw = Mathf.Lerp(targetYaw, 0f, t);
        }

        Vector3 newAngularVel = ang + transform.up * (targetYaw - yaw);
        if (IsValidVector3(newAngularVel))
        {
            _rb.angularVelocity = newAngularVel;
        }
    }

    // Braking is disabled for the base vehicle loop.
    private void ApplyBrake()
    {
        // Braking is disabled — player cannot stop the vehicle
        wheelFront.brakeTorque = 0f;
        wheelBack.brakeTorque  = 0f;
    }

    // Damps sideways sliding after turns.
    private void ApplyLateralStabilization()
    {
        if (movementData.lateralDamping <= 0f && movementData.maxLateralSpeedRatio <= 0f)
            return;

        Vector3 vel = _rb.linearVelocity;
        float forwardSpeed = Vector3.Dot(vel, transform.forward);
        Vector3 forward = transform.forward * forwardSpeed;
        Vector3 vertical = Vector3.Project(vel, transform.up);
        Vector3 lateral = vel - forward - vertical;

        if (movementData.lateralDamping > 0f)
        {
            float t = Mathf.Clamp01(movementData.lateralDamping * Time.fixedDeltaTime);
            lateral *= (1f - t);
        }

        float maxRatio = movementData.maxLateralSpeedRatio;
        if (maxRatio > 0f)
        {
            float maxLateral = Mathf.Abs(forwardSpeed) * maxRatio;
            float lateralSpeed = lateral.magnitude;
            if (lateralSpeed > maxLateral && lateralSpeed > 0f)
            {
                lateral = lateral * (maxLateral / lateralSpeed);
            }
        }

        Vector3 newVel = forward + lateral + vertical;
        if (IsValidVector3(newVel))
        {
            _rb.linearVelocity = newVel;
        }
    }

    // Tilts only the visual model to emulate scooter-like leaning during turns.
    private void ApplyVisualLean()
    {
        if (visualModel == null)
            return;
        // Make lean more visible even when maxSpeed is set very high.
        // Reach full lean at a fraction of configured maxSpeed (tweak 0.25f as needed).
        float currentSpeed = _rb.linearVelocity.magnitude;
        float denom = Mathf.Max(0.1f, movementData.maxSpeed * 0.25f);
        float speedFactor = Mathf.Clamp01(currentSpeed / denom);
        float targetLean = -_inputTurn * maxLeanAngle * speedFactor;

        _currentLeanAngle = Mathf.Lerp(_currentLeanAngle, targetLean, leanSmooth * Time.deltaTime);

        // Keep the model anchored at its original local position
        visualModel.localPosition = _visualBaseLocalPosition;
        // Apply lean only on Z axis; Y rotation will follow the vehicle's orientation naturally
        visualModel.localRotation = Quaternion.Euler(0f, 0f, _currentLeanAngle);
    }
}
