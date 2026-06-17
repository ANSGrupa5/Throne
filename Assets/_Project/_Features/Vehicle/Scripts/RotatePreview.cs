using UnityEngine;

enum Axis
{
    X,
    Y,
    Z
}

public class RotatePreview : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private int rotateAxis = (int)Axis.Y;

    private void Update()
    {
        float delta = speed * Time.deltaTime;

        switch (rotateAxis)
        {
            case (int)Axis.X:
                transform.Rotate(delta, 0f, 0f);
                break;
            case (int)Axis.Z:
                transform.Rotate(0f, 0f, delta);
                break;
            default:
                transform.Rotate(0f, delta, 0f);
                break;
        }
    }
}
