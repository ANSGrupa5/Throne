using UnityEngine;

public class PlayerVehicleInput : MonoBehaviour, IVehicleCommandSource
{
    public VehicleCommand GetCommand()
    {
        float turn = 0f;

        if (Input.GetKey(InputManager.Instance.TurnLeft))
            turn -= 1f;

        if (Input.GetKey(InputManager.Instance.TurnRight))
            turn += 1f;

        return new VehicleCommand(turn, false);
    }
}
