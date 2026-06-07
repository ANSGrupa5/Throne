using System;
using TMPro;
using UnityEngine;

public class DistanceTracker : MonoBehaviour
{
    public static DistanceTracker Instance;

    [Header("Target")]
    public Transform target;

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

    private void Start()
    {
        
    }

    private void Update()
    {
        if (target != null)
            CalculateDistance();
    }

    public void GetTarget()
    {
        GameObject playerObject = GameObject.Find("motorFINAL2_WORKING(Clone)");
        if (playerObject == null)
            playerObject = GameObject.Find("motor22(Clone)");

        SetTarget(playerObject != null ? playerObject.transform : null);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        totalDistance = 0f;
        lastPosition = target != null ? target.position : Vector3.zero;
    }

    void CalculateDistance()
    {
        float distance = Vector3.Distance(target.position, lastPosition);
        if( distance < 25f) //don't count the distance between death position and respawn position
            totalDistance += distance;
        lastPosition = target.position;
        Debug.Log("Updated distance... total distance: " + totalDistance);
    }

    public float GetTotalDistance()
    {
        return totalDistance;
    }
}
