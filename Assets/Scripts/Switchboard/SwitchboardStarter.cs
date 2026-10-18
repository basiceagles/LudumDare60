
using UnityEngine;
using UnityEngine.InputSystem;

// Arms the switchboard shift when the player right clicks an object on the
// chosen layer, then turns itself off so it only ever fires once.
public class SwitchboardStarter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SwitchboardGame switchboard;
    [SerializeField] private Camera playerCamera;

    [Header("Interaction")]
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float rayDistance = 5f;

    [Header("Behaviour")]
    [Tooltip("Turn this component off after it has started the shift.")]
    [SerializeField] private bool disableAfterUse = true;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (switchboard == null)
            switchboard = FindFirstObjectByType<SwitchboardGame>();

        // Nothing picked in the Inspector: fall back to the project's
        // "Interactable" layer when it exists.
        if (interactableLayer.value == 0)
        {
            int layer = LayerMask.NameToLayer("Interactable");

            if (layer >= 0)
                interactableLayer = 1 << layer;
        }
    }

    private void Update()
    {
        // Leave the input alone while the settings menu is up.
        if (GameSettings.MenuOpen || Mouse.current == null ||
            !Mouse.current.rightButton.wasPressedThisFrame)
            return;

        if (playerCamera == null || switchboard == null)
            return;

        Ray ray = playerCamera.ScreenPointToRay(
            Mouse.current.position.ReadValue()
        );

        // Only an object on the interactable layer counts.
        if (!Physics.Raycast(ray, out _, rayDistance, interactableLayer))
            return;

        switchboard.StartShift();

        if (disableAfterUse)
            enabled = false;
    }
}
