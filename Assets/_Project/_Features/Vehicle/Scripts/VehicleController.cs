using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [SerializeField] private Movement movementData;

    [Header("Wheel Colliders")]
    [SerializeField] private WheelCollider wheelFront;
    [SerializeField] private WheelCollider wheelBack;

    private Rigidbody _rb;
    private float _inputForward;
    private float _inputTurn;

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
    }

    // Reads raw player input every frame.
    private void Update()
    {
        _inputForward = Input.GetAxisRaw("Vertical");
        _inputTurn    = Input.GetAxisRaw("Horizontal");
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

        wheelBack.motorTorque  = torque;
        wheelFront.motorTorque = torque;
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
}
