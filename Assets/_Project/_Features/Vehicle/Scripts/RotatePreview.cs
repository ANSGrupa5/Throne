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

    private float X;
    private float Y;
    private float Z;

    void Start()
    {
        switch(rotateAxis)
        {
            case (int)Axis.X:
                X = speed * Time.deltaTime;
                Y = 0f;
                Z = 0f;
                break;

            case (int)Axis.Y:
                X = 0f;
                Y = speed * Time.deltaTime;
                Z = 0f;
                break;

            case (int)Axis.Z:
                X = 0f;
                Y = 0f;
                Z = speed * Time.deltaTime;
                break;
        }
    }

    void Update()
    {
        transform.Rotate(X, Y, Z);
    }
}
