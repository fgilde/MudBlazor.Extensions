namespace MainSample.WebAssembly.Utils
{
    public class RandomTitleGenerator
    {
        private static readonly Random Random = new Random();

        private static readonly List<string> Adjectives = new List<string>
        {
            "Good",
            "Bad",
            "Interesting",
            "Boring",
            "Curious",
            "Impressive"
        };

        private static readonly List<string> Nouns = new List<string>
        {
            "Conversation",
            "Meeting",
            "Dialogue",
            "Chat",
            "Conference",
            "Discussion"
        };

        public static string GenerateRandomTitle()
        {
            string randomAdjective = Adjectives[Random.Next(Adjectives.Count)];
            string randomNoun = Nouns[Random.Next(Nouns.Count)];

            return $"{randomAdjective} {randomNoun}";
        }
    }
}
