using UnityEngine;

[ExecuteAlways]
public class WheelVisualSync : MonoBehaviour
{
    public WheelCollider wheelCollider;
    public Transform visual;
    // Visual root that applies lean (e.g. LeanContainer). If null, uses visual.parent.
    public Transform inheritParent;

    private Quaternion _localRotationOffset;
    private bool _offsetInitialized;

    void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        if (wheelCollider == null || visual == null) return;

        wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        Transform parent = visual.parent;
        Transform leanRoot = inheritParent != null ? inheritParent : parent;
        Transform poseSpace = wheelCollider.attachedRigidbody != null
            ? wheelCollider.attachedRigidbody.transform
            : null;

        Vector3 localPos = pos;
        Quaternion localRot = rot;
        if (poseSpace != null)
        {
            localPos = poseSpace.InverseTransformPoint(pos);
            localRot = Quaternion.Inverse(poseSpace.rotation) * rot;
        }

        Vector3 desiredWorldPos = leanRoot != null ? leanRoot.TransformPoint(localPos) : pos;
        Quaternion desiredWorldRot = leanRoot != null ? leanRoot.rotation * localRot : rot;

        if (parent != null)
        {
            visual.localPosition = parent.InverseTransformPoint(desiredWorldPos);

            Quaternion localWheelRot = Quaternion.Inverse(parent.rotation) * desiredWorldRot;
            if (!_offsetInitialized)
            {
                _localRotationOffset = Quaternion.Inverse(localWheelRot) * visual.localRotation;
                _offsetInitialized = true;
            }

            visual.localRotation = localWheelRot * _localRotationOffset;
        }
        else
        {
            visual.position = desiredWorldPos;

            if (!_offsetInitialized)
            {
                _localRotationOffset = Quaternion.Inverse(desiredWorldRot) * visual.rotation;
                _offsetInitialized = true;
            }

            visual.rotation = desiredWorldRot * _localRotationOffset;
        }
    }
}
