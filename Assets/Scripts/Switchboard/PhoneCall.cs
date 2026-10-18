using System;
using UnityEngine;

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
    public string policeLineId = "police";
    public PhoneLine[] policeReply = new PhoneLine[0];
    public string psychiatristLineId = "ambulance";
    public PhoneLine[] psychiatristReply = new PhoneLine[0];
    public string ambulanceLineId = "psycho";
    public PhoneLine[] ambulanceReply = new PhoneLine[0];
    public string fireLineId = "fire";
    public PhoneLine[] fireReply = new PhoneLine[0];
    public string informationLineId = "pizza";
    public PhoneLine[] informationReply = new PhoneLine[0];
    public string line6Id = "service";
    public PhoneLine[] line6Reply = new PhoneLine[0];
    public string line7Id = "graveyard";
    public PhoneLine[] line7Reply = new PhoneLine[0];
    public string line8Id = "disconnect";
    public PhoneLine[] line8Reply = new PhoneLine[0];
    public LineReply[] additionalReplies = new LineReply[0];

    public PhoneLine[] ReplyFor(string lineId)
    {
        if (string.IsNullOrWhiteSpace(lineId))
        {
            return Array.Empty<PhoneLine>();
        }

        if (Matches(lineId, policeLineId)) return policeReply ?? Array.Empty<PhoneLine>();
        if (Matches(lineId, psychiatristLineId)) return psychiatristReply ?? Array.Empty<PhoneLine>();
        if (Matches(lineId, ambulanceLineId)) return ambulanceReply ?? Array.Empty<PhoneLine>();
        if (Matches(lineId, fireLineId)) return fireReply ?? Array.Empty<PhoneLine>();
        if (Matches(lineId, informationLineId)) return informationReply ?? Array.Empty<PhoneLine>();
        if (Matches(lineId, line6Id)) return line6Reply ?? Array.Empty<PhoneLine>();
        if (Matches(lineId, line7Id)) return line7Reply ?? Array.Empty<PhoneLine>();
        if (Matches(lineId, line8Id)) return line8Reply ?? Array.Empty<PhoneLine>();

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

    private static bool Matches(string first, string second)
    {
        return !string.IsNullOrWhiteSpace(first) && string.Equals(first.Trim(), second?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
