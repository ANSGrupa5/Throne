using System.Collections.Generic;
using UnityEngine;

public class BotVehicleInput : MonoBehaviour, IVehicleCommandSource
{
    public enum BotDriveMode
    {
        Safety,
        TrailAvoidance,
        Intercept,
        Powerup,
        Center,
        Roam
    }

    [System.Serializable]
    public struct RayDebugSample
    {
        public string label;
        public Vector3 origin;
        public Vector3 direction;
        public float length;
        public bool hit;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public Color color;
    }

    public struct DebugSnapshot
    {
        public BotDriveMode mode;
        public bool hasTarget;
        public Vector3 targetPoint;
        public float turn;
        public RayDebugSample[] rays;
    }

    [Header("Priority 1 - Map Safety")]
    [SerializeField] private LayerMask mapBoundaryMask;
    [SerializeField] private LayerMask suddenDeathMask;
    [SerializeField, Min(0.5f)] private float safetyRayLength = 12f;
    [SerializeField, Range(5f, 75f)] private float safetyRayAngle = 30f;
    [SerializeField, Min(0f)] private float safetyAvoidanceStrength = 3f;

    [Header("Priority 2 - Trail Avoidance")]
    [SerializeField] private LayerMask trailMask;
    [SerializeField, Min(0.5f)] private float trailRayLength = 10f;
    [SerializeField, Range(5f, 75f)] private float trailRayAngle = 25f;
    [SerializeField, Min(0f)] private float trailAvoidanceStrength = 2.25f;

    [Header("Priority 3 - Intercept")]
    [SerializeField, Min(0f)] private float playerSearchRadius = 150f;
    [SerializeField, Min(0f)] private float blockAheadDistance = 8f;
    [SerializeField, Min(0f)] private float lateralBlockOffset = 2.5f;
    [SerializeField, Min(0f)] private float interceptLeadTime = 0.55f;
    [SerializeField, Min(0f)] private float interceptBuffer = 5f;
    [SerializeField, Min(0.1f)] private float interceptMaxPredictionTime = 4f;
    [SerializeField, Min(0f)] private float interceptSteeringStrength = 2.2f;

    [Header("Priority 4 - Powerups")]
    [SerializeField] private LayerMask powerupMask;
    [SerializeField, Min(0f)] private float powerupSearchRadius = 500f;
    [SerializeField, Min(0f)] private float powerupSteeringStrength = 1.55f;

    [Header("Priority 5 - Center")]
    [SerializeField] private Transform mapCenter;
    [SerializeField, Min(0f)] private float centerRoamRadius = 12f;
    [SerializeField, Min(0f)] private float centerSteeringStrength = 1.4f;
    [SerializeField, Min(0.1f)] private float boundaryAwarenessRadius = 14f;
    [SerializeField, Min(0f)] private float boundaryEscapeRayLength = 18f;
    [SerializeField, Min(0f)] private float boundaryEscapeStrength = 3.5f;
    [SerializeField, Min(0f)] private float roamDirectionDuration = 3f;
    [SerializeField, Min(0f)] private float roamSteeringStrength = 1.15f;

    [Header("General")]
    [SerializeField, Min(0f)] private float wanderStrength = 0.04f;
    [SerializeField, Min(0.1f)] private float wanderChangeInterval = 1.25f;

    private float _wanderTurn;
    private float _nextWanderChangeTime;
    private float _roamUntilTime;
    private Vector3 _roamDirection;
    private Rigidbody _rb;
    private VehicleLife _ownerLife;
    private DebugSnapshot _debugSnapshot;
    private bool _hasDebugSnapshot;
    private Vector3 _cachedEstimatedCenter;
    private bool _hasCachedEstimatedCenter;

    public void ConfigureRuntime(LayerMask runtimeMapBoundaryMask, LayerMask runtimeSuddenDeathMask, LayerMask runtimeTrailMask, LayerMask runtimePowerupMask, Transform runtimeMapCenter)
    {
        if (runtimeMapBoundaryMask.value != 0)
            mapBoundaryMask = runtimeMapBoundaryMask;

        if (runtimeSuddenDeathMask.value != 0)
            suddenDeathMask = runtimeSuddenDeathMask;

        if (runtimeTrailMask.value != 0)
            trailMask = runtimeTrailMask;

        if (runtimePowerupMask.value != 0)
            powerupMask = runtimePowerupMask;

        if (runtimeMapCenter != null)
            mapCenter = runtimeMapCenter;
    }

    public bool TryGetDebugSnapshot(out DebugSnapshot snapshot)
    {
        snapshot = _debugSnapshot;
        return _hasDebugSnapshot;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _ownerLife = GetComponent<VehicleLife>();
    }

    private void OnEnable()
    {
        ApplyFallbackConfiguration();
        PickNewWanderTurn();
        if (_ownerLife == null)
            _ownerLife = GetComponent<VehicleLife>();
    }

    public VehicleCommand GetCommand()
    {
        UpdateWander();
        _hasDebugSnapshot = false;

        Vector3 position = transform.position;
        List<RayDebugSample> debugRays = new List<RayDebugSample>(16);

        if (TryAvoidLayer(mapBoundaryMask.value | suddenDeathMask.value, safetyRayLength, safetyRayAngle, safetyAvoidanceStrength, Color.red, "P1 Safety", out Vector3 safetyTurn, debugRays))
        {
            float turn = ApplyWanderAndClamp(safetyTurn);
            CommitDebug(BotDriveMode.Safety, turn, debugRays, false, default);
            return new VehicleCommand(turn, false);
        }

        if (TryAvoidLayer(trailMask.value, trailRayLength, trailRayAngle, trailAvoidanceStrength, Color.magenta, "P2 Trail", out Vector3 trailTurn, debugRays))
        {
            float turn = ApplyWanderAndClamp(trailTurn);
            CommitDebug(BotDriveMode.TrailAvoidance, turn, debugRays, false, default);
            return new VehicleCommand(turn, false);
        }

        Vector3 targetPoint;
        BotDriveMode mode;
        float targetStrength;

        Transform player = FindClosestPlayer(playerSearchRadius);
        if (player != null)
        {
            targetPoint = ComputeInterceptPoint(player);
            mode = BotDriveMode.Intercept;
            targetStrength = interceptSteeringStrength;
            CancelRoam();
        }
        else if (TryFindNearestPowerup(position, out Vector3 powerupPoint))
        {
            targetPoint = powerupPoint;
            mode = BotDriveMode.Powerup;
            targetStrength = powerupSteeringStrength;
            CancelRoam();
        }
        else
        {
            Vector3 center = GetMapCenter();
            Vector3 boundaryEscape = ComputeBoundaryEscape(position);
            float distanceToCenter = FlatDistance(position, center);

            if (distanceToCenter <= centerRoamRadius)
            {
                if (Time.time >= _roamUntilTime || _roamDirection.sqrMagnitude < 0.0001f)
                    BeginRoam();

                targetPoint = position + _roamDirection;
                mode = BotDriveMode.Roam;
                targetStrength = roamSteeringStrength;
            }
            else
            {
                CancelRoam();
                targetPoint = center;
                if (boundaryEscape.sqrMagnitude > 0.0001f)
                    targetPoint += boundaryEscape * boundaryEscapeStrength;
                mode = BotDriveMode.Center;
                targetStrength = centerSteeringStrength;
            }
        }

        Vector3 targetVector = targetPoint - position;
        targetVector.y = 0f;
        if (targetVector.sqrMagnitude < 0.0001f)
            targetVector = Flatten(transform.forward);

        Vector3 combined = targetVector.normalized * targetStrength;
        combined += Flatten(transform.right) * _wanderTurn;

        Vector3 local = transform.InverseTransformDirection(combined.normalized);
        float finalTurn = Mathf.Clamp(local.x, -1f, 1f);

            CommitDebug(mode, finalTurn, debugRays, true, targetPoint);
            return new VehicleCommand(finalTurn, false);
        }

    private float ApplyWanderAndClamp(Vector3 turnVector)
    {
        Vector3 combined = turnVector + Flatten(transform.right) * _wanderTurn;
        Vector3 local = transform.InverseTransformDirection(combined.normalized);
        return Mathf.Clamp(local.x, -1f, 1f);
    }

    private void CommitDebug(BotDriveMode mode, float turn, List<RayDebugSample> rays, bool hasTarget, Vector3 targetPoint)
    {
        _debugSnapshot = new DebugSnapshot
        {
            mode = mode,
            hasTarget = hasTarget,
            targetPoint = targetPoint,
            turn = turn,
            rays = rays.ToArray()
        };
        _hasDebugSnapshot = true;
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

    private void ApplyFallbackConfiguration()
    {
        if (mapBoundaryMask.value == 0)
            mapBoundaryMask = ResolveMask("Map Boundry", "MapBoundary", "Boundary", "ArenaBoundary", "Wall", "Walls");

        if (suddenDeathMask.value == 0)
            suddenDeathMask = ResolveMask("Dead Zone", "SuddenDeath", "DeathZone", "KillZone", "Lava");

        if (trailMask.value == 0)
            trailMask = ResolveMask("Trail");

        if (powerupMask.value == 0)
            powerupMask = ResolveMask("Power Ups", "Powerup", "PowerUp", "Pickup");

        if (mapCenter == null)
            mapCenter = ResolveMapCenter();

        if (mapCenter == null)
            _hasCachedEstimatedCenter = TryEstimateMapCenterFromBounds(out _cachedEstimatedCenter);
    }

    private LayerMask ResolveMask(params string[] layerNames)
    {
        int mask = 0;
        for (int i = 0; i < layerNames.Length; i++)
        {
            int layer = LayerMask.NameToLayer(layerNames[i]);
            if (layer >= 0)
                mask |= 1 << layer;
        }

        return mask;
    }

    private Transform ResolveMapCenter()
    {
        GameObject named = GameObject.Find("MapCenter");
        if (named != null)
            return named.transform;

        GameObject arenaCenter = GameObject.Find("ArenaCenter");
        if (arenaCenter != null)
            return arenaCenter.transform;

        return null;
    }

    private void CancelRoam()
    {
        _roamUntilTime = 0f;
    }

    private Vector3 ComputeInterceptPoint(Transform player)
    {
        Vector3 botPosition = transform.position;
        Vector3 playerPosition = player.position;
        Vector3 playerForward = player.forward;
        Vector3 playerVelocity = Vector3.zero;
        Vector3 botVelocity = Vector3.zero;

        if (_rb != null)
            botVelocity = Flatten(_rb.linearVelocity);

        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
            playerVelocity = Flatten(playerRb.linearVelocity);

        if (playerVelocity.sqrMagnitude > 0.01f)
            playerForward = playerVelocity.normalized;

        Vector3 flatForward = Flatten(playerForward);
        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Flatten(player.forward);
        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Vector3.forward;
        flatForward.Normalize();

        Vector3 relativePosition = playerPosition - botPosition;
        Vector3 relativeVelocity = playerVelocity - botVelocity;
        float predictionTime = 0f;
        float denom = relativeVelocity.sqrMagnitude;
        if (denom > 0.0001f)
        {
            predictionTime = -Vector3.Dot(relativePosition, relativeVelocity) / denom;
            predictionTime = Mathf.Clamp(predictionTime, 0f, interceptMaxPredictionTime);
        }

        Vector3 leadPoint = playerPosition + playerVelocity * Mathf.Max(interceptLeadTime, predictionTime);
        Vector3 right = Vector3.Cross(Vector3.up, flatForward).normalized;
        Vector3 interceptPoint = leadPoint + flatForward * interceptBuffer;

        Vector3 relativeToPlayer = botPosition - playerPosition;
        relativeToPlayer.y = 0f;
        float side = Mathf.Sign(Vector3.Dot(relativeToPlayer, right));
        if (Mathf.Abs(side) < 0.01f)
            side = Random.value < 0.5f ? -1f : 1f;

        return interceptPoint + flatForward * blockAheadDistance + right * side * lateralBlockOffset;
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

    private bool TryAvoidLayer(int mask, float rayLength, float rayAngle, float strength, Color debugColor, string debugLabel, out Vector3 avoidanceTurn, List<RayDebugSample> debugRays)
    {
        avoidanceTurn = Vector3.zero;
        if (mask == 0 || rayLength <= 0f || strength <= 0f)
            return false;

        Vector3 origin = transform.position + transform.up * 0.25f;
        Vector3 forward = Flatten(transform.forward);
        if (forward.sqrMagnitude < 0.001f)
            return false;
        forward.Normalize();

        Vector3 right = Flatten(transform.right);
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(Vector3.up, forward).normalized;
        else
            right.Normalize();

        float wideAngle = Mathf.Min(89f, rayAngle * 2.5f);
        float extremeAngle = Mathf.Min(89f, rayAngle * 3.5f);

        Vector3[] directions =
        {
            forward,
            Quaternion.AngleAxis(rayAngle, Vector3.up) * forward,
            Quaternion.AngleAxis(-rayAngle, Vector3.up) * forward,
            Quaternion.AngleAxis(wideAngle, Vector3.up) * forward,
            Quaternion.AngleAxis(-wideAngle, Vector3.up) * forward,
            Quaternion.AngleAxis(extremeAngle, Vector3.up) * forward,
            Quaternion.AngleAxis(-extremeAngle, Vector3.up) * forward
        };

        bool hitAny = false;
        Vector3 weightedTurn = Vector3.zero;

        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 direction = directions[i];
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                continue;
            direction.Normalize();

            bool hit = Physics.Raycast(origin, direction, out RaycastHit rayHit, rayLength, mask, QueryTriggerInteraction.Collide);
            Vector3 turnContribution = Vector3.zero;
            Vector3 hitPoint = origin + direction * rayLength;
            Vector3 hitNormal = Vector3.zero;

            if (hit)
            {
                if (IsOwnTrailHit(rayHit.collider))
                {
                    debugRays.Add(new RayDebugSample
                    {
                        label = $"{debugLabel} [{i + 1}] (own ignored)",
                        origin = origin,
                        direction = direction,
                        length = rayLength,
                        hit = false,
                        hitPoint = hitPoint,
                        hitNormal = hitNormal,
                        color = new Color(debugColor.r, debugColor.g, debugColor.b, 0.2f)
                    });
                    continue;
                }

                hitAny = true;
                hitPoint = rayHit.point;
                hitNormal = rayHit.normal;

                float sideSign = Mathf.Sign(Vector3.Dot(Flatten(hitPoint - origin), right));
                if (Mathf.Abs(sideSign) < 0.01f)
                    sideSign = i % 2 == 0 ? 1f : -1f;

                if (i == 0)
                {
                    Vector3 away = Flatten(rayHit.normal);
                    if (away.sqrMagnitude < 0.001f)
                        away = -direction;
                    turnContribution = away;
                }
                else
                {
                    // Ray hits on the left should push right, and vice versa.
                    turnContribution = right * -sideSign;
                }

                float proximity = 1f - Mathf.Clamp01(rayHit.distance / rayLength);
                weightedTurn += turnContribution * (proximity * strength);
            }

            debugRays.Add(new RayDebugSample
            {
                label = $"{debugLabel} [{i + 1}]",
                origin = origin,
                direction = direction,
                length = rayLength,
                hit = hit,
                hitPoint = hitPoint,
                hitNormal = hitNormal,
                color = debugColor
            });
        }

        if (!hitAny)
            return false;

        avoidanceTurn = weightedTurn;
        return true;
    }

    private Vector3 GetMapCenter()
    {
        if (mapCenter != null)
            return mapCenter.position;

        if (_hasCachedEstimatedCenter)
            return _cachedEstimatedCenter;

        return Vector3.zero;
    }

    private bool TryEstimateMapCenterFromBounds(out Vector3 center)
    {
        center = Vector3.zero;

        Collider[] colliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
        int count = 0;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null)
                continue;

            if (!IsInMask(collider.gameObject.layer, mapBoundaryMask.value))
                continue;

            center += collider.bounds.center;
            count++;
        }

        if (count == 0)
            return false;

        center /= count;
        return true;
    }

    private Vector3 ComputeBoundaryEscape(Vector3 position)
    {
        int mask = mapBoundaryMask.value | suddenDeathMask.value;
        if (mask == 0)
            return Vector3.zero;

        Vector3 origin = position + Vector3.up * 0.25f;
        Collider[] nearby = Physics.OverlapSphere(origin, boundaryAwarenessRadius, mask, QueryTriggerInteraction.Collide);
        Vector3 escape = Vector3.zero;

        for (int i = 0; i < nearby.Length; i++)
        {
            Collider collider = nearby[i];
            if (collider == null)
                continue;

            Vector3 closest = collider.ClosestPoint(origin);
            Vector3 away = origin - closest;
            away.y = 0f;

            float sqrDistance = away.sqrMagnitude;
            if (sqrDistance < 0.0001f)
                continue;

            float proximity = 1f - Mathf.Clamp01(Mathf.Sqrt(sqrDistance) / boundaryAwarenessRadius);
            escape += away.normalized * proximity;
        }

        if (escape.sqrMagnitude > 0.0001f)
            return escape.normalized;

        Vector3[] directions =
        {
            transform.forward,
            -transform.forward,
            transform.right,
            -transform.right,
            (transform.forward + transform.right).normalized,
            (transform.forward - transform.right).normalized,
            (-transform.forward + transform.right).normalized,
            (-transform.forward - transform.right).normalized
        };

        Vector3 rayEscape = Vector3.zero;

        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 dir = Flatten(directions[i]);
            if (dir.sqrMagnitude < 0.001f)
                continue;
            dir.Normalize();

            if (!Physics.Raycast(origin, dir, out RaycastHit hit, boundaryEscapeRayLength, mask, QueryTriggerInteraction.Collide))
                continue;

            float proximity = 1f - Mathf.Clamp01(hit.distance / boundaryEscapeRayLength);
            Vector3 away = Flatten(hit.normal);
            if (away.sqrMagnitude < 0.001f)
                away = -dir;

            rayEscape += away.normalized * proximity;
        }

        return rayEscape.sqrMagnitude > 0.0001f ? rayEscape.normalized : Vector3.zero;
    }

    private bool IsOwnTrailHit(Collider hitCollider)
    {
        if (hitCollider == null)
            return false;

        string currentOwnerId = GetCurrentOwnerId();
        if (string.IsNullOrWhiteSpace(currentOwnerId))
            return false;

        TrailSegment trailSegment = hitCollider.GetComponent<TrailSegment>();
        if (trailSegment != null)
        {
            if (string.Equals(trailSegment.OwnerId, currentOwnerId, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        TrailSegment trailFromParent = hitCollider.GetComponentInParent<TrailSegment>();
        if (trailFromParent != null)
        {
            if (string.Equals(trailFromParent.OwnerId, currentOwnerId, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private string GetCurrentOwnerId()
    {
        if (_ownerLife == null)
            _ownerLife = GetComponent<VehicleLife>();

        if (_ownerLife != null && !string.IsNullOrWhiteSpace(_ownerLife.OwnerId))
            return _ownerLife.OwnerId.Trim();

        return string.Empty;
    }

    private bool IsInMask(int layer, int mask)
    {
        return (mask & (1 << layer)) != 0;
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
