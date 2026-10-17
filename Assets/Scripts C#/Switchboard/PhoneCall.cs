using System;
using UnityEngine;

namespace LudumDare60.Switchboard
{
    public enum ServiceDestination
    {
        [InspectorName("01 — Полиция")] Police,
        [InspectorName("02 — Психиатр")] Psychiatrist,
        [InspectorName("03 — Скорая помощь")] Ambulance,
        [InspectorName("04 — Пожарная служба")] Fire,
        [InspectorName("05 — Справочная")] Information
    }

    [Serializable]
    public class PhoneLine
    {
        public string speaker;
        [TextArea(2, 5)] public string text;
    }

    [CreateAssetMenu(menuName = "Novel/Phone Call", fileName = "PhoneCall")]
    public class PhoneCall : ScriptableObject
    {
        public string title;
        public ServiceDestination correctDestination;
        public PhoneLine[] conversation = new PhoneLine[0];
        public PhoneLine[] policeReply = new PhoneLine[0];
        public PhoneLine[] psychiatristReply = new PhoneLine[0];
        public PhoneLine[] ambulanceReply = new PhoneLine[0];
        public PhoneLine[] fireReply = new PhoneLine[0];
        public PhoneLine[] informationReply = new PhoneLine[0];

        public PhoneLine[] ReplyFor(ServiceDestination destination)
        {
            switch (destination)
            {
                case ServiceDestination.Police: return policeReply;
                case ServiceDestination.Psychiatrist: return psychiatristReply;
                case ServiceDestination.Ambulance: return ambulanceReply;
                case ServiceDestination.Fire: return fireReply;
                case ServiceDestination.Information: return informationReply;
                default: throw new ArgumentOutOfRangeException(nameof(destination));
            }
        }
    }
}
