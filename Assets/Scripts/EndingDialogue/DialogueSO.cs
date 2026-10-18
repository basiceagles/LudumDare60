
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Dialogue",
    menuName = "Dialogue/Dialogue Data"
)]
public class DialogueSO : ScriptableObject
{
    [Header("Main Dialogue")]
    public List<DialogueLine> mainDialogue =
        new List<DialogueLine>();

    [Header("Bad Ending")]
    public List<DialogueLine> badEnding =
        new List<DialogueLine>();

    [Header("Good Ending")]
    public List<DialogueLine> goodEnding =
        new List<DialogueLine>();

    [Header("Forced Ending (manual override)")]
    [Tooltip(
        "Tick by hand to always play this asset's bad ending. Useful for " +
        "testing the End scene directly: what the player does during a shift " +
        "is kept in memory only and takes priority over this."
    )]
    public bool forceBadEnding;

    [Tooltip(
        "Tick by hand to always play this asset's good ending. Useful for " +
        "testing the End scene directly: what the player does during a shift " +
        "is kept in memory only and takes priority over this."
    )]
    public bool forceGoodEnding;
}
