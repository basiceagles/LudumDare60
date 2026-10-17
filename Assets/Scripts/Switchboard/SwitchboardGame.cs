using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LudumDare60.Switchboard
{
    public class SwitchboardGame : MonoBehaviour
    {
        public PhoneCall[] calls = new PhoneCall[0];
        public GameObject dialoguePanel;
        public TMP_Text speakerText;
        public TMP_Text dialogueText;
        [Min(1)] public float charactersPerSecond = 38;

        public Transform plug;
        public Camera viewCamera;
        public Collider grabCollider;
        public Collider answerCallButton;
        public Collider[] sockets = new Collider[5];
        public Color hoverColor = new Color(0.95f, 0.7f, 0.3f);
        public float tipOffset = 0.48f;

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

        private Phase phase;
        private PhoneLine[] currentLines;
        private int callIndex = -1;
        private int lineIndex;
        private bool dragging;
        private bool isTyping;

        private Vector3 homePos;
        private Quaternion homeRot;
        private Plane dragPlane;
        private Renderer[] socketRenderers;
        private Color[] socketColors;
        private MaterialPropertyBlock highlight;
        private int hoveredSocket = -1;

        private Tween typingTween;
        private Tween plugTween;

        public bool ReadyForConnection => phase == Phase.Routing;

        private void Start()
        {
            SetVisible(false);

            if (plug != null)
            {
                homePos = plug.position;
                homeRot = plug.rotation;
            }

            if (sockets.Length > 0 && sockets[0] != null)
            {
                dragPlane = new Plane(Vector3.up, sockets[0].bounds.center);
                highlight = new MaterialPropertyBlock();

                socketRenderers = new Renderer[sockets.Length];
                socketColors = new Color[sockets.Length];
                for (int i = 0; i < sockets.Length; i++)
                {
                    socketRenderers[i] = sockets[i].GetComponent<Renderer>();
                    if (socketRenderers[i] != null)
                    {
                        socketColors[i] = socketRenderers[i].sharedMaterial.GetColor("_BaseColor");
                    }
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

            if (ReadyForConnection)
            {
                DragPlug();
                return;
            }

            if (!Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            Ray ray = viewCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (phase == Phase.IncomingCall)
            {
                if (answerCallButton != null)
                {
                    bool isHit = answerCallButton.Raycast(ray, out _, 100f) ||
                                 (Physics.Raycast(ray, out RaycastHit hit, 100f) && (hit.collider == answerCallButton || hit.transform == answerCallButton.transform));

                    if (isHit)
                    {
                        answerCallButton.transform.DOPunchScale(Vector3.one * -0.15f, 0.2f);
                        StartConversation();
                    }
                }
                return;
            }

            if (phase == Phase.Finished && calls.Length > 0)
            {
                RestartShift();
                return;
            }

            if (phase == Phase.Conversation || phase == Phase.Reply)
            {
                AdvanceDialogue();
            }
        }

        public void RestartShift()
        {
            if (plug != null)
            {
                DOTween.Kill(plug);
            }
            typingTween?.Kill();
            dragging = false;
            isTyping = false;
            callIndex = -1;
            NextCall();
        }

        private void NextCall()
        {
            phase = Phase.Transition;
            SetVisible(false);
            ReturnHome();

            if (++callIndex >= calls.Length)
            {
                phase = Phase.Finished;
                return;
            }

            phase = Phase.IncomingCall;
        }

        private void StartConversation()
        {
            PlayDialogue(calls[callIndex].conversation, Phase.Conversation);
        }

        private void PlayDialogue(PhoneLine[] lines, Phase nextPhase)
        {
            phase = nextPhase;
            currentLines = lines;
            lineIndex = 0;
            ShowLine();
        }

        private void ShowLine()
        {
            PhoneLine line = currentLines[lineIndex];
            speakerText.text = line.speaker;
            dialogueText.text = line.text;
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.ForceMeshUpdate();

            SetVisible(true);

            int total = dialogueText.textInfo.characterCount;
            float duration = total / charactersPerSecond;
            isTyping = true;

            typingTween?.Kill();
            typingTween = DOTween.To(() => dialogueText.maxVisibleCharacters, x => dialogueText.maxVisibleCharacters = x, total, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    isTyping = false;
                });
        }

        private void SetVisible(bool visible)
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(visible);
            }
        }

        private void FinishTyping()
        {
            typingTween?.Kill();
            dialogueText.maxVisibleCharacters = int.MaxValue;
            isTyping = false;
        }

        private void AdvanceDialogue()
        {
            if (isTyping)
            {
                FinishTyping();
                return;
            }

            if (++lineIndex < currentLines.Length)
            {
                ShowLine();
            }
            else if (phase == Phase.Conversation)
            {
                SetVisible(false);
                phase = Phase.Routing;
            }
            else
            {
                NextCall();
            }
        }

        private void DragPlug()
        {
            if (plug == null)
            {
                return;
            }

            Ray ray = viewCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (!dragging && Mouse.current.leftButton.wasPressedThisFrame && grabCollider.Raycast(ray, out _, 100))
            {
                dragging = true;
                grabCollider.enabled = false;
                plugTween?.Kill();
            }

            if (!dragging)
            {
                return;
            }

            if (dragPlane.Raycast(ray, out float dist))
            {
                Vector3 target = ray.GetPoint(dist) + Vector3.up * (tipOffset + 0.2f);
                plug.position = Vector3.Lerp(plug.position, target, 1 - Mathf.Exp(-22 * Time.deltaTime));
                plug.rotation = Quaternion.Slerp(plug.rotation, Quaternion.identity, 1 - Mathf.Exp(-22 * Time.deltaTime));
            }

            int hovered = -1;
            for (int i = 0; i < sockets.Length; i++)
            {
                if (sockets[i].Raycast(ray, out _, 100))
                {
                    hovered = i;
                    break;
                }
            }
            HighlightSocket(hovered);

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                dragging = false;
                if (hovered >= 0)
                {
                    ConnectTo(hovered);
                }
                else
                {
                    CancelDrag();
                }
            }
        }

        public void ConnectTo(int socketIndex)
        {
            if (socketIndex < 0 || socketIndex >= sockets.Length)
            {
                return;
            }

            phase = Phase.Transition;
            HighlightSocket(-1);

            Vector3 target = sockets[socketIndex].bounds.center + Vector3.up * tipOffset;

            if (plug != null)
            {
                plugTween?.Kill();
                plugTween = DOTween.Sequence()
                    .Join(plug.DOMove(target, 0.25f).SetEase(Ease.OutBack))
                    .Join(plug.DORotateQuaternion(Quaternion.identity, 0.25f).SetEase(Ease.OutQuad))
                    .OnComplete(() =>
                    {
                        PhoneLine[] reply = calls[callIndex].ReplyFor((ServiceDestination)socketIndex);
                        PlayDialogue(reply, Phase.Reply);
                    });
            }
            else
            {
                PhoneLine[] reply = calls[callIndex].ReplyFor((ServiceDestination)socketIndex);
                PlayDialogue(reply, Phase.Reply);
            }
        }

        private void ReturnHome(float duration = 0.35f)
        {
            HighlightSocket(-1);
            if (grabCollider != null)
            {
                grabCollider.enabled = false;
            }

            if (plug != null)
            {
                plugTween?.Kill();
                plugTween = DOTween.Sequence()
                    .Join(plug.DOMove(homePos, duration).SetEase(Ease.OutQuad))
                    .Join(plug.DORotateQuaternion(homeRot, duration).SetEase(Ease.OutQuad))
                    .OnComplete(() =>
                    {
                        if (grabCollider != null)
                        {
                            grabCollider.enabled = true;
                        }
                    });
            }
        }

        private void CancelDrag()
        {
            phase = Phase.Transition;
            ReturnHome();
            DOVirtual.DelayedCall(0.35f, () =>
            {
                phase = Phase.Routing;
            });
        }

        private void HighlightSocket(int index)
        {
            if (hoveredSocket == index)
            {
                return;
            }
            hoveredSocket = index;

            for (int i = 0; i < socketRenderers.Length; i++)
            {
                if (socketRenderers[i] == null)
                {
                    continue;
                }
                highlight.SetColor("_BaseColor", i == index ? hoverColor : socketColors[i]);
                highlight.SetColor("_EmissionColor", i == index ? hoverColor * 0.8f : Color.black);
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
            for (int i = 0; i < cable.positionCount; i++)
            {
                float t = (float)i / (cable.positionCount - 1);
                Vector3 pt = Vector3.Lerp(cableAnchor.position, end, t);
                pt.y = Mathf.Max(0.99f, pt.y - Mathf.Sin(t * Mathf.PI) * 0.3f);
                pt.z -= Mathf.Sin(t * Mathf.PI) * 0.25f;
                cable.SetPosition(i, pt);
            }
        }
    }
}
