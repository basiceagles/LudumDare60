using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Edge Scrolling")]
    [Tooltip("Thickness of the border zone in pixels.")]
    public float borderSize = 50f;
    [Tooltip("Degrees per second the camera rotates while the pointer is in the border zone.")]
    public float rotateSpeed = 60f;
    [Tooltip("Disable edge scrolling (e.g. while clicking on hotspots).")]
    public bool edgeScrollingEnabled = true;

    [Header("Rotation Limits (degrees)")]
    public float minUp = -30f;
    public float maxUp = 30f;
    public float minSide = -30f;
    public float maxSide = 30f;

    [Header("Sensitivity")]
    [Tooltip(
        "Multiplier for the rotation speed. The settings menu drives this " +
        "with its sensitivity slider; it starts from the saved setting."
    )]
    [Min(0f)]
    public float sensitivity = 1f;

    [Header("Right Mouse Zoom (toggle)")]
    [Tooltip("Click the right mouse button to toggle between the default and zoomed FOV.")]
    public bool rmbZoomEnabled = true;
    [Tooltip("FOV while zoomed in.")]
    public float zoomFov = 20f;
    [Tooltip("How quickly the FOV eases toward the target. Higher = snappier.")]
    public float zoomSmooth = 8f;
    [Tooltip("Disable edge-scroll rotation while zoomed in.")]
    public bool blockRotationWhileZoomed = true;

    private float pitch;
    private float yaw;
    private float defaultFov;
    private float targetFov;
    private bool isZoomed;
    private float basePitch;
    private float baseYaw;
    private float baseRoll;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            defaultFov = cam.fieldOfView;
            targetFov = defaultFov;
        }

        Vector3 e = transform.eulerAngles;
        basePitch = NormalizeAngle(e.x);
        baseYaw = NormalizeAngle(e.y);
        baseRoll = NormalizeAngle(e.z);

        // The saved setting wins over the Inspector value.
        sensitivity = GameSettings.CameraSensitivity;
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }

    void Update()
    {
        if (GameSettings.MenuOpen || Mouse.current == null) return;

        if (rmbZoomEnabled && Mouse.current.rightButton.wasPressedThisFrame)
        {
            isZoomed = !isZoomed;
            targetFov = isZoomed ? zoomFov : defaultFov;
        }

        bool canRotate = edgeScrollingEnabled && !(blockRotationWhileZoomed && isZoomed);
        if (canRotate)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            float horizontal = 0f;
            float vertical = 0f;

            if (mousePos.x <= borderSize) horizontal = -1f;
            else if (mousePos.x >= Screen.width - borderSize) horizontal = 1f;

            if (mousePos.y <= borderSize) vertical = 1f;
            else if (mousePos.y >= Screen.height - borderSize) vertical = -1f;

            float speed = rotateSpeed * sensitivity;
            yaw = Mathf.Clamp(yaw + horizontal * speed * Time.deltaTime, minSide, maxSide);
            pitch = Mathf.Clamp(pitch + vertical * speed * Time.deltaTime, minUp, maxUp);
        }

        if (cam != null)
        {
            float t = 1f - Mathf.Exp(-zoomSmooth * Time.deltaTime);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, t);
        }

        transform.rotation = Quaternion.Euler(basePitch + pitch, baseYaw + yaw, baseRoll);
    }
}
