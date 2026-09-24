namespace RimMind.Storyteller.Memory
{
    public static class TensionMath
    {
        public const int TicksPerDay = RimMind.Domain.Common.RimMindTime.TicksPerDay;

        public static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        /// <summary>
        /// 将 tick 数转换为游戏天数（从 1 开始）。
        /// </summary>
        public static int TicksToDay(int tick)
        {
            return RimMind.Domain.Common.RimMindTime.TicksToDay(tick);
        }

        public static float ComputeDecay(float currentTension, float decayPerDay, int ticksElapsed)
        {
            if (ticksElapsed <= 0) return currentTension;
            float daysElapsed = ticksElapsed / (float)TicksPerDay;
            return Clamp01(currentTension - decayPerDay * daysElapsed);
        }

        /// <summary>
        /// Computes adaptive decay with non-linear dampening/soft-landing when tension is at extreme high levels.
        /// Prevents colonies from being locked in unending crisis loops.
        /// </summary>
        public static float ComputeAdaptiveDecay(
            float currentTension,
            float baseDecayPerDay,
            int ticksElapsed,
            float highTensionThreshold = 0.75f,
            float maxMultiplier = 3.0f)
        {
            if (ticksElapsed <= 0) return Clamp01(currentTension);

            float clampedTension = Clamp01(currentTension);
            float effectiveDecay = baseDecayPerDay;
            if (clampedTension > highTensionThreshold)
            {
                float excessRatio = (clampedTension - highTensionThreshold) / System.Math.Max(0.01f, 1.0f - highTensionThreshold);
                excessRatio = System.Math.Min(1.0f, excessRatio);
                float multiplier = 1.0f + (maxMultiplier - 1.0f) * excessRatio;
                effectiveDecay *= multiplier;
            }

            float daysElapsed = ticksElapsed / (float)TicksPerDay;
            return Clamp01(clampedTension - effectiveDecay * daysElapsed);
        }

        public static float ApplyDelta(float currentTension, float delta)
        {
            return Clamp01(currentTension + delta);
        }
    }
}
