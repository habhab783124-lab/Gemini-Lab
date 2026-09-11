using System;
using UnityEngine;

namespace GeminiLab.Modules.Pet.Social
{
    [CreateAssetMenu(menuName = "Gemini-Lab/Indoor Door Dialogue")]
    public sealed class IndoorDoorDialogueCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Topic
        {
            public PetId Initiator;
            public string Id;
            public string Title;
            [TextArea] public string Opening;
            [TextArea] public string Warm;
            [TextArea] public string Normal;
            [TextArea] public string NeedSpace;

            public string Reply(SocialResponseType response) => response switch
            {
                SocialResponseType.Warm => Warm,
                SocialResponseType.NeedSpace => NeedSpace,
                _ => Normal
            };
        }

        [SerializeField] private Topic[] _topics = Array.Empty<Topic>();
        public Topic GetRandomTopic(PetId initiator, Topic previous = null)
        {
            int count = 0;
            foreach (var topic in _topics)
                if (topic != null && topic.Initiator == initiator && topic != previous) count++;
            if (count == 0) return GetTopic(initiator, 0);
            int selected = UnityEngine.Random.Range(0, count);
            foreach (var topic in _topics)
                if (topic != null && topic.Initiator == initiator && topic != previous && selected-- == 0) return topic;
            return null;
        }

        public Topic GetTopic(PetId initiator, int index)
        {
            int count = 0;
            foreach (var topic in _topics) if (topic != null && topic.Initiator == initiator) count++;
            if (count == 0) return null;
            int selected = ((index % count) + count) % count;
            foreach (var topic in _topics)
                if (topic != null && topic.Initiator == initiator && selected-- == 0) return topic;
            return null;
        }
    }
}
