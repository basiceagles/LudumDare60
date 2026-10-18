using DG.Tweening;
using SmallHedge.SoundManager;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[System.Serializable]
public class SwitchboardSocket
{
    public Collider collider;
    public string lineId;
}

[System.Serializable]
public class SwitchboardSounds
{
    public SoundType plugPickup = SoundType.PLUG_PICKUP;
    public SoundType plugDrag = SoundType.PLUG_DRAG;
    public SoundType plugInsert = SoundType.PLUG_INSERT;
    public SoundType plugPutDown = SoundType.PLUG_PUT_DOWN;
    public SoundType plugCancel = SoundType.PLUG_CANCEL;
    public SoundType socketHover = SoundType.SOCKET_HOVER;
    public SoundType correctConnection = SoundType.CONNECTION_CORRECT;
    public SoundType wrongConnection = SoundType.CONNECTION_WRONG;
    public SoundType dialogueNext = SoundType.DIALOGUE_NEXT;
    public SoundType dialogueTyping = SoundType.DIALOGUE_TYPING;
    public SoundType dialogueFinished = SoundType.DIALOGUE_FINISHED;
    public SoundType shiftFinished = SoundType.SHIFT_FINISHED;
}

public class SwitchboardGame : MonoBehaviour
{
    public PhoneReceiver receiver;
    public SwitchboardSounds sounds = new SwitchboardSounds();
    public PhoneCall[] calls = new PhoneCall[0];
    public GameObject dialoguePanel;
    public Image characterIcon;
    public TMP_Text speakerText;
    public TMP_Text dialogueText;
    [Min(1)] public float charactersPerSecond = 38;

    [Header("Call Timing")]
    [Tooltip("Shortest random pause, in seconds, before the next call rings.")]
    [Min(0f)] public float minSecondsBetweenCalls = 5f;
    [Tooltip("Longest random pause, in seconds, before the next call rings.")]
    [Min(0f)] public float maxSecondsBetweenCalls = 10f;

    public Transform plug;
    public Camera viewCamera;
    public Collider grabCollider;
    public Collider answerCallButton;
    public SwitchboardSocket[] sockets = new SwitchboardSocket[0];
    public Color hoverColor = new Color(0.95f, 0.7f, 0.3f);
    public Transform plugTip;

    public Transform cableAnchor;
    public LineRenderer cable;

    private enum Phase
    {
        IncomingCall,
        Conversation,
        Routing,
        Reply,
        Transition,
        Finished
    }

    private Phase phase = Phase.Transition;
    private PhoneLine[] currentLines;
    private int callIndex = -1;
    private int lineIndex;
    private bool dragging;

    private Vector3 homePos;
    private Quaternion homeRot;
    private Vector3 localPlugTip;
    private Plane dragPlane;
    private Renderer[] socketRenderers;
    private Color[] socketColors;
    private MaterialPropertyBlock highlight;
    private int hoveredSocket = -1;

    public bool ReadyForConnection => phase == Phase.Routing;

    private void Start()
    {
        if (receiver == null)
        {
            receiver = FindFirstObjectByType<PhoneReceiver>();
        }
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }
        if (plug == null && grabCollider != null)
        {
            plug = grabCollider.transform;
        }
        if (grabCollider == null && plug != null)
        {
            grabCollider = plug.GetComponent<Collider>();
        }
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (plug != null)
        {
            homePos = plug.position;
            homeRot = plug.rotation;
            localPlugTip = plugTip != null ? plug.InverseTransformPoint(plugTip.position) : Vector3.zero;
            if (plugTip == null && grabCollider is BoxCollider plugBox)
            {
                localPlugTip = plug.InverseTransformPoint(plugBox.transform.TransformPoint(
                    plugBox.center + Vector3.up * plugBox.size.y * 0.5f));
            }
        }

        Vector3 planePoint = homePos;
        for (int i = 0; i < sockets.Length; i++)
        {
            if (sockets[i] != null && sockets[i].collider != null)
            {
                planePoint = sockets[i].collider.bounds.center;
                break;
            }
        }
        if (viewCamera != null)
        {
            dragPlane = new Plane(-viewCamera.transform.forward, planePoint);
        }

        highlight = new MaterialPropertyBlock();
        socketRenderers = new Renderer[sockets.Length];
        socketColors = new Color[sockets.Length];
        for (int i = 0; i < sockets.Length; i++)
        {
            if (sockets[i] == null || sockets[i].collider == null)
            {
                continue;
            }
            socketRenderers[i] = sockets[i].collider.GetComponent<Renderer>();
            if (socketRenderers[i] != null && socketRenderers[i].sharedMaterial != null)
            {
                socketColors[i] = socketRenderers[i].sharedMaterial.GetColor("_BaseColor");
            }
        }

        if (cable != null)
        {
            cable.positionCount = 24;
        }

        RestartShift();
    }

    private void Update()
    {
        if (Mouse.current == null)
        {
            return;
        }

        if (phase == Phase.Routing)
        {
            DragPlug();
            return;
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        switch (phase)
        {
            case Phase.IncomingCall:
                if (answerCallButton != null && viewCamera != null)
                {
                    Ray ray = viewCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                    if (answerCallButton.Raycast(ray, out _, 100f))
                    {
                        answerCallButton.transform.DOKill();
                        answerCallButton.transform.DOPunchScale(Vector3.one * -0.15f, 0.2f);
                        StartConversation();
                    }
                }
                break;

            case Phase.Conversation:
            case Phase.Reply:
                AdvanceDialogue();
                break;

            case Phase.Finished:
                if (calls.Length > 0)
                {
                    RestartShift();
                }
                break;
        }
    }

    public void RestartShift()
    {
        DOTween.Kill(this);
        plug?.DOKill();
        dialogueText?.DOKill();
        dragging = false;
        callIndex = -1;
        NextCall();
    }

    public void HangUp()
    {
        if (callIndex < 0 || callIndex >= calls.Length)
        {
            return;
        }
        dialogueText?.DOKill();
        dragging = false;
        NextCall();
    }

    private void NextCall()
    {
        DOTween.Kill(this);
        phase = Phase.Transition;
        StopLoopSounds();
        receiver?.SetIdle();
        ShowCharacter(null);
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        ReturnHome();

        if (++callIndex >= calls.Length)
        {
            phase = Phase.Finished;
            SoundManager.PlaySound(sounds.shiftFinished);
            return;
        }

        // The very first call keeps the receiver's own start-up delay. Every
        // call after that waits a random pause, so inserting a plug no longer
        // makes the next call ring the moment the current one ends.
        float delay = callIndex == 0
            ? (receiver != null ? Mathf.Max(0f, receiver.firstCallDelay) : 0f)
            : RandomSecondsBetweenCalls();

        DOVirtual.DelayedCall(delay, () =>
        {
            phase = Phase.IncomingCall;
            receiver?.StartRinging();
        }, false).SetLink(gameObject).SetTarget(this).SetId(this);
    }

    // Random pause before the next call rings, kept between the two
    // inspector values (a reversed range falls back to the minimum).
    private float RandomSecondsBetweenCalls()
    {
        float min = Mathf.Max(0f, minSecondsBetweenCalls);

        float max = Mathf.Max(min, maxSecondsBetweenCalls);

        return UnityEngine.Random.Range(min, max);
    }

    private void StartConversation()
    {
        receiver?.SetAccepted();
        if (callIndex < 0)
        {
            callIndex = 0;
        }
        PlayDialogue(calls[callIndex].conversation, Phase.Conversation);
    }

    private void PlayDialogue(PhoneLine[] lines, Phase nextPhase)
    {
        phase = nextPhase;
        currentLines = lines;
        lineIndex = 0;
        if (currentLines == null || currentLines.Length == 0)
        {
            FinishDialogue();
            return;
        }
        ShowLine();
    }

    private void ShowLine()
    {
        SoundManager.PlaySound(sounds.dialogueNext);
        SoundManager.PlaySound(sounds.dialogueTyping);
        PhoneLine line = currentLines[lineIndex];
        ShowCharacter(line.characterSprite != null ? line.characterSprite : calls[callIndex].characterSprite);
        speakerText.text = line.speaker;
        dialogueText.text = line.text;
        dialogueText.maxVisibleCharacters = 0;

        // TextMeshPro only rebuilds its mesh while the object is active.
        // ForceMeshUpdate() below would silently do nothing if the panel was
        // still hidden, leaving textInfo.characterCount at the previous line's
        // length and cutting the new line short, so show the panel first.
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        dialogueText.ForceMeshUpdate();

        int total = dialogueText.textInfo.characterCount;
        float duration = total / charactersPerSecond;

        dialogueText.DOKill();
        DOTween.To(() => dialogueText.maxVisibleCharacters, x => dialogueText.maxVisibleCharacters = x, total, duration)
            .SetEase(Ease.Linear)
            .SetTarget(dialogueText)
            .OnComplete(() =>
            {
                dialogueText.maxVisibleCharacters = total;
                SoundManager.StopSound(sounds.dialogueTyping);
            });
    }

    private void ShowCharacter(Sprite sprite)
    {
        if (characterIcon == null)
        {
            return;
        }

        characterIcon.raycastTarget = false;
        characterIcon.preserveAspect = true;
        characterIcon.sprite = sprite;
        characterIcon.enabled = sprite != null;
    }

    private void AdvanceDialogue()
    {
        if (DOTween.IsTweening(dialogueText))
        {
            dialogueText.DOComplete();
            return;
        }

        if (++lineIndex < currentLines.Length)
        {
            ShowLine();
        }
        else
        {
            FinishDialogue();
        }
    }

    private void FinishDialogue()
    {
        ShowCharacter(null);
        SoundManager.StopSound(sounds.dialogueTyping);
        if (phase == Phase.Conversation)
        {
            SoundManager.PlaySound(sounds.dialogueFinished);
            dialoguePanel.SetActive(false);
            phase = Phase.Routing;
        }
        else
        {
            NextCall();
        }
    }

    private void DragPlug()
    {
        if (viewCamera == null || plug == null || grabCollider == null)
        {
            return;
        }
        Ray ray = viewCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!dragging && Mouse.current.leftButton.wasPressedThisFrame && grabCollider.Raycast(ray, out _, 100))
        {
            dragging = true;
            SoundManager.PlaySound(sounds.plugPickup);
            SoundManager.PlaySound(sounds.plugDrag);
            grabCollider.enabled = false;
            plug.DOKill();
        }

        if (!dragging)
        {
            return;
        }

        if (dragPlane.Raycast(ray, out float dist))
        {
            plug.DOKill();
            plug.DOMove(ray.GetPoint(dist), 0.05f);
            plug.DORotateQuaternion(homeRot, 0.05f);
        }

        int hovered = GetHoveredSocket(ray, out RaycastHit hit);
        HighlightSocket(hovered);

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            dragging = false;
            SoundManager.StopSound(sounds.plugDrag);
            if (hovered >= 0)
            {
                ConnectTo(hovered, hit);
            }
            else
            {
                CancelDrag();
            }
        }
    }

    private int GetHoveredSocket(Ray ray, out RaycastHit nearestHit)
    {
        int hovered = -1;
        nearestHit = default;
        float minDist = 100f;
        for (int i = 0; i < sockets.Length; i++)
        {
            if (sockets[i] != null && sockets[i].collider != null && sockets[i].collider.Raycast(ray, out RaycastHit h, minDist))
            {
                hovered = i;
                nearestHit = h;
                minDist = h.distance;
            }
        }
        return hovered;
    }

    public void ConnectTo(int socketIndex)
    {
        if (socketIndex < 0 || socketIndex >= sockets.Length || sockets[socketIndex] == null || sockets[socketIndex].collider == null)
        {
            return;
        }

        Collider socket = sockets[socketIndex].collider;
        Ray ray = new Ray(viewCamera.transform.position, socket.bounds.center - viewCamera.transform.position);
        if (socket.Raycast(ray, out RaycastHit hit, 100f))
        {
            ConnectTo(socketIndex, hit);
        }
    }

    private void ConnectTo(int socketIndex, RaycastHit hit)
    {
        if (!ReadyForConnection)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sockets[socketIndex].lineId))
        {
            CancelDrag();
            return;
        }

        phase = Phase.Transition;
        HighlightSocket(-1);

        Quaternion targetRot = Quaternion.FromToRotation(homeRot * Vector3.up, -hit.normal) * homeRot;
        Vector3 center = sockets[socketIndex].collider.bounds.center;
        Vector3 surface = center + hit.normal * Vector3.Dot(hit.point - center, hit.normal);
        Vector3 target = surface - targetRot * Vector3.Scale(localPlugTip, plug.lossyScale);

        AnimatePlug(target, targetRot, 0.25f, () =>
        {
            SoundManager.PlaySound(sounds.plugInsert);
            SoundManager.PlaySound(calls[callIndex].IsCorrectLine(sockets[socketIndex].lineId) ? sounds.correctConnection : sounds.wrongConnection);
            PlayDialogue(calls[callIndex].ReplyFor(sockets[socketIndex].lineId), Phase.Reply);
        });
    }

    private void ReturnHome(float duration = 0.35f, TweenCallback onComplete = null)
    {
        if (plug == null || grabCollider == null)
        {
            return;
        }
        bool moved = (plug.position - homePos).sqrMagnitude > 0.0001f || Quaternion.Angle(plug.rotation, homeRot) > 0.1f;
        HighlightSocket(-1);
        grabCollider.enabled = false;

        AnimatePlug(homePos, homeRot, duration, () =>
        {
            grabCollider.enabled = true;
            if (moved)
            {
                SoundManager.PlaySound(sounds.plugPutDown);
            }
            onComplete?.Invoke();
        });
    }

    private void CancelDrag()
    {
        SoundManager.PlaySound(sounds.plugCancel);
        phase = Phase.Transition;
        ReturnHome(0.35f, () =>
        {
            phase = Phase.Routing;
        });
    }

    private void AnimatePlug(Vector3 targetPos, Quaternion targetRot, float duration, TweenCallback onComplete = null)
    {
        plug.DOKill();
        DOTween.Sequence()
            .SetTarget(plug)
            .Join(plug.DOMove(targetPos, duration).SetEase(Ease.OutQuad))
            .Join(plug.DORotateQuaternion(targetRot, duration).SetEase(Ease.OutQuad))
            .OnComplete(onComplete);
    }

    private void HighlightSocket(int index)
    {
        if (hoveredSocket == index)
        {
            return;
        }
        hoveredSocket = index;
        if (dragging && index >= 0)
        {
            SoundManager.PlaySound(sounds.socketHover);
        }

        for (int i = 0; i < socketRenderers.Length; i++)
        {
            if (socketRenderers[i] == null)
            {
                continue;
            }
            bool isHovered = i == index;
            highlight.SetColor("_BaseColor", isHovered ? hoverColor : socketColors[i]);
            highlight.SetColor("_EmissionColor", isHovered ? hoverColor * 0.8f : Color.black);
            socketRenderers[i].SetPropertyBlock(highlight);
        }
    }

    private void LateUpdate()
    {
        if (cable == null || cableAnchor == null || plug == null)
        {
            return;
        }

        Vector3 end = plug.TransformPoint(Vector3.up * 0.3f);
        int count = cable.positionCount;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            float sag = Mathf.Sin(t * Mathf.PI);
            Vector3 pt = Vector3.Lerp(cableAnchor.position, end, t);
            pt.y = Mathf.Max(0.99f, pt.y - sag * 0.3f);
            pt.z -= sag * 0.25f;
            cable.SetPosition(i, pt);
        }
    }

    private void StopLoopSounds()
    {
        SoundManager.StopSound(sounds.plugDrag);
        SoundManager.StopSound(sounds.dialogueTyping);
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
        plug?.DOKill();
        dialogueText?.DOKill();
        StopLoopSounds();
    }
}
