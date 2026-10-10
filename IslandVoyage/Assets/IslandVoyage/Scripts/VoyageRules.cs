namespace IslandVoyage
{
    public enum SchoolMood { Calm, Fleeing, Returning }

    public static class VoyageRules
    {
        public const float MaxSpeed = 6f;
        public const float SlowSpeed = 2f;
        public const float FastSpeed = 4f;
        public const float AlertRange = 8f;
        public const float FishingRange = 12f;
        public const float CastDuration = 1.25f;
        public const float CastCooldown = 1.2f;
        public const float BiteWindow = 1.35f;

        public static bool IsThreat(float speed, float distance) => speed >= FastSpeed && distance <= AlertRange;
        public static bool CanFish(float speed, float distance, SchoolMood mood, int fishCount, float cooldown)
            => speed <= SlowSpeed && distance <= FishingRange && mood == SchoolMood.Calm && fishCount > 0 && cooldown <= 0f;
    }
}
