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

    private Rigidbody _rb;
    private float _inputForward;
    private float _inputTurn;
    private IVehicleInput _inputProvider;
    private float _currentLeanAngle;
    private Vector3 _visualBaseLocalPosition;
    private Quaternion _visualBaseLocalRotation;

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
            _visualBaseLocalRotation = visualModel.localRotation;
        }
    }

    // Reads raw player input every frame.
    private void Update()
    {
        if (_inputProvider == null)
            _inputProvider = GetComponent<IVehicleInput>();

        if (_inputProvider != null)
        {
            _inputProvider.GetInputs(out _inputForward, out _inputTurn);
        }
        else
        {
            // No input provider attached => remain idle by default
            _inputForward = 0f;
            _inputTurn = 0f;
        }

        ApplyVisualLean();
    }

    // Applies movement physics on a fixed timestep.
    private void FixedUpdate()
    {
        ApplyMotor();
        ApplySteering();
        ApplyBrake();
    }

    // Applies drive torque with a smooth falloff near max speed.
    private void ApplyMotor()
    {
        if (_inputForward <= 0f)
        {
            wheelBack.motorTorque  = 0f;
            wheelFront.motorTorque = 0f;
            return;
        }

        float currentSpeed = _rb.linearVelocity.magnitude;
        // speedFactor approaches 0 as vehicle nears maxSpeed, smoothly cutting off acceleration
        float speedFactor = Mathf.Clamp01(1f - (currentSpeed / movementData.maxSpeed));
        // Squaring the factor gives a quadratic torque curve: strong pull at low speed, gentle near the limit
        float torqueCurve  = speedFactor * speedFactor;
        float torque = movementData.acceleration * torqueCurve;

        // Drive the rear wheel only; the front wheel is used for steering.
        wheelBack.motorTorque  = torque;
        wheelFront.motorTorque = 0f;
    }

    // Turns the front wheel based on horizontal input.
    private void ApplySteering()
    {
        float steer = _inputTurn * movementData.turnSpeed;
        wheelFront.steerAngle = steer;
    }

    // Applies front-biased braking when the reverse key is held.
    private void ApplyBrake()
    {
        if (_inputForward >= 0f)
        {
            wheelFront.brakeTorque = 0f;
            wheelBack.brakeTorque  = 0f;
            return;
        }

        // Distributing more brake force to the front prevents rear wheel lockup,
        // which would cause the vehicle to spin under heavy braking
        float front = movementData.brakeForce * movementData.brakeFrontBias;
        float rear  = movementData.brakeForce * (1f - movementData.brakeFrontBias);
        wheelFront.brakeTorque = front;
        wheelBack.brakeTorque  = rear;
    }

    // Tilts only the visual model to emulate scooter-like leaning during turns.
    private void ApplyVisualLean()
    {
        if (visualModel == null)
            return;

        float speedFactor = Mathf.Clamp01(_rb.linearVelocity.magnitude / movementData.maxSpeed);
        float targetLean = -_inputTurn * maxLeanAngle * speedFactor;

        _currentLeanAngle = Mathf.Lerp(_currentLeanAngle, targetLean, leanSmooth * Time.deltaTime);

        // Keep the model anchored at its original local position so the root does not drift.
        visualModel.localPosition = _visualBaseLocalPosition;
        visualModel.localRotation = _visualBaseLocalRotation * Quaternion.Euler(0f, 0f, _currentLeanAngle);
    }
}
