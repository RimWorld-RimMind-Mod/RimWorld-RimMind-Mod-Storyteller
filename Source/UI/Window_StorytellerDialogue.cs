using System;
using System.Linq;
using RimMind.Application.Features.Llm;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Api;
using RimMind.Presentation.UI;
using RimMind.Storyteller.Memory;
using UnityEngine;
using Verse;

namespace RimMind.Storyteller.UI
{
    public class Window_StorytellerDialogue : RimMindChatWindowBase
    {
        private const int MaxHistoryRounds = 6;
        private static readonly Color StorytellerHeaderColor = new Color(1f, 0.9f, 0.7f);

        public override Vector2 InitialSize => new Vector2(520f, 560f);
        protected override Color HeaderColor => StorytellerHeaderColor;

        public Window_StorytellerDialogue(Map? map = null) : base()
        {
            _ = map;
            closeOnClickedOutside = true;

            var memory = StorytellerMemory.Instance;
            if (memory != null)
            {
                int loadCount = Math.Min(MaxHistoryRounds, memory.DialogueRecords.Count);
                var recentRecords = memory.DialogueRecords
                    .Skip(Math.Max(0, memory.DialogueRecords.Count - loadCount));
                foreach (var r in recentRecords)
                {
                    AppendMessage(r.role, r.content, notifyHook: false);
                }
            }
        }

        protected override string GetTitle()
            => RimMindAPI.Text.TrOr("RimMind.Storyteller.Dialogue.Title", "Communicate with Storyteller");

        protected override string GetWaitingText()
            => RimMindAPI.Text.TrOr("RimMind.Storyteller.Dialogue.Thinking", "● Thinking...");

        protected override string GetSendButtonLabel()
            => RimMindAPI.Text.TrOr("RimMind.Storyteller.Dialogue.Send", "Send");

        protected override string GetPrefixForRole(string role)
        {
            if (string.Equals(role, "user", StringComparison.OrdinalIgnoreCase))
                return RimMindAPI.Text.TrOr("RimMind.Storyteller.Dialogue.PlayerPrefix", "You: ");
            if (string.Equals(role, "system", StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            return RimMindAPI.Text.TrOr("RimMind.Storyteller.Dialogue.StorytellerPrefix", "Storyteller: ");
        }

        protected override void OnMessageAppended(ChatWindowMessage message)
        {
            if (!string.Equals(message.Role, "system", StringComparison.OrdinalIgnoreCase))
            {
                RecordDialogueToMemory(message.Role, message.Content);
            }
        }

        protected override void OnSubmitUserMessage(string trimmedInput)
        {
            if (IsClosed || IsWaiting) return;
            if (!RimMindAPI.Settings.IsConfigured()) return;

            AppendMessage("user", trimmedInput);
            SetWaiting(true);

            var envelope = LlmRequestEnvelopeBuilder
                .ForNpc(
                    StorytellerRequestIds.DefaultNpcId,
                    gameStateInfo: new GameStateInfo()
                        .AddSection("dialogue_input", trimmedInput))
                .ForScenarioId(StorytellerRequestIds.ChatScenarioId)
                .WithModId(StorytellerRequestIds.ChatModId)
                .WithRequireAliveColonist(false)
                .WithMaxTokens(400)
                .WithTemperature(0.9f)
                .WithCancellation(LifetimeToken)
                .Build();

            RimMindAPI.Request.Send(envelope, result =>
            {
                if (IsClosed) return;

                if (result.IsErr)
                {
                    if (result.Error.Code == RimMindErrorCode.Cancelled)
                    {
                        SetWaiting(false);
                        return;
                    }

                    string errText = RimMindAPI.Text.TrOr(
                        "RimMind.Storyteller.Dialogue.ErrorResponse",
                        "[Error] {0}",
                        result.Error.Message);
                    PostReply("system", errText);
                    return;
                }

                string assistantMsg = result.Value.Content?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(assistantMsg))
                {
                    string emptyText = RimMindAPI.Text.TrOr(
                        "RimMind.Storyteller.Dialogue.EmptyResponse",
                        "[Error] Empty response from storyteller.");
                    PostReply("system", emptyText);
                    return;
                }

                PostReply("assistant", assistantMsg);
            });
        }

        private static void RecordDialogueToMemory(string role, string content)
        {
            var memory = StorytellerMemory.Instance;
            if (memory == null) return;

            int tick = Find.TickManager?.TicksGame ?? 0;
            memory.RecordDialogue(role, content, tick);
            StorytellerStateChangedEvent.Publish();

            TryPushToMemoryMod(role, content, tick);
        }

        private static void TryPushToMemoryMod(string role, string content, int tick)
        {
            string prefix = string.Equals(role, "user", StringComparison.OrdinalIgnoreCase)
                ? RimMindAPI.Text.TrOr("RimMind.Storyteller.Prompt.RolePlayer", "Player")
                : RimMindAPI.Text.TrOr("RimMind.Storyteller.Prompt.RoleNarrator", "Narrator");

            RimMindAPI.Memory.AddNarratorMemory($"{prefix}: {content}", tick, 0.3f);
        }
    }
}
