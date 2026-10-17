
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

    private bool waitingForSecondClick;

    private void Start()
    {
        firstText.gameObject.SetActive(true);
        secondText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Mouse.current == null)
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
            if (Physics.Raycast(
                playerCamera.ScreenPointToRay(
                    Mouse.current.position.ReadValue()),
                out RaycastHit hit,
                rayDistance,
                interactLayer))
            {
                secondText.gameObject.SetActive(false);
                enabled = false;
            }
        }
    }
}
