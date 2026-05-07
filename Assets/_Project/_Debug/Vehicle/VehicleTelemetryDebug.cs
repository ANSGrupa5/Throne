using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class VehicleTelemetryDebug : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private WheelCollider wheelFront;
    [SerializeField] private WheelCollider wheelBack;
    [SerializeField] private Transform visualRoot;

    [Header("Logging")]
    [SerializeField] private bool logContinuously = true;
    [SerializeField, Min(0.05f)] private float logInterval = 0.25f;
    [SerializeField] private KeyCode snapshotKey = KeyCode.F8;
    [SerializeField] private bool writeToFile = false;
    [SerializeField] private bool writeToProjectLogs = false;
    [SerializeField] private string fileName = "vehicle_debug.log";

    private float _nextLogTime;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!Application.isPlaying)
            return;

        if (Input.GetKeyDown(snapshotKey))
            LogSnapshot("key");

        if (logContinuously && Time.time >= _nextLogTime)
        {
            _nextLogTime = Time.time + logInterval;
            LogSnapshot("interval");
        }
    }

    private void LogSnapshot(string reason)
    {
        if (rb == null)
            return;

        StringBuilder sb = new StringBuilder(1024);
        sb.AppendLine($"[VehicleTelemetry:{reason}] t={Time.time:F2} frame={Time.frameCount}");

        Vector3 vel = rb.linearVelocity;
        Vector3 ang = rb.angularVelocity;
        float speed = vel.magnitude;
        float yawVel = Vector3.Dot(ang, transform.up);

        sb.AppendLine($"pos={Fmt(transform.position)} rot={Fmt(transform.eulerAngles)}");
        sb.AppendLine($"vel={Fmt(vel)} speed={speed:F2} angVel={Fmt(ang)} yawVel={yawVel:F2}");
        sb.AppendLine($"mass={rb.mass:F2} angularDamping={rb.angularDamping:F2} com={Fmt(rb.centerOfMass)}");
        sb.AppendLine($"inertiaTensor={Fmt(rb.inertiaTensor)} inertiaRot={Fmt(rb.inertiaTensorRotation.eulerAngles)}");

        if (visualRoot != null)
            sb.AppendLine($"visualRoot rot={Fmt(visualRoot.rotation.eulerAngles)} pos={Fmt(visualRoot.position)}");

        AppendWheel(sb, "Front", wheelFront);
        AppendWheel(sb, "Back", wheelBack);

        string output = sb.ToString();
        Debug.Log(output, this);

        if (writeToFile)
        {
            string path = Path.Combine(Application.persistentDataPath, fileName);
            File.AppendAllText(path, output + "\n");
        }

        if (writeToProjectLogs)
        {
            string logsDir = Path.Combine(Application.dataPath, "_Project", "_Debug", "Logs");
            Directory.CreateDirectory(logsDir);
            string path = Path.Combine(logsDir, fileName);
            File.AppendAllText(path, output + "\n");
        }
    }

    private static void AppendWheel(StringBuilder sb, string label, WheelCollider wheel)
    {
        if (wheel == null)
        {
            sb.AppendLine($"{label}: null");
            return;
        }

        wheel.GetWorldPose(out Vector3 pos, out Quaternion rot);
        bool grounded = wheel.GetGroundHit(out WheelHit hit);
        sb.AppendLine($"{label}: steer={wheel.steerAngle:F2} rpm={wheel.rpm:F1} motor={wheel.motorTorque:F1} brake={wheel.brakeTorque:F1} grounded={grounded}");
        sb.AppendLine($"  pose pos={Fmt(pos)} rot={Fmt(rot.eulerAngles)}");
        sb.AppendLine($"  radius={wheel.radius:F3} suspDist={wheel.suspensionDistance:F3}");

        WheelFrictionCurve ff = wheel.forwardFriction;
        WheelFrictionCurve sf = wheel.sidewaysFriction;
        sb.AppendLine($"  fricF ext=({ff.extremumSlip:F2},{ff.extremumValue:F2}) asym=({ff.asymptoteSlip:F2},{ff.asymptoteValue:F2}) stiff={ff.stiffness:F2}");
        sb.AppendLine($"  fricS ext=({sf.extremumSlip:F2},{sf.extremumValue:F2}) asym=({sf.asymptoteSlip:F2},{sf.asymptoteValue:F2}) stiff={sf.stiffness:F2}");

        if (grounded)
        {
            sb.AppendLine($"  hit point={Fmt(hit.point)} normal={Fmt(hit.normal)} force={hit.force:F1}");
            sb.AppendLine($"  slip fwd={hit.forwardSlip:F2} side={hit.sidewaysSlip:F2}");
            sb.AppendLine($"  dirs fwd={Fmt(hit.forwardDir)} side={Fmt(hit.sidewaysDir)}");
        }
    }

    private static string Fmt(Vector3 v)
    {
        return string.Format(CultureInfo.InvariantCulture, "({0:F2},{1:F2},{2:F2})", v.x, v.y, v.z);
    }
}
