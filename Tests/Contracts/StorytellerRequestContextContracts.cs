using System.Text;
using RimMind.Storyteller.Extensions;
using RimMind.Storyteller.Memory;
using RimMind.Testing;
using RimWorld;
using Verse;
using Xunit;

namespace RimMind.Storyteller.Tests.Contracts
{
    public sealed class StorytellerRequestContextContracts
    {
        [Fact]
        public void Stable_request_context_and_memory_boundaries()
        {
            ContractCaseRunner.Run(
                ("tension clamps decay and day conversion stay deterministic", TensionMathRemainsDeterministic),
                ("custom instruction is prepended only when nonblank", CustomInstructionComposition),
                ("request state publishes once and cancellation never marks director unhealthy", RequestStatePublishesOnceAndCancelPreservesHealth),
                ("difficulty context prefers live difficultyDef label and falls back cleanly", DifficultyContextPrefersLiveLabel));
        }

        private static void TensionMathRemainsDeterministic()
        {
            Assert.Equal(0f, TensionMath.Clamp01(-0.1f));
            Assert.Equal(1f, TensionMath.Clamp01(1.1f));
            Assert.Equal(0.65f, TensionMath.ApplyDelta(0.5f, 0.15f), 3);
            Assert.Equal(0.44f, TensionMath.ComputeDecay(0.5f, 0.03f, 120000), 3);
            Assert.Equal(1, TensionMath.TicksToDay(0));
            Assert.Equal(2, TensionMath.TicksToDay(60000));

            // Below threshold: adaptive decay matches linear decay
            Assert.Equal(
                TensionMath.ComputeDecay(0.5f, 0.03f, 120000),
                TensionMath.ComputeAdaptiveDecay(0.5f, 0.03f, 120000),
                3);

            // Extreme high tension: adaptive soft-landing decay accelerates
            float normalDecay = TensionMath.ComputeDecay(0.95f, 0.03f, 60000);
            float adaptiveDecay = TensionMath.ComputeAdaptiveDecay(0.95f, 0.03f, 60000);
            Assert.True(adaptiveDecay < normalDecay);
            Assert.True(adaptiveDecay > 0.80f);
        }

        private static void CustomInstructionComposition()
        {
            Assert.Equal(
                "custom\n\ngenerated",
                StorytellerContextPolicy.ComposeTaskInstruction(" custom ", "generated"));
            Assert.Equal(
                "generated",
                StorytellerContextPolicy.ComposeTaskInstruction(" ", "generated"));
        }

        private static void RequestStatePublishesOnceAndCancelPreservesHealth()
        {
            var state = new StorytellerRequestState<string>();
            long token = 0;
            Assert.True(state.TryDispatch(
                captured =>
                {
                    token = captured;
                    Assert.True(state.IsCurrent(captured));
                },
                failureTick: 100,
                out var error));
            Assert.Null(error);
            Assert.False(state.TryDispatch(_ => { }, 101, out _));
            Assert.True(state.Publish(token, "incident", 120));
            Assert.False(state.HasPendingRequest);
            Assert.True(state.HasPendingResult);
            Assert.True(state.TryTake(out string? incident));
            Assert.Equal("incident", incident);
            Assert.False(state.TryTake(out _));

            // Intentional cancellation clears pending state without advancing LastFailTick
            long cancelToken = 0;
            Assert.True(state.TryDispatch(captured => cancelToken = captured, failureTick: 200, out _));
            Assert.True(state.CancelCurrent(cancelToken));
            Assert.False(state.HasPendingRequest);
            Assert.Equal(-99999, state.LastFailTick);
            Assert.True(state.IsDirectorHealthy(currentTick: 250, healthyThresholdTicks: 120000));

            var freshAfterLoad = new StorytellerRequestState<string>();
            Assert.True(freshAfterLoad.IsDirectorHealthy(currentTick: 500000, healthyThresholdTicks: 120000));

            freshAfterLoad.TryDispatch(_ => { }, failureTick: 500000, out _);
            Assert.True(freshAfterLoad.Fail(1, tick: 500010));
            Assert.False(freshAfterLoad.IsDirectorHealthy(currentTick: 500100, healthyThresholdTicks: 120000));
        }

        private static void DifficultyContextPrefersLiveLabel()
        {
            var prev = Find.Storyteller;
            try
            {
                var st = new RimWorld.Storyteller
                {
                    difficultyDef = new DifficultyDef { LabelCap = "Custom Modded Nightmare" }
                };
                st.difficulty.threatScale = 1.5f;
                Find.Storyteller = st;

                var sb = new StringBuilder();
                StorytellerContextBuilder.AppendDifficultyContext(sb);
                string text = sb.ToString();
                Assert.Contains("Custom Modded Nightmare", text);
                Assert.Contains("1.50", text);

                // Fallback to tier table when difficultyDef is absent
                st.difficultyDef = null;
                sb.Clear();
                StorytellerContextBuilder.AppendDifficultyContext(sb);
                Assert.Contains("Blood and Dust", sb.ToString());
            }
            finally
            {
                Find.Storyteller = prev;
            }
        }
    }
}
