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

        // Guard against NaN quaternions
        if (!IsValidQuaternion(desiredWorldRot) || !IsValidVector3(desiredWorldPos))
            return;

        if (parent != null)
        {
            visual.localPosition = parent.InverseTransformPoint(desiredWorldPos);

            Quaternion localWheelRot = Quaternion.Inverse(parent.rotation) * desiredWorldRot;
            if (!_offsetInitialized)
            {
                _localRotationOffset = Quaternion.Inverse(localWheelRot) * visual.localRotation;
                _offsetInitialized = true;
            }

            Quaternion targetRot = localWheelRot * _localRotationOffset;
            if (IsValidQuaternion(targetRot))
            {
                visual.localRotation = targetRot;
            }
        }
        else
        {
            visual.position = desiredWorldPos;

            if (!_offsetInitialized)
            {
                _localRotationOffset = Quaternion.Inverse(desiredWorldRot) * visual.rotation;
                _offsetInitialized = true;
            }

            Quaternion targetRot = desiredWorldRot * _localRotationOffset;
            if (IsValidQuaternion(targetRot))
            {
                visual.rotation = targetRot;
            }
        }
    }

    private bool IsValidQuaternion(Quaternion q)
    {
        return !float.IsNaN(q.x) && !float.IsNaN(q.y) && !float.IsNaN(q.z) && !float.IsNaN(q.w);
    }

    private bool IsValidVector3(Vector3 v)
    {
        return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z);
    }
}
