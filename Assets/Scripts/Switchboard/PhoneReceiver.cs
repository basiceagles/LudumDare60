using SmallHedge.SoundManager;
using UnityEngine;

public class PhoneReceiver : MonoBehaviour
{
    [Min(0)] public float firstCallDelay = 3f;
    [Min(0.05f)] public float blinkInterval = 0.35f;
    public Renderer incomingLamp;
    public Renderer connectedLamp;
    public Material redMaterial;
    public Material blinkMaterial1;
    public Material blinkMaterial2;
    public Material greenMaterial;
    public SpriteRenderer callSprite;
    public SoundType ringSound = SoundType.CALL_RING;
    public SoundType answerSound = SoundType.CALL_ANSWER;
    public SoundType hangUpSound = SoundType.CALL_HANG_UP;

    private bool ringing;
    private bool accepted;
    private bool firstBlink;
    private float blinkTimer;

    private void Awake()
    {
        SetIdle(false);
    }

    private void Update()
    {
        if (!ringing)
        {
            return;
        }

        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0)
        {
            blinkTimer = Mathf.Max(0.05f, blinkInterval);
            firstBlink = !firstBlink;
            SetLamp(incomingLamp, firstBlink ? blinkMaterial1 : blinkMaterial2);
        }
    }

    public void SetIdle(bool playSound = true)
    {
        bool hadCall = ringing || accepted;
        ringing = false;
        accepted = false;
        SoundManager.StopSound(ringSound);
        SetLamp(incomingLamp, redMaterial);
        SetLamp(connectedLamp, redMaterial);
        ShowSprite(false);
        if (playSound && hadCall)
        {
            SoundManager.PlaySound(hangUpSound);
        }
    }

    public void StartRinging()
    {
        ringing = true;
        accepted = false;
        firstBlink = true;
        blinkTimer = Mathf.Max(0.05f, blinkInterval);
        SetLamp(incomingLamp, blinkMaterial1);
        SetLamp(connectedLamp, redMaterial);
        ShowSprite(true);
        SoundManager.PlaySound(ringSound);
    }

    public void SetAccepted()
    {
        ringing = false;
        accepted = true;
        SoundManager.StopSound(ringSound);
        SetLamp(incomingLamp, redMaterial);
        SetLamp(connectedLamp, greenMaterial);
        ShowSprite(false);
        SoundManager.PlaySound(answerSound);
    }

    private void ShowSprite(bool visible)
    {
        if (callSprite != null)
        {
            callSprite.enabled = visible;
            callSprite.gameObject.SetActive(visible);
        }
    }

    private static void SetLamp(Renderer lamp, Material material)
    {
        if (lamp != null && material != null)
        {
            lamp.sharedMaterial = material;
        }
    }

    private void OnDisable()
    {
        SetIdle(false);
    }
}
