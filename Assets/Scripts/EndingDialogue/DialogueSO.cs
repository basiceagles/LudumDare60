
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

    [Header("Forced Ending")]
    [Tooltip(
        "Play this asset's bad ending no matter what. The switchboard turns " +
        "this on when the player plugs the wrong connector, and it can also " +
        "be ticked by hand to test the bad ending."
    )]
    public bool forceBadEnding;

    [Tooltip(
        "Play this asset's good ending no matter what. The switchboard turns " +
        "this on when the player plugs the correct connector, and it can also " +
        "be ticked by hand to test the good ending."
    )]
    public bool forceGoodEnding;

    public bool HasForcedEnding => forceBadEnding || forceGoodEnding;

    // Called by the switchboard with the result of the plug the player used
    // on the call that feeds this ending.
    public void ForceEnding(bool good)
    {
        forceGoodEnding = good;
        forceBadEnding = !good;
    }
}
