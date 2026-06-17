using UnityEngine;

public class DistanceTracker : MonoBehaviour
{
    public static DistanceTracker Instance;

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Stats")]
    public float totalDistance;

    private Vector3 lastPosition;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (target != null)
            CalculateDistance();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        totalDistance = 0f;

        if (target != null)
            lastPosition = target.position;
    }

    public void ClearTarget(Transform oldTarget)
    {
        if (target == oldTarget)
            target = null;
    }

    public void GetTarget()
    {
        SetTarget(target);
    }

    private void CalculateDistance()
    {
        float distance = Vector3.Distance(target.position, lastPosition);
        if (distance < 25f)
            totalDistance += distance;

        lastPosition = target.position;
    }

    public float GetTotalDistance()
    {
        return totalDistance;
    }
}
