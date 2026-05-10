using UnityEngine;

public class VehicleCameraController : MonoBehaviour
{
    [System.Serializable]
    public struct CameraPreset
    {
        public string label;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
    }

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float transitionSpeed = 6f;
    [SerializeField] private CameraPreset[] presets = new CameraPreset[]
    {
        new CameraPreset
        {
            label = "Follow",
            localPosition    = new Vector3(0f, 3.5f, -7f),
            localEulerAngles = new Vector3(15f, 0f, 0f)
        },
        new CameraPreset
        {
            label = "TopDown",
            localPosition    = new Vector3(0f, 14f, -3f),
            localEulerAngles = new Vector3(65f, 0f, 0f)
        }
    };

    private int _currentPreset;
    private Transform _spectatorTarget;
    private bool _useSpectatorTarget;

    public Transform CameraTransform => cameraTransform;

    // Handles preset switching input and updates camera interpolation.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            // Cycle through presets
            _currentPreset = (_currentPreset + 1) % presets.Length;

        SmoothTransition();
    }

    // Smoothly moves and rotates the camera toward the active preset.
    private void SmoothTransition()
    {
        if (_useSpectatorTarget && _spectatorTarget != null)
        {
            cameraTransform.position = Vector3.Lerp(
                cameraTransform.position,
                _spectatorTarget.position,
                transitionSpeed * Time.deltaTime
            );

            cameraTransform.rotation = Quaternion.Slerp(
                cameraTransform.rotation,
                _spectatorTarget.rotation,
                transitionSpeed * Time.deltaTime
            );
            return;
        }

        CameraPreset target = presets[_currentPreset];

        // Lerp/Slerp each frame creates an ease-out effect: fast at first, slowing as it approaches the target.
        // Not frame-rate independent at high speeds, but acceptable for a camera transition.
        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition,
            target.localPosition,
            transitionSpeed * Time.deltaTime
        );

        // Slerp is used instead of Lerp for rotation to follow the shortest arc on the unit sphere
        cameraTransform.localRotation = Quaternion.Slerp(
            cameraTransform.localRotation,
            Quaternion.Euler(target.localEulerAngles),
            transitionSpeed * Time.deltaTime
        );
    }

    public void SetPresetByLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label) || presets == null)
            return;

        for (int i = 0; i < presets.Length; i++)
        {
            if (string.Equals(presets[i].label, label, System.StringComparison.OrdinalIgnoreCase))
            {
                _currentPreset = i;
                _useSpectatorTarget = false;
                _spectatorTarget = null;
                return;
            }
        }
    }

    public void SetSpectatorTarget(Transform target)
    {
        _spectatorTarget = target;
        _useSpectatorTarget = target != null;
    }

    public void ClearSpectatorTarget()
    {
        _spectatorTarget = null;
        _useSpectatorTarget = false;
    }
}
