using UnityEngine;

[RequireComponent(typeof(Behaviour))]
public class PlayerVehicleInput : MonoBehaviour, IVehicleInput
{
    public void GetInputs(out float forward, out float turn)
    {
        forward = Input.GetAxisRaw("Vertical");
        turn = Input.GetAxisRaw("Horizontal");
    }
}
