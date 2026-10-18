using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class PhoneLine
{
    public string speaker;
    public Sprite characterSprite;
    [TextArea(2, 5)] public string text;
}

[Serializable]
public class LineReply
{
    public string lineId;
    public PhoneLine[] lines = new PhoneLine[0];
}

[CreateAssetMenu(menuName = "Novel/Phone Call", fileName = "PhoneCall")]
public class PhoneCall : ScriptableObject
{
    public string title;
    public Sprite characterSprite;
    public string correctLineId;
    public PhoneLine[] conversation = new PhoneLine[0];

    [Header("Ending")]
    public DialogueSO ending;
    public string policeLineId = "police";
    public PhoneLine[] policeReply = new PhoneLine[0];

    [FormerlySerializedAs("psychiatristLineId")]
    public string ambulanceLineId = "ambulance";
    [FormerlySerializedAs("psychiatristReply")]
    public PhoneLine[] ambulanceReply = new PhoneLine[0];

    [FormerlySerializedAs("ambulanceLineId")]
    public string psychoLineId = "psycho";
    [FormerlySerializedAs("ambulanceReply")]
    public PhoneLine[] psychoReply = new PhoneLine[0];

    public string fireLineId = "fire";
    public PhoneLine[] fireReply = new PhoneLine[0];

    [FormerlySerializedAs("informationLineId")]
    public string pizzaLineId = "pizza";
    [FormerlySerializedAs("informationReply")]
    public PhoneLine[] pizzaReply = new PhoneLine[0];

    [FormerlySerializedAs("line6Id")]
    public string serviceLineId = "service";
    [FormerlySerializedAs("line6Reply")]
    public PhoneLine[] serviceReply = new PhoneLine[0];

    [FormerlySerializedAs("line7Id")]
    public string graveyardLineId = "graveyard";
    [FormerlySerializedAs("line7Reply")]
    public PhoneLine[] graveyardReply = new PhoneLine[0];

    [FormerlySerializedAs("line8Id")]
    public string disconnectLineId = "disconnect";
    [FormerlySerializedAs("line8Reply")]
    public PhoneLine[] disconnectReply = new PhoneLine[0];

    public LineReply[] additionalReplies = new LineReply[0];

    public PhoneLine[] ReplyFor(string lineId)
    {
        if (string.IsNullOrWhiteSpace(lineId))
        {
            return Array.Empty<PhoneLine>();
        }

        if (Matches(lineId, policeLineId)) return policeReply ?? Array.Empty<PhoneLine>();
        if (Matches(lineId, ambulanceLineId)) return ambulanceReply ?? Array.Empty<PhoneLine>();
        if (Matches(lineId, psychoLineId)) return psychoReply ?? Array.Empty<PhoneLine>();
        if (Matches(lineId, fireLineId)) return fireReply ?? Array.Empty<PhoneLine>();
        if (Matches(lineId, pizzaLineId)) return pizzaReply ?? Array.Empty<PhoneLine>();
        if (Matches(lineId, serviceLineId)) return serviceReply ?? Array.Empty<PhoneLine>();
        if (Matches(lineId, graveyardLineId)) return graveyardReply ?? Array.Empty<PhoneLine>();
        if (Matches(lineId, disconnectLineId)) return disconnectReply ?? Array.Empty<PhoneLine>();

        if (additionalReplies != null)
        {
            foreach (LineReply reply in additionalReplies)
            {
                if (reply != null && Matches(lineId, reply.lineId))
                {
                    return reply.lines ?? Array.Empty<PhoneLine>();
                }
            }
        }

        return Array.Empty<PhoneLine>();
    }

    public bool IsCorrectLine(string lineId)
    {
        return Matches(lineId, correctLineId);
    }

    public void ApplyEndingResult(bool correctLine)
    {
        if (ending == null)
        {
            return;
        }

        ending.ForceEnding(correctLine);
    }

    private static bool Matches(string first, string second)
    {
        return !string.IsNullOrWhiteSpace(first) && string.Equals(first.Trim(), second?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
