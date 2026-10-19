using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class GrabbableThrower : MonoBehaviour
{
    [Header("Grab Settings")]
    [Tooltip("Layer mask of objects that can be grabbed and thrown.")]
    public LayerMask grabbableLayer;

    [Tooltip("Distance to raycast from the camera when searching for an object.")]
    public float grabRange = 5f;

    [Tooltip("Safety drop: if the object cannot reach the hold point by this much, it is let go.")]
    public float maxHoldDistance = 6f;

    [Tooltip("How fast the held object chases the hold point under the mouse cursor. Lower = heavier.")]
    public float holdMoveSpeed = 20f;

    [Tooltip("Angular damping applied to the held object while grabbed.")]
    public float holdAngularDamping = 15f;

    [Tooltip("Drag applied to the held object while grabbed.")]
    public float holdDrag = 10f;

    [Header("Throw Settings")]
    [Tooltip("Force applied when throwing the object. The higher this is, the further it flies.")]
    public float throwForce = 15f;

    [Tooltip("When enabled, the throw keeps some of the momentum of the held object.")]
    public bool applyVelocityCompensation = true;

    [Header("Debug")]
    public bool showDebugRay;

    // Below this distance the object counts as being at its hold point. The
    // velocity is zeroed instead of pushed towards the target, so the object
    // does not vibrate against the surface it is resting on.
    private const float HoldDeadZone = 0.005f;

    private Rigidbody heldRigidbody;
    private float originalLinearDamping;
    private float originalAngularDamping;

    // Hold point: the world position under the cursor, kept on the plane that
    // runs through the object at the depth it was grabbed at. So the object
    // travels with the cursor, up and down included.
    private Vector3 holdPosition;
    private float holdDepth;

    private bool isHolding;

    private void Update()
    {
        if (Mouse.current == null)
        {
            return;
        }

        if (GameSettings.MenuOpen)
        {
            if (isHolding)
            {
                ReleaseObject();
            }

            return;
        }

        // Grabbing works only with the left button: pressing it picks an
        // object up and it stays up as long as the button is held.
        if (!isHolding)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryGrab();
            }

            return;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            // Let go of the button: put the object down where it is.
            ReleaseObject();
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            // Right button while holding: throw it.
            ReleaseObject(true);
        }
    }

    private void FixedUpdate()
    {
        if (!isHolding || heldRigidbody == null)
        {
            return;
        }

        Vector3 target = HoldPoint();
        Vector3 direction = target - heldRigidbody.position;

        if (direction.magnitude > maxHoldDistance)
        {
            ReleaseObject();
            return;
        }

        if (direction.sqrMagnitude <= HoldDeadZone * HoldDeadZone)
        {
            heldRigidbody.linearVelocity = Vector3.zero;
        }
        else
        {
            heldRigidbody.linearVelocity = direction * holdMoveSpeed;
        }

        heldRigidbody.angularVelocity = Vector3.Lerp(
            heldRigidbody.angularVelocity,
            Vector3.zero,
            holdAngularDamping * Time.fixedDeltaTime
        );
    }

    // World point under the cursor, at the depth the object was grabbed at,
    // never inside other geometry.
    private Vector3 HoldPoint()
    {
        if (Mouse.current == null)
        {
            return holdPosition;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return holdPosition;
        }

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Vector3 target = ray.origin + ray.direction * holdDepth;

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * holdDepth, Color.cyan);
        }

        // Stop the hold point at the first thing in the way, so the object is
        // never pulled into a table or a wall. The ray starts inside the held
        // object, so its own collider cannot be hit.
        Vector3 offset = target - heldRigidbody.position;
        float distance = offset.magnitude;

        if (distance > 0f &&
            Physics.Raycast(
                heldRigidbody.position,
                offset / distance,
                out RaycastHit block,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore
            ) &&
            block.rigidbody != heldRigidbody)
        {
            target = heldRigidbody.position
                + (offset / distance) * Mathf.Max(0f, block.distance - 0.1f);
        }

        return target;
    }

    private void TryGrab()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * grabRange, Color.green, 0.1f);
        }

        if (!Physics.Raycast(ray, out RaycastHit hit, grabRange, grabbableLayer))
        {
            return;
        }

        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb == null || rb.isKinematic)
        {
            return;
        }

        heldRigidbody = rb;

        holdPosition = rb.position;
        holdDepth = Mathf.Max(
            0.5f,
            Vector3.Dot(
                rb.position - cam.transform.position,
                cam.transform.forward
            )
        );

        originalLinearDamping = heldRigidbody.linearDamping;
        originalAngularDamping = heldRigidbody.angularDamping;

        heldRigidbody.useGravity = false;
        heldRigidbody.linearDamping = holdDrag;
        heldRigidbody.angularDamping = holdAngularDamping;
        heldRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

        isHolding = true;
    }

    private void ReleaseObject(bool throwObject = false)
    {
        if (heldRigidbody == null)
        {
            isHolding = false;
            return;
        }

        heldRigidbody.useGravity = true;
        heldRigidbody.linearDamping = originalLinearDamping;
        heldRigidbody.angularDamping = originalAngularDamping;
        heldRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

        if (throwObject)
        {
            Vector3 throwDirection = Camera.main != null
                ? Camera.main.transform.forward
                : transform.forward;

            Vector3 throwVelocity = throwDirection * throwForce;

            if (applyVelocityCompensation)
            {
                throwVelocity += heldRigidbody.linearVelocity * 0.5f;
            }

            heldRigidbody.linearVelocity = throwVelocity;
            heldRigidbody.angularVelocity = Random.insideUnitSphere * 5f;
        }

        heldRigidbody = null;
        isHolding = false;
    }
}
