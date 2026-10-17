
using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    [TextArea(2, 5)]
    public string text;

    [Header("Text Settings")]
    public float textSize = 36f;

    [Tooltip("Total time in seconds to reveal the entire line.")]
    [Min(0f)]
    public float typeSpeed = 2f;

    [Header("Typing Audio")]
    public AudioClip typingClip;

    [Range(0f, 1f)]
    public float typingVolume = 1f;

    [Min(1)]
    public int soundEveryCharacters = 1;

    [Header("Letter Shake")]
    public bool shakeLetters;

    [Tooltip(
        "How many times per second each letter re-randomizes its chaotic " +
        "offset. Values up to about 30 all look equally frantic."
    )]
    [Min(0f)]
    public float shakeSpeed = 20f;

    [Tooltip("Maximum chaotic jitter distance, in text units.")]
    [Min(0f)]
    public float shakeAmount = 2f;
}
