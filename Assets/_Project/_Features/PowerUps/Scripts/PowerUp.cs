using UnityEngine;

public enum PowerUpType
{
    SpeedUp,
    SlowDownOthers,
    Invincibility
}

public class PowerUp : MonoBehaviour
{
    public PowerUpType powerUpType;

    [Header("Speed Up Settings")]
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float speedDuration = 5f;

    [Header("Slow Down Settings")]
    [SerializeField] private float slowMultiplier = 0.5f;
    [SerializeField] private float slowDuration = 2.5f;

    [Header("Invincibility Settings")]
    [SerializeField] private float invincibilityDuration = 5f;

    private void OnTriggerEnter(Collider other)
    {
        // Sprawdzamy czy pobrany został VehicleLife z collidera lub Rigidbody
        VehicleLife vehicleLife = other.GetComponentInParent<VehicleLife>();
        if (vehicleLife == null && other.attachedRigidbody != null)
        {
            vehicleLife = other.attachedRigidbody.GetComponent<VehicleLife>();
        }

        if (vehicleLife == null || vehicleLife.IsDead)
            return;

        ApplyEffect(vehicleLife);
        
        Destroy(gameObject);
    }

    private void ApplyEffect(VehicleLife collectorLife)
    {
        VehicleController collectorController = collectorLife.GetComponent<VehicleController>();

        switch (powerUpType)
        {
            case PowerUpType.SpeedUp:
                if (collectorController != null)
                {
                    collectorController.ApplySpeedModifier(speedMultiplier, speedDuration);
                }
                break;

            case PowerUpType.SlowDownOthers:
                VehicleController[] allControllers = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
                foreach (var controller in allControllers)
                {
                    // Pomijamy tego, który podniósł
                    if (controller == collectorController)
                        continue;

                    VehicleLife otherLife = controller.GetComponent<VehicleLife>();
                    if (otherLife != null && otherLife.IsDead)
                        continue;

                    controller.ApplySpeedModifier(slowMultiplier, slowDuration);
                }
                break;

            case PowerUpType.Invincibility:
                collectorLife.GrantInvulnerability(invincibilityDuration);
                break;
        }
    }
}
