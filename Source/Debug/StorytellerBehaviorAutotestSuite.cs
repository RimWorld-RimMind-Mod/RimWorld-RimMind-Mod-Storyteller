using System;
using System.Linq;
using LudeonTK;
using RimMind.Presentation.Api;
using RimMind.Storyteller.Memory;
using RimWorld;
using Verse;

namespace RimMind.Storyteller.Debug
{
    /// <summary>
    /// In-game behavioral autotest suite for RimMind-Storyteller.
    /// Discovered automatically by Core's BehaviorAutotestRunner and exposed to Dev menu.
    /// </summary>
    public sealed class StorytellerBehaviorAutotestSuite : BehaviorAutotestSuiteBase
    {
        public override string ModId => "Storyteller";
        public override string SuiteId => "Behavior.StorytellerCore";

        [DebugAction("Autotests", "Run Storyteller In-Game Behavior Test", actionType = DebugActionType.Action)]
        public static void RunFromDevMenu() => RunSuiteFromDevMenu<StorytellerBehaviorAutotestSuite>();

        public override void RunSuite(IInGameBehaviorSuiteContext context)
        {
            // 1. StorytellerMemory WorldComponent
            var memory = StorytellerMemory.Instance;
            context.Assert(memory != null, "StorytellerMemory WorldComponent instance is available");

            if (memory != null)
            {
                context.Assert(memory.TensionLevel >= 0f && memory.TensionLevel <= 1f, $"Current storyteller tension level is valid 0-1 range ({memory.TensionLevel:F2})");

                // 2. DialogueRecord Addition and Cleanup
                int beforeCount = memory.DialogueRecords.Count;
                memory.RecordDialogue("assistant", "Storyteller autotest greeting", Find.TickManager.TicksGame);
                context.Assert(memory.DialogueRecords.Count == beforeCount + 1, "DialogueRecord added successfully to StorytellerMemory");

                // Self-cleanup
                memory.ClearDialogueRecords();
                context.Assert(memory.DialogueRecords.Count == 0, "Dialogue records cleanly cleared after test");
            }

            // 3. TensionMath Calculations
            float clampedUpper = TensionMath.Clamp01(1.5f);
            float clampedLower = TensionMath.Clamp01(-0.5f);
            float decayed = TensionMath.ComputeDecay(0.8f, 0.2f, TensionMath.TicksPerDay);
            float delta = TensionMath.ApplyDelta(0.4f, 0.3f);

            context.Assert(clampedUpper == 1.0f, $"TensionMath.Clamp01(1.5f) clamped to 1.0 (got: {clampedUpper})");
            context.Assert(clampedLower == 0.0f, $"TensionMath.Clamp01(-0.5f) clamped to 0.0 (got: {clampedLower})");
            context.Assert(Math.Abs(decayed - 0.6f) < 0.01f, $"TensionMath.ComputeDecay correctly decays tension (got: {decayed:F2}, expected 0.60)");
            context.Assert(Math.Abs(delta - 0.7f) < 0.01f, $"TensionMath.ApplyDelta correctly applies delta (got: {delta:F2}, expected 0.70)");

            // 4. StorytellerResponseParserPure JSON Parsing
            string testJson = "{\"defName\":\"Eclipse\",\"reason\":\"Autotest dramatic sky darkening\",\"points\":150}";
            var parsed = StorytellerResponseParserPure.ParseResponse(testJson);
            context.Assert(parsed != null, "StorytellerResponseParserPure parsed valid incident JSON");
            context.Assert(parsed?.defName == "Eclipse", $"Parsed incident defName is 'Eclipse' (got: {parsed?.defName})");
        }
    }
}
