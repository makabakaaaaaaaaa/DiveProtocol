using System;

namespace DiveProtocol.Enemies.CorpseReanimation
{
    /// <summary>
    /// Static event hub for optional UI, audio, build, and analytics hooks.
    /// </summary>
    public static class CorpseReanimationEvents
    {
        public static event Action<ReanimatingCorpseEnemy> CorpseReanimationRolled;
        public static event Action<ReanimatingCorpseEnemy, float, bool> CorpseReanimationResult;
        public static event Action<ReanimatingCorpseEnemy> CorpseReanimated;
        public static event Action<float> CorpseActivityChanged;

        public static void RaiseCorpseReanimationRolled(ReanimatingCorpseEnemy corpse)
        {
            CorpseReanimationRolled?.Invoke(corpse);
        }

        public static void RaiseCorpseReanimationResult(
            ReanimatingCorpseEnemy corpse,
            float chance,
            bool didReanimate)
        {
            CorpseReanimationResult?.Invoke(corpse, chance, didReanimate);
        }

        public static void RaiseCorpseReanimated(ReanimatingCorpseEnemy corpse)
        {
            CorpseReanimated?.Invoke(corpse);
        }

        public static void RaiseCorpseActivityChanged(float activity)
        {
            CorpseActivityChanged?.Invoke(activity);
        }
    }
}
