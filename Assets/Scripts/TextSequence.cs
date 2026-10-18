
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class TextSequence : MonoBehaviour
{
    [Header("UI Text")]
    [SerializeField] private TMP_Text firstText;
    [SerializeField] private TMP_Text secondText;

    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private float rayDistance = 5f;

    [Header("Incoming Call")]
    [SerializeField] private SwitchboardGame switchboard;
    [Tooltip(
        "Seconds between the click on the interactable object and the first " +
        "call arriving. Negative keeps the receiver's own delay."
    )]
    [SerializeField] private float callDelay = 1f;

    private bool waitingForSecondClick;

    private void Start()
    {
        firstText.gameObject.SetActive(true);
        secondText.gameObject.SetActive(false);

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (switchboard == null)
            switchboard = FindFirstObjectByType<SwitchboardGame>();
    }

    private void Update()
    {
        // Leave the input alone while the settings menu is up.
        if (GameSettings.MenuOpen || Mouse.current == null)
            return;

        // RMB: Hide first text and show second text
        if (!waitingForSecondClick &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            firstText.gameObject.SetActive(false);
            secondText.gameObject.SetActive(true);
            waitingForSecondClick = true;
        }

        // LMB: Hide second text only when clicking
        // an object on the Interact layer
        else if (waitingForSecondClick &&
                 Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (RaycastInteractable())
            {
                secondText.gameObject.SetActive(false);

                // Arm the shift so the opening call rings a second later.
                if (switchboard != null)
                    switchboard.StartShift(callDelay);

                enabled = false;
            }
        }
    }

    private bool RaycastInteractable()
    {
        if (playerCamera == null || Mouse.current == null)
            return false;

        return Physics.Raycast(
            playerCamera.ScreenPointToRay(
                Mouse.current.position.ReadValue()),
            out _,
            rayDistance,
            interactLayer);
    }
}
