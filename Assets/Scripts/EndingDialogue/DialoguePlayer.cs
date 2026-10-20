
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DialoguePlayer : MonoBehaviour
{
    [Header("Dialogue Sequence")]
    public List<DialogueSO> dialogues = new List<DialogueSO>();

    [Header("UI")]
    public TMP_Text dialogueText;
    public GameObject dialoguePanel;

    [Header("Typing Audio")]
    public AudioSource typingAudioSource;

    [Tooltip(
        "Turn this off to silence the typing sound for every dialogue and " +
        "every line. The typing animation itself keeps playing."
    )]
    public bool playTypingSounds = true;

    [Tooltip(
        "Play at most one typing sound at a time. Prevents fast typing " +
        "from stacking sounds into an echo."
    )]
    public bool avoidOverlappingTypingSounds = true;

    [Tooltip(
        "Minimum delay in seconds between typing sounds. 0 lets the clip " +
        "length set the pace."
    )]
    [Min(0f)]
    public float typingSoundMinInterval = 0f;

    [Header("Events")]

    [Tooltip(
        "Fired once after EVERY dialogue has finished - whether the last " +
        "line played through by itself or the whole sequence was skipped " +
        "with StopDialogue(). Every entry is invoked in order, so use it " +
        "for the ending scene load, credits, a sound, etc."
    )]
    public List<UnityEvent> onAllDialoguesFinished = new List<UnityEvent>();

    [Header("Debug")]
    public bool forceGoodEnding;
    public bool forceBadEnding;

    private bool isPlaying;
    private bool finishedEventsFired;
    private bool isTyping;
    private bool skipTypingRequested;
    private bool advanceRequested;
    private float nextTypingSoundTime;
    private Vector3[][] shakeOriginalVertices;
    private Vector3[] shakeOffsets;
    private float nextShakeRollTime;

    private Coroutine dialogueCoroutine;

    private void Update()
    {
        if (!isPlaying || GameSettings.MenuOpen || Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (isTyping)
            {
                // First click: reveal the entire current line.
                skipTypingRequested = true;

                // Stop any typing sounds playing on this source.
                if (typingAudioSource != null)
                    typingAudioSource.Stop();
            }
            else
            {
                // Click while the line is complete: advance.
                advanceRequested = true;
            }
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (isTyping)
            {
                skipTypingRequested = true;

                if (typingAudioSource != null)
                    typingAudioSource.Stop();
            }
            else
            {
                advanceRequested = true;
            }
        }
    }

    // Uses what the player did during the shift first (kept in memory only),
    // then the manual force flags on the ending assets, then the Inspector
    // debug toggles. If both sides say something, Bad Ending wins.
    public void PlayDialogue()
    {
        PlayDialogue(ResolveGoodEnding());
    }

    private bool ResolveGoodEnding()
    {
        foreach (DialogueSO dialogueData in dialogues)
        {
            if (dialogueData == null)
                continue;

            if (EndingState.TryGet(dialogueData, out bool playerGood))
                return playerGood;
        }

        foreach (DialogueSO dialogueData in dialogues)
        {
            if (dialogueData == null)
                continue;

            if (dialogueData.forceBadEnding)
                return false;

            if (dialogueData.forceGoodEnding)
                return true;
        }

        if (forceBadEnding)
            return false;

        if (forceGoodEnding)
            return true;

        // Nothing was forced: default to the bad ending.
        return false;
    }

    // Ending version to play for one asset: the player's decision for that
    // asset wins, then its manual Inspector flag, then the fallback taken
    // when playback started.
    private static bool ResolveEndingFor(
        DialogueSO dialogueData,
        bool fallback
    )
    {
        if (EndingState.TryGet(dialogueData, out bool playerGood))
            return playerGood;

        if (dialogueData.forceBadEnding)
            return false;

        if (dialogueData.forceGoodEnding)
            return true;

        return fallback;
    }

    // Call from another script to choose the ending.
    public void PlayDialogue(bool goodEnding)
    {
        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
            dialogueCoroutine = null;
        }

        if (typingAudioSource != null)
            typingAudioSource.Stop();

        isPlaying = true;
        isTyping = false;
        skipTypingRequested = false;
        advanceRequested = false;
        finishedEventsFired = false;

        dialogueCoroutine = StartCoroutine(
            PlayAllDialogues(goodEnding)
        );
    }

    private IEnumerator PlayAllDialogues(bool goodEnding)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (dialogueText != null)
        {
            // With this on, TMP recalculates the vertical alignment from the
            // descender of the characters that are currently revealed. Since
            // the text is middle-aligned, revealing the rest of a line (skip
            // or natural finish) changes that descender and nudges the whole
            // block up. Basing the alignment on the full line keeps it still.
            dialogueText.useMaxVisibleDescender = false;

            dialogueText.text = "";
            dialogueText.maxVisibleCharacters = 0;
        }

        foreach (DialogueSO dialogueData in dialogues)
        {
            if (dialogueData == null)
                continue;

            // Play this asset's main dialogue.
            if (dialogueData.mainDialogue != null)
            {
                foreach (DialogueLine line in dialogueData.mainDialogue)
                {
                    if (line == null)
                        continue;

                    yield return StartCoroutine(PlayLine(line));
                }
            }

            // Play the selected ending for this asset. What the player did
            // during the shift wins, then the asset's manual force flag,
            // otherwise the decision taken when playback started is used.
            bool useGoodEnding = ResolveEndingFor(dialogueData, goodEnding);

            List<DialogueLine> ending = useGoodEnding
                ? dialogueData.goodEnding
                : dialogueData.badEnding;

            if (ending == null)
                continue;

            foreach (DialogueLine line in ending)
            {
                if (line == null)
                    continue;

                yield return StartCoroutine(PlayLine(line));
            }
        }

        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.maxVisibleCharacters = 0;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (typingAudioSource != null)
            typingAudioSource.Stop();

        isPlaying = false;
        isTyping = false;
        dialogueCoroutine = null;

        // Every dialogue has been played through: let the reactions set up
        // in the Inspector run. The coroutine is already cleared, so a
        // listener may safely start a new one (PlayDialogue).
        FireFinishedEvents();
    }

    // Invokes onAllDialoguesFinished exactly once per playback run, so the
    // natural end and StopDialogue() can never both trigger it.
    private void FireFinishedEvents()
    {
        if (finishedEventsFired)
            return;

        finishedEventsFired = true;

        if (onAllDialoguesFinished == null)
            return;

        foreach (UnityEvent finishedEvent in onAllDialoguesFinished)
        {
            if (finishedEvent != null)
                finishedEvent.Invoke();
        }
    }

    private IEnumerator PlayLine(DialogueLine line)
    {
        if (dialogueText == null)
            yield break;

        // Stop any typing sound left over from the previous line so it
        // cannot bleed into (echo over) this one, and never loop.
        if (typingAudioSource != null)
        {
            typingAudioSource.Stop();
            typingAudioSource.loop = false;
        }

        nextTypingSoundTime = 0f;
        shakeOriginalVertices = null;
        shakeOffsets = null;
        nextShakeRollTime = 0f;

        advanceRequested = false;
        skipTypingRequested = false;

        dialogueText.text = line.text ?? "";
        dialogueText.fontSize = line.textSize;

        // Generate the full line once so the character count and the
        // shake baseline cover every character. The line is hidden again
        // straight away so it still types out from the start, and so the
        // baseline never flashes on screen.
        dialogueText.maxVisibleCharacters = int.MaxValue;
        dialogueText.ForceMeshUpdate();

        int characterCount = dialogueText.textInfo.characterCount;

        if (line.shakeLetters)
            CaptureOriginalVertices();

        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        if (characterCount > 0)
        {
            yield return StartCoroutine(TypeLine(line, characterCount));
        }

        // Make sure the full line is visible after typing.
        dialogueText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;

        // Wait for another click before advancing.
        // Letters shake while waiting if enabled.
        yield return StartCoroutine(WaitForAdvance(line));
    }

    private IEnumerator TypeLine(
        DialogueLine line,
        int characterCount)
    {
        isTyping = true;

        float duration = Mathf.Max(0f, line.typeSpeed);
        float elapsed = 0f;

        int previousVisible = 0;
        int soundInterval = Mathf.Max(1, line.soundEveryCharacters);

        // Zero seconds means show the whole line immediately.
        if (duration <= 0f)
        {
            dialogueText.maxVisibleCharacters = characterCount;
            isTyping = false;
            yield break;
        }

        while (elapsed < duration && !skipTypingRequested)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            int visibleCount = Mathf.Min(
                characterCount,
                Mathf.FloorToInt(progress * characterCount)
            );

            if (visibleCount > previousVisible)
            {
                PlayTypingSounds(
                    line,
                    previousVisible,
                    visibleCount,
                    soundInterval
                );

                previousVisible = visibleCount;
                dialogueText.maxVisibleCharacters = visibleCount;
            }

            // Shake while the line is still typing. The mesh is rebuilt
            // first so the offsets are applied on top of a fresh mesh,
            // otherwise TMP would overwrite them later in the frame.
            if (line.shakeLetters && shakeOriginalVertices != null)
            {
                dialogueText.ForceMeshUpdate();
                ApplyLetterShake(line, shakeOriginalVertices);
            }

            yield return null;
        }

        // Reveal any remaining characters at the end (no extra sound, so
        // there is nothing left playing when the animation stops).
        dialogueText.maxVisibleCharacters = characterCount;

        // Stop the typing sound the moment the type animation ends, whether
        // it finished normally or was skipped, so it can never keep playing
        // or looping while waiting for the next click.
        if (typingAudioSource != null)
        {
            typingAudioSource.loop = false;
            typingAudioSource.Stop();
        }

        isTyping = false;
        skipTypingRequested = false;
    }

    private void PlayTypingSounds(
        DialogueLine line,
        int startIndex,
        int endIndex,
        int soundInterval)
    {
        if (!playTypingSounds)
        {
            // Also silence a clip that was already playing when the toggle
            // was turned off mid-line.
            if (typingAudioSource != null && typingAudioSource.isPlaying)
                typingAudioSource.Stop();

            return;
        }

        if (line.typingClip == null || typingAudioSource == null)
            return;

        dialogueText.ForceMeshUpdate();
        TMP_TextInfo textInfo = dialogueText.textInfo;

        // Only ever start ONE sound for this batch of newly revealed
        // characters. Previously a sound was played for every character,
        // so a burst of characters stacked many overlapping copies of the
        // clip and sounded like a loop with echoes.
        bool shouldPlay = false;

        for (int i = startIndex; i < endIndex; i++)
        {
            if (i % soundInterval != 0)
                continue;

            if (i >= textInfo.characterCount)
                continue;

            TMP_CharacterInfo characterInfo =
                textInfo.characterInfo[i];

            if (Char.IsWhiteSpace(characterInfo.character))
                continue;

            shouldPlay = true;
            break;
        }

        if (!shouldPlay)
            return;

        // Never overlap typing sounds: wait for the previous one to finish
        // before starting another.
        if (
            avoidOverlappingTypingSounds
            && typingAudioSource.isPlaying
        )
        {
            return;
        }

        // Optional minimum spacing between sounds.
        if (
            typingSoundMinInterval > 0f
            && Time.time < nextTypingSoundTime
        )
        {
            return;
        }

        nextTypingSoundTime = Time.time + typingSoundMinInterval;

        // Use Play (not PlayOneShot) so there is exactly ONE voice and
        // Stop() reliably silences it. Force loop off so a shot can never
        // turn into a continuous, looping drone.
        typingAudioSource.loop = false;
        typingAudioSource.clip = line.typingClip;
        typingAudioSource.volume = line.typingVolume;
        typingAudioSource.Play();
    }

    private IEnumerator WaitForAdvance(DialogueLine line)
    {
        // Bake the fully revealed mesh once so the shake writes below are
        // not overwritten by a TMP rebuild later in the frame.
        if (
            line.shakeLetters
            && dialogueText != null
            && shakeOriginalVertices != null
        )
        {
            dialogueText.ForceMeshUpdate();
        }

        while (isPlaying && !advanceRequested)
        {
            if (line.shakeLetters && shakeOriginalVertices != null)
            {
                ApplyLetterShake(line, shakeOriginalVertices);
            }

            yield return null;
        }

        // Restore the original character positions.
        RestoreVertices(shakeOriginalVertices);

        advanceRequested = false;
    }

    // Snapshot the fully revealed line's vertices so every shake frame
    // starts from a clean baseline instead of accumulating offsets.
    // The caller is responsible for having generated the full mesh
    // (maxVisibleCharacters set high) before calling this.
    private void CaptureOriginalVertices()
    {
        shakeOriginalVertices = null;

        if (dialogueText == null)
            return;

        TMP_TextInfo textInfo = dialogueText.textInfo;

        shakeOriginalVertices = new Vector3[textInfo.meshInfo.Length][];

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            Vector3[] vertices = textInfo.meshInfo[i].vertices;

            shakeOriginalVertices[i] = new Vector3[vertices.Length];
            System.Array.Copy(
                vertices,
                shakeOriginalVertices[i],
                vertices.Length
            );
        }
    }

    // Chaotic letter shake: every visible letter jumps to its own random
    // offset instead of riding a smooth wave, so the text jitters.
    private void ApplyLetterShake(
        DialogueLine line,
        Vector3[][] originalVertices)
    {
        TMP_TextInfo textInfo = dialogueText.textInfo;
        int characterCount = textInfo.characterCount;

        RollShakeOffsets(line, characterCount);

        for (int i = 0; i < characterCount; i++)
        {
            TMP_CharacterInfo characterInfo =
                textInfo.characterInfo[i];

            // Only move characters that are already revealed. Hidden
            // characters have zeroed vertices, so restoring their real
            // position would make them pop in before the typewriter
            // reaches them.
            if (!characterInfo.isVisible)
                continue;

            int materialIndex =
                characterInfo.materialReferenceIndex;

            int vertexIndex = characterInfo.vertexIndex;

            Vector3[] vertices =
                textInfo.meshInfo[materialIndex].vertices;

            Vector3 offset = shakeOffsets[i];

            for (int j = 0; j < 4; j++)
            {
                Vector3 baseVertex;

                if (
                    TryGetOriginalVertex(
                        originalVertices,
                        materialIndex,
                        vertexIndex + j,
                        out baseVertex
                    )
                )
                {
                    vertices[vertexIndex + j] = baseVertex + offset;
                }
            }
        }

        dialogueText.UpdateVertexData(
            TMP_VertexDataUpdateFlags.Vertices
        );
    }

    // Re-rolls the random jitter. shakeSpeed drives how often the offsets
    // change, but never slower than 30 times per second so the shake
    // always reads as chaos instead of a frozen random offset.
    private void RollShakeOffsets(DialogueLine line, int characterCount)
    {
        bool resized = shakeOffsets == null
            || shakeOffsets.Length < characterCount;

        if (resized)
            shakeOffsets = new Vector3[characterCount];

        if (!resized && Time.time < nextShakeRollTime)
            return;

        float rollInterval = Mathf.Min(
            1f / Mathf.Max(0.001f, line.shakeSpeed),
            1f / 30f
        );

        nextShakeRollTime = Time.time + rollInterval;

        for (int i = 0; i < characterCount; i++)
        {
            Vector2 jitter =
                UnityEngine.Random.insideUnitCircle * line.shakeAmount;

            shakeOffsets[i] = new Vector3(jitter.x, jitter.y, 0f);
        }
    }

    private static bool TryGetOriginalVertex(
        Vector3[][] originalVertices,
        int materialIndex,
        int vertexIndex,
        out Vector3 vertex)
    {
        vertex = Vector3.zero;

        if (originalVertices == null)
            return false;

        if (materialIndex < 0 || materialIndex >= originalVertices.Length)
            return false;

        Vector3[] original = originalVertices[materialIndex];

        if (
            original == null
            || vertexIndex < 0
            || vertexIndex >= original.Length
        )
        {
            return false;
        }

        vertex = original[vertexIndex];
        return true;
    }

    private void RestoreVertices(Vector3[][] originalVertices)
    {
        if (dialogueText == null || originalVertices == null)
            return;

        TMP_TextInfo textInfo = dialogueText.textInfo;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            if (i >= textInfo.meshInfo.Length)
                break;

            Vector3[] vertices = textInfo.meshInfo[i].vertices;

            System.Array.Copy(
                originalVertices[i],
                vertices,
                Mathf.Min(
                    originalVertices[i].Length,
                    vertices.Length
                )
            );
        }

        dialogueText.UpdateVertexData(
            TMP_VertexDataUpdateFlags.Vertices
        );
    }

    public void StopDialogue()
    {
        // Only counts as "skipped to the end" if a run was active, so
        // stopping something that never started fires nothing.
        bool wasPlaying = isPlaying;

        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
            dialogueCoroutine = null;
        }

        if (typingAudioSource != null)
            typingAudioSource.Stop();

        isPlaying = false;
        isTyping = false;
        skipTypingRequested = false;
        advanceRequested = false;

        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.maxVisibleCharacters = 0;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (wasPlaying)
            FireFinishedEvents();
    }
}
