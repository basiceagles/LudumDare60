
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
}
