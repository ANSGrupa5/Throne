using UnityEngine;

public class PlayerVehicleInput : MonoBehaviour, IVehicleCommandSource
{
    private float _localTurn;

    private void Update()
    {
        _localTurn = Input.GetAxisRaw("Horizontal");
    }

    public VehicleCommand GetCommand()
    {
        return new VehicleCommand(_localTurn, false);
    }
}
