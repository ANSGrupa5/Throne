using UnityEngine;

public class PlayerVehicleInput : MonoBehaviour, IVehicleCommandSource
{
    public VehicleCommand GetCommand()
    {
        float turn = Input.GetAxisRaw("Horizontal");
        return new VehicleCommand(turn, false);
    }
}
