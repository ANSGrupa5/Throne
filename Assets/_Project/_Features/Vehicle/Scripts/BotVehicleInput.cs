using UnityEngine;

public class BotVehicleInput : MonoBehaviour, IVehicleCommandSource
{
    [Header("Priority 1 - Map Safety")]
    [SerializeField] private LayerMask mapBoundaryMask;
    [SerializeField] private LayerMask suddenDeathMask;
    [SerializeField, Min(0.5f)] private float safetyRayLength = 10f;
    [SerializeField, Range(5f, 75f)] private float safetyRayAngle = 30f;
    [SerializeField, Min(0f)] private float safetyAvoidanceStrength = 2.5f;

    [Header("Priority 2 - Trail Avoidance")]
    [SerializeField] private LayerMask trailMask;
    [SerializeField, Min(0.5f)] private float trailRayLength = 8f;
    [SerializeField, Range(5f, 75f)] private float trailRayAngle = 25f;
    [SerializeField, Min(0f)] private float trailAvoidanceStrength = 1.7f;

    [Header("Priority 3 - Intercept")]
    [SerializeField, Min(0f)] private float playerSearchRadius = 150f;
    [SerializeField, Min(0f)] private float blockAheadDistance = 8f;
    [SerializeField, Min(0f)] private float lateralBlockOffset = 2.5f;
    [SerializeField, Min(0f)] private float interceptLeadTime = 0.55f;
    [SerializeField, Min(0f)] private float interceptSteeringStrength = 2.1f;

    [Header("Priority 4 - Powerups")]
    [SerializeField] private LayerMask powerupMask;
    [SerializeField, Min(0f)] private float powerupSearchRadius = 500f;
    [SerializeField, Min(0f)] private float powerupSteeringStrength = 1.5f;

    [Header("Priority 5 - Center")]
    [SerializeField] private Transform mapCenter;
    [SerializeField, Min(0f)] private float centerRoamRadius = 12f;
    [SerializeField, Min(0f)] private float centerSteeringStrength = 1.35f;
    [SerializeField, Min(0f)] private float roamDirectionDuration = 3f;
    [SerializeField, Min(0f)] private float roamSteeringStrength = 1.15f;

    [Header("General")]
    [SerializeField, Min(0f)] private float wanderStrength = 0.04f;
    [SerializeField, Min(0.1f)] private float wanderChangeInterval = 1.25f;

    private float _wanderTurn;
    private float _nextWanderChangeTime;
    private float _roamUntilTime;
    private Vector3 _roamDirection;
    private VehicleLife _ownerLife;

    private void Awake()
    {
        _ownerLife = GetComponent<VehicleLife>();
    }

    private void OnEnable()
    {
        PickNewWanderTurn();
        if (_ownerLife == null)
            _ownerLife = GetComponent<VehicleLife>();
    }

    public VehicleCommand GetCommand()
    {
        UpdateWander();

        Vector3 position = transform.position;
        Vector3 safetyAvoidance = ComputeRayAvoidance(mapBoundaryMask | suddenDeathMask, safetyRayLength, safetyRayAngle, safetyAvoidanceStrength);
        Vector3 trailAvoidance = ComputeRayAvoidance(trailMask, trailRayLength, trailRayAngle, trailAvoidanceStrength);

        Vector3 targetVector;
        float targetStrength;

        Transform player = FindClosestPlayer(playerSearchRadius);
        if (player != null)
        {
            targetVector = ComputeInterceptPoint(player) - position;
            targetStrength = interceptSteeringStrength;
            CancelRoam();
        }
        else if (TryFindNearestPowerup(position, out Vector3 powerupPoint))
        {
            targetVector = powerupPoint - position;
            targetStrength = powerupSteeringStrength;
            CancelRoam();
        }
        else
        {
            Vector3 center = GetMapCenter();
            float distanceToCenter = FlatDistance(position, center);

            if (distanceToCenter <= centerRoamRadius)
            {
                if (Time.time >= _roamUntilTime || _roamDirection.sqrMagnitude < 0.0001f)
                    BeginRoam();

                targetVector = _roamDirection;
                targetStrength = roamSteeringStrength;
            }
            else
            {
                CancelRoam();
                targetVector = center - position;
                targetStrength = centerSteeringStrength;
            }
        }

        targetVector.y = 0f;
        if (targetVector.sqrMagnitude < 0.0001f)
            targetVector = Flatten(transform.forward);

        Vector3 combined = targetVector.normalized * targetStrength;
        combined += safetyAvoidance;
        combined += trailAvoidance;
        combined += Flatten(transform.right) * _wanderTurn;

        if (combined.sqrMagnitude < 0.0001f)
            combined = Flatten(transform.forward);

        Vector3 local = transform.InverseTransformDirection(combined.normalized);
        float turn = Mathf.Clamp(local.x, -1f, 1f);
        return new VehicleCommand(turn, false);
    }

    private void UpdateWander()
    {
        if (Time.time >= _nextWanderChangeTime)
            PickNewWanderTurn();
    }

    private void PickNewWanderTurn()
    {
        _nextWanderChangeTime = Time.time + wanderChangeInterval;
        _wanderTurn = Random.Range(-wanderStrength, wanderStrength);
    }

    private void BeginRoam()
    {
        _roamUntilTime = Time.time + roamDirectionDuration;
        Vector2 roam = Random.insideUnitCircle.normalized;
        if (roam.sqrMagnitude < 0.001f)
            roam = Vector2.up;

        _roamDirection = new Vector3(roam.x, 0f, roam.y);
    }

    private void CancelRoam()
    {
        _roamUntilTime = 0f;
    }

    private Vector3 ComputeInterceptPoint(Transform player)
    {
        Vector3 playerPosition = player.position;
        Vector3 playerForward = player.forward;
        Vector3 playerVelocity = Vector3.zero;

        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
            playerVelocity = playerRb.linearVelocity;

        if (playerVelocity.sqrMagnitude > 0.01f)
            playerForward = playerVelocity.normalized;

        Vector3 flatForward = Flatten(playerForward);
        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Flatten(player.forward);

        Vector3 leadPoint = playerPosition + playerVelocity * interceptLeadTime;
        Vector3 right = Vector3.Cross(Vector3.up, flatForward).normalized;

        Vector3 relativeToPlayer = transform.position - playerPosition;
        relativeToPlayer.y = 0f;
        float side = Mathf.Sign(Vector3.Dot(relativeToPlayer, right));
        if (Mathf.Abs(side) < 0.01f)
            side = Random.value < 0.5f ? -1f : 1f;

        return leadPoint + flatForward * blockAheadDistance + right * side * lateralBlockOffset;
    }

    private Transform FindClosestPlayer(float maxDistance)
    {
        VehicleLife[] vehicles = Object.FindObjectsByType<VehicleLife>(FindObjectsSortMode.None);
        VehicleLife best = null;
        float bestDistance = maxDistance * maxDistance;

        for (int i = 0; i < vehicles.Length; i++)
        {
            VehicleLife candidate = vehicles[i];
            if (candidate == null || candidate == _ownerLife || candidate.IsDead)
                continue;

            if (candidate.GetComponent<BotVehicleInput>() != null)
                continue;

            float distance = Vector3.SqrMagnitude(candidate.transform.position - transform.position);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best != null ? best.transform : null;
    }

    private bool TryFindNearestPowerup(Vector3 origin, out Vector3 powerupPoint)
    {
        powerupPoint = default;

        if (powerupMask.value == 0)
            return false;

        Collider[] hits = Physics.OverlapSphere(origin, powerupSearchRadius, powerupMask, QueryTriggerInteraction.Collide);
        float bestDistance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
                continue;

            float distance = Vector3.SqrMagnitude(hit.bounds.center - origin);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                powerupPoint = hit.bounds.center;
                found = true;
            }
        }

        return found;
    }

    private Vector3 ComputeRayAvoidance(LayerMask mask, float rayLength, float rayAngle, float strength)
    {
        if (mask.value == 0 || rayLength <= 0f || strength <= 0f)
            return Vector3.zero;

        Vector3 origin = transform.position + transform.up * 0.25f;
        Vector3 forward = Flatten(transform.forward);
        if (forward.sqrMagnitude < 0.001f)
            return Vector3.zero;

        Vector3[] directions =
        {
            forward,
            Quaternion.AngleAxis(rayAngle, Vector3.up) * forward,
            Quaternion.AngleAxis(-rayAngle, Vector3.up) * forward
        };

        Vector3 avoidance = Vector3.zero;
        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 direction = directions[i];
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                continue;

            direction.Normalize();

            if (!Physics.Raycast(origin, direction, out RaycastHit hit, rayLength, mask, QueryTriggerInteraction.Collide))
                continue;

            Vector3 away = Flatten(hit.normal);
            if (away.sqrMagnitude < 0.001f)
                away = -direction;
            else
                away.Normalize();

            float proximity = 1f - Mathf.Clamp01(hit.distance / rayLength);
            avoidance += away * (proximity * strength);
        }

        return avoidance;
    }

    private Vector3 GetMapCenter()
    {
        if (mapCenter != null)
            return mapCenter.position;

        return Vector3.zero;
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private Vector3 Flatten(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }
}
