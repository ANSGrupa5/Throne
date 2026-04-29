using UnityEngine;

public class VehicleGroundHitDebug : MonoBehaviour
{
    [Header("Wheel Colliders")]
    [SerializeField] private WheelCollider wheelFront;
    [SerializeField] private WheelCollider wheelBack;

    [Header("Debug")]
    [SerializeField] private bool logGroundHit = true;
    [SerializeField, Min(0.1f)] private float logInterval = 0.5f;
    [SerializeField] private bool logOnlyOnStateChange = true;

    private float _nextLogTime;
    private bool _frontWasGrounded;
    private bool _backWasGrounded;

    private void FixedUpdate()
    {
        if (!logGroundHit)
            return;

        WheelHit frontHit = default;
        WheelHit backHit = default;
        bool frontGrounded = wheelFront != null && wheelFront.GetGroundHit(out frontHit);
        bool backGrounded  = wheelBack  != null && wheelBack.GetGroundHit(out backHit);

        bool stateChanged = frontGrounded != _frontWasGrounded || backGrounded != _backWasGrounded;
        bool timeElapsed = Time.time >= _nextLogTime;

        if ((logOnlyOnStateChange && !stateChanged) || (!stateChanged && !timeElapsed))
            return;

        _frontWasGrounded = frontGrounded;
        _backWasGrounded = backGrounded;
        _nextLogTime = Time.time + logInterval;

        string frontInfo = wheelFront == null
            ? "F: null"
            : frontGrounded
                ? $"F: hitY={frontHit.point.y:F2} force={frontHit.force:F1} slip=({frontHit.forwardSlip:F2},{frontHit.sidewaysSlip:F2})"
                : "F: no ground";

        string backInfo = wheelBack == null
            ? "B: null"
            : backGrounded
                ? $"B: hitY={backHit.point.y:F2} force={backHit.force:F1} slip=({backHit.forwardSlip:F2},{backHit.sidewaysSlip:F2})"
                : "B: no ground";

        Debug.Log($"[GroundHit] {frontInfo} | {backInfo}", this);
    }
}
