using DG.Tweening;
using SmallHedge.SoundManager;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
    [Tooltip("Image существующего NPC_Icon. Спрайт из звонка или реплики меняет его Source Image.")]
    public Image characterIcon;
    public TMP_Text speakerText;
    public TMP_Text dialogueText;
    [Min(1)] public float charactersPerSecond = 38;

    [Header("Start")]
    [Tooltip(
        "Start the first call from Start(). Leave this off when an RMB " +
        "interaction should arm the shift through StartShift() instead."
    )]
    public bool startOnStart;

    [Header("Call Timing")]
    [Tooltip("Shortest random pause, in seconds, before the next call rings.")]
    [Min(0f)] public float minSecondsBetweenCalls = 5f;
    [Tooltip("Longest random pause, in seconds, before the next call rings.")]
    [Min(0f)] public float maxSecondsBetweenCalls = 10f;
    [Tooltip("Seconds the plug stays in the socket after a call ends before it returns home.")]
    [Min(0f)] public float plugHoldSeconds = 1f;
    [Tooltip("Seconds before the opening call rings when StartShift() is called without a delay. Negative uses the receiver's own First Call Delay.")]
    public float firstCallDelayOverride = -1f;

    public Transform plug;
    public Camera viewCamera;
    public Collider grabCollider;
    public Collider answerCallButton;
    public SwitchboardSocket[] sockets = new SwitchboardSocket[0];
    public Color hoverColor = new Color(0.95f, 0.7f, 0.3f);
    public Transform plugTip;

    public Transform cableAnchor;
    public LineRenderer cable;

    [Header("Call Rules")]
    [Tooltip("When on, the first call ends with its dialogue and skips the plug step, so the between-calls timer starts right away.")]
    public bool firstCallSkipsPlug = true;

    [Header("End Fade")]
    [Tooltip("Full screen UI image used to fade the screen out when the last call ends. It is shown and its alpha is tweened from 0 to 1.")]
    public Image fadeImage;
    [Tooltip("Seconds the fade to black takes.")]
    [Min(0f)] public float fadeDuration = 1.5f;
    [Tooltip("Scene loaded once the fade is done. Leave empty to stay on the ending screen.")]
    public string nextSceneName;
    [Tooltip("Optional SceneLoader called once the fade is done. Takes priority over the scene name field above.")]
    public SceneLoader nextSceneLoader;

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
    private bool shiftStarted;
    private bool shiftEnding;

    private Vector3 homePos;
    private Quaternion homeRot;
    private Vector3 localPlugTip;
    private Plane dragPlane;
    private Vector3 dragCenterOffset;
    private Vector3 dragHalfExtents;
    private BoxCollider dragProbe;
    private const float DragSurfaceGap = 0.005f;
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

        // The shift does not start by itself any more. The first call is
        // armed by StartShift(), which an RMB interaction calls once the
        // player interacts with the switchboard.
        if (startOnStart)
        {
            StartShift();
        }
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
                // While the ending fade is running the shift must not restart
                // under it, so clicks are ignored until the scene hands over.
                if (!shiftEnding && calls.Length > 0)
                {
                    RestartShift();
                }
                break;
        }
    }

    public void StartShift()
    {
        StartShift(-1f);
    }

    // Arms the shift and rings the opening call after the given delay.
    // A negative delay keeps the receiver's own First Call Delay.
    public void StartShift(float firstCallDelay)
    {
        if (shiftStarted)
        {
            return;
        }

        if (firstCallDelay >= 0f)
        {
            firstCallDelayOverride = firstCallDelay;
        }

        RestartShift();
    }

    public void RestartShift()
    {
        shiftStarted = true;
        DOTween.Kill(this);
        plug?.DOKill();
        dialogueText?.DOKill();
        dragging = false;
        callIndex = -1;
        shiftEnding = false;
        HideFade();
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
        // Keep the plug seated in the socket for a beat after the call ends
        // instead of yanking it home the instant the dialogue finishes.
        float plugHold = Mathf.Max(0f, plugHoldSeconds);
        if (plugHold > 0f)
        {
            DOVirtual.DelayedCall(plugHold, () => ReturnHome(), false).SetLink(gameObject).SetTarget(this).SetId(this);
        }
        else
        {
            ReturnHome();
        }

        if (++callIndex >= calls.Length)
        {
            phase = Phase.Finished;
            SoundManager.PlaySound(sounds.shiftFinished);
            BeginEnding();
            return;
        }

        // The very first call uses the delay handed to StartShift() or the
        // receiver's own start-up delay. Every call after that waits a random
        // pause, so inserting a plug no longer makes the next call ring the
        // moment the current one ends.
        float delay = callIndex == 0
            ? FirstCallDelay()
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

    // Delay before the opening call rings: the value handed to StartShift()
    // wins, otherwise the receiver's own start-up delay is used.
    private float FirstCallDelay()
    {
        if (firstCallDelayOverride >= 0f)
        {
            return firstCallDelayOverride;
        }

        return receiver != null ? Mathf.Max(0f, receiver.firstCallDelay) : 0f;
    }

    // The last call is over: fade the screen out and hand over to the
    // configured scene. Nothing is armed when no fader and no scene are set,
    // so the old "click to run the shift again" flow keeps working.
    private void BeginEnding()
    {
        if (fadeImage == null && nextSceneLoader == null && string.IsNullOrWhiteSpace(nextSceneName))
        {
            return;
        }

        shiftEnding = true;

        // Let the plug finish its hold first, so the screen only goes dark
        // once the call is really over (the return animation overlaps the
        // start of the fade).
        float delay = Mathf.Max(0f, plugHoldSeconds);
        if (delay > 0f)
        {
            DOVirtual.DelayedCall(delay, FadeOutToScene, false).SetLink(gameObject).SetTarget(this).SetId(this);
        }
        else
        {
            FadeOutToScene();
        }
    }

    private void FadeOutToScene()
    {
        if (fadeImage == null)
        {
            LoadTargetScene();
            return;
        }

        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;

        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;

        fadeImage.DOKill();
        fadeImage.DOFade(1f, Mathf.Max(0f, fadeDuration))
            .SetTarget(fadeImage)
            .SetId(this)
            .SetLink(gameObject)
            .OnComplete(LoadTargetScene);
    }

    private void LoadTargetScene()
    {
        if (nextSceneLoader != null)
        {
            nextSceneLoader.LoadScene();
            return;
        }

        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void HideFade()
    {
        if (fadeImage == null)
        {
            return;
        }

        fadeImage.DOKill();

        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;

        fadeImage.gameObject.SetActive(false);
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
        if (characterIcon == null && dialoguePanel != null)
        {
            Transform icon = dialoguePanel.transform.Find("NPC_Icon");
            if (icon != null)
            {
                characterIcon = icon.GetComponent<Image>();
            }
        }
        if (characterIcon == null)
        {
            return;
        }

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

            // The opening call can be a plain conversation: when it skips the
            // plug step the between-calls timer starts the moment it is over.
            if (SkipPlugForCurrentCall)
            {
                NextCall();
            }
            else
            {
                phase = Phase.Routing;
            }
        }
        else
        {
            NextCall();
        }
    }

    private bool SkipPlugForCurrentCall => firstCallSkipsPlug && callIndex == 0;

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
            dragPlane = new Plane(-viewCamera.transform.forward, plug.position);
            CacheDragBounds();
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
            float follow = 1f - Mathf.Exp(-35f * Time.deltaTime);
            plug.position = GetSafeDragPosition(ray, dist, follow);
            plug.rotation = homeRot;
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

    private void CacheDragBounds()
    {
        Bounds bounds = grabCollider.bounds;
        foreach (Renderer renderer in plug.GetComponentsInChildren<Renderer>())
        {
            if (renderer.enabled && (renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        dragCenterOffset = bounds.center - plug.position;
        dragHalfExtents = Vector3.Max(bounds.extents, Vector3.one * 0.001f);
        PrepareDragProbe();
    }

    private Vector3 GetSafeDragPosition(Ray ray, float distance, float follow = 1f)
    {
        // ComputePenetration needs an enabled collider in this Unity version.
        // Enable this trigger only during the query, never during simulation.
        dragProbe.enabled = true;
        try
        {
            return SolveDragPosition(ray, distance, follow);
        }
        finally
        {
            dragProbe.enabled = false;
        }
    }

    private Vector3 SolveDragPosition(Ray ray, float distance, float follow)
    {
        float clearDistance = GetClearDragDistance(ray.origin + dragCenterOffset, ray.direction, distance);
        Vector3 target = Vector3.Lerp(plug.position, ray.GetPoint(clearDistance), follow) + dragCenterOffset;
        Vector3 center = ResolveDragOverlaps(plug.position + dragCenterOffset);
        Vector3 remaining = target - center;
        Vector3 firstNormal = Vector3.zero;
        Vector3 secondNormal = Vector3.zero;

        // Sweep each part of the slide. A tween straight to the final position
        // would cut through a corner even when both end points were clear.
        for (int step = 0; step < 6 && remaining.sqrMagnitude > 0.000001f; step++)
        {
            float length = remaining.magnitude;
            Vector3 direction = remaining / length;
            if (!TryGetDragHit(center, direction, length, out RaycastHit hit))
            {
                center += remaining;
                break;
            }

            float travel = Mathf.Clamp(hit.distance - DragSurfaceGap, 0f, length);
            center += direction * travel;
            remaining -= direction * travel;
            remaining = Vector3.ProjectOnPlane(remaining, hit.normal);

            if (firstNormal == Vector3.zero)
            {
                firstNormal = hit.normal;
            }
            else if (Mathf.Abs(Vector3.Dot(firstNormal, hit.normal)) < 0.99f)
            {
                if (secondNormal == Vector3.zero)
                {
                    secondNormal = hit.normal;
                }
                Vector3 edge = Vector3.Cross(firstNormal, secondNormal);
                remaining = Vector3.Project(remaining, edge);
                if (Vector3.Dot(remaining, hit.normal) < -0.00001f)
                {
                    remaining = Vector3.zero;
                }
            }
        }

        return ResolveDragOverlaps(center) - dragCenterOffset;
    }

    private void PrepareDragProbe()
    {
        if (dragProbe == null)
        {
            // Reusable query shape; kept out of the physics simulation.
            dragProbe = gameObject.AddComponent<BoxCollider>();
            dragProbe.enabled = false;
            dragProbe.isTrigger = true;
            dragProbe.hideFlags = HideFlags.HideAndDontSave;
        }
        Vector3 size = (dragHalfExtents + Vector3.one * DragSurfaceGap) * 2f;
        Vector3 scale = transform.lossyScale;
        dragProbe.center = Vector3.zero;
        dragProbe.size = new Vector3(
            size.x / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
            size.y / Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
            size.z / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));
    }

    private Vector3 ResolveDragOverlaps(Vector3 center)
    {
        for (int step = 0; step < 8; step++)
        {
            bool corrected = false;
            foreach (Collider obstacle in Physics.OverlapBox(center, dragHalfExtents + Vector3.one * DragSurfaceGap,
                         Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (obstacle == dragProbe || obstacle.transform.IsChildOf(plug))
                {
                    continue;
                }
                if (Physics.ComputePenetration(dragProbe, center, Quaternion.identity,
                        obstacle, obstacle.transform.position, obstacle.transform.rotation,
                        out Vector3 normal, out float depth) && depth > 0.00001f)
                {
                    center += normal * (depth + 0.0001f);
                    corrected = true;
                }
            }
            if (!corrected)
            {
                break;
            }
        }
        return center;
    }

    private float GetClearDragDistance(Vector3 center, Vector3 direction, float distance)
    {
        return TryGetDragHit(center, direction, distance, out RaycastHit hit)
            ? Mathf.Clamp(hit.distance - DragSurfaceGap, 0f, distance)
            : distance;
    }

    private bool TryGetDragHit(Vector3 center, Vector3 direction, float distance, out RaycastHit nearest)
    {
        nearest = default;
        bool found = false;
        float closest = distance + DragSurfaceGap;
        foreach (RaycastHit hit in Physics.BoxCastAll(center, dragHalfExtents, direction,
                     Quaternion.identity, closest, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            Collider obstacle = hit.collider;
            if (obstacle == dragProbe || obstacle.transform.IsChildOf(plug))
            {
                continue;
            }
            RaycastHit contact = hit;
            // An overlapping BoxCast reports -direction as its normal. Use
            // the actual separating normal so moving away cannot be blocked.
            if (hit.distance <= 0.00001f && Physics.ComputePenetration(dragProbe, center, Quaternion.identity,
                    obstacle, obstacle.transform.position, obstacle.transform.rotation,
                    out Vector3 normal, out _))
            {
                contact.normal = normal;
            }
            if (Vector3.Dot(direction, contact.normal) >= -0.0001f || contact.distance > closest)
            {
                continue;
            }
            nearest = contact;
            closest = contact.distance;
            found = true;
        }
        return found;
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
            // Grab the call and the chosen connector now, so a hang up during
            // the insert animation cannot shift them under us.
            PhoneCall call = calls[callIndex];
            string lineId = sockets[socketIndex].lineId;
            bool correct = call.IsCorrectLine(lineId);

            SoundManager.PlaySound(sounds.plugInsert);
            SoundManager.PlaySound(correct ? sounds.correctConnection : sounds.wrongConnection);

            // The connector the player chose decides this call's ending:
            // correct line = good ending, anything else = bad ending.
            call.ApplyEndingResult(correct);

            PlayDialogue(call.ReplyFor(lineId), Phase.Reply);
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
        if (dragProbe != null)
        {
            Destroy(dragProbe);
        }
    }
}
