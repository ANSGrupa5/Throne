using UnityEngine;

enum Axis
{
    X, //0
    Y, //1
    Z  //2
}

public class RotatePreview : MonoBehaviour
{
    [SerializeField] private float speed = 25f;

    [SerializeField] private int rotateAxis = (int)Axis.Y;

    private Vector3 rotationAxis = Vector3.up;

    void OnValidate()
    {
        CacheRotationAxis();
    }

    void Awake()
    {
        CacheRotationAxis();
    }

    private void CacheRotationAxis()
    {
        switch(rotateAxis)
        {
            case (int)Axis.X:
                rotationAxis = Vector3.right;
                break;

            case (int)Axis.Y:
                rotationAxis = Vector3.up;
                break;

            case (int)Axis.Z:
                rotationAxis = Vector3.forward;
                break;

            default:
                rotationAxis = Vector3.up;
                break;
        }
    }

    void Update()
    {
        transform.Rotate(rotationAxis * speed * Time.unscaledDeltaTime, Space.Self);
    }
}
