
using UnityEngine;
using UnityEngine.InputSystem;

public class HoverCursorChanger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera raycastCamera;

    [Header("Cursor Textures")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D interactableCursor;

    [Header("Cursor Settings")]
    [SerializeField] private Vector2 defaultHotspot = Vector2.zero;
    [SerializeField] private Vector2 interactableHotspot = Vector2.zero;
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float maxRayDistance = 1000f;
    [SerializeField] private QueryTriggerInteraction triggerInteraction =
        QueryTriggerInteraction.Ignore;

    [Header("Debug")]
    [SerializeField] private bool showDebugRay;
    [SerializeField] private bool logHits;

    private bool isHoveringInteractable;

    private void Awake()
    {
        if (raycastCamera == null)
            raycastCamera = Camera.main;

        SetDefaultCursor();
    }

    private void Update()
    {
        bool hoveringNow = CheckForInteractable();

        if (hoveringNow == isHoveringInteractable)
            return;

        isHoveringInteractable = hoveringNow;

        if (isHoveringInteractable)
        {
            Cursor.SetCursor(
                interactableCursor,
                interactableHotspot,
                cursorMode
            );
        }
        else
        {
            SetDefaultCursor();
        }
    }

    private bool CheckForInteractable()
    {
        if (raycastCamera == null || Mouse.current == null)
            return false;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = raycastCamera.ScreenPointToRay(mousePosition);

        if (showDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * maxRayDistance,
                Color.yellow);

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxRayDistance,
                interactableLayer,
                triggerInteraction))
        {
            return false;
        }

        if (logHits)
            Debug.Log("Cursor hit: " + hit.collider.name, hit.collider);

        return true;
    }

    private void SetDefaultCursor()
    {
        Cursor.SetCursor(defaultCursor, defaultHotspot, cursorMode);
    }

    private void OnDisable()
    {
        isHoveringInteractable = false;
        SetDefaultCursor();
    }
}
