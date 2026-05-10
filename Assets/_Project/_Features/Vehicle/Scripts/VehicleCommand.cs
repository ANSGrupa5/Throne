using UnityEngine;

public struct VehicleCommand
{
    public float turn;
    public bool boost;

    public VehicleCommand(float turn, bool boost)
    {
        this.turn = Mathf.Clamp(turn, -1f, 1f);
        this.boost = boost;
    }

    public static VehicleCommand Neutral => new VehicleCommand(0f, false);
}
