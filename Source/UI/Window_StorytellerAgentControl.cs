using RimMind.Presentation.Api;
using RimMind.Storyteller.Agent;
using UnityEngine;
using Verse;

namespace RimMind.Storyteller.UI
{
    public sealed class Window_StorytellerAgentControl : Window
    {
        private Vector2 _debugScrollPos = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(560f, 420f);

        public Window_StorytellerAgentControl()
        {
            doCloseX = true;
            absorbInputAroundWindow = false;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.Agent.Title", "Storyteller Agent"));
            Text.Font = GameFont.Small;

            var agent = StorytellerAgentController.Find();
            float y = inRect.y + 42f;
            string notCreated = RimMindAPI.Text.TrOr(
                "RimMind.Storyteller.UI.Agent.NotCreated",
                "No Storyteller agent has been created yet.");

            string stateDisplay;
            if (agent == null)
            {
                stateDisplay = notCreated;
            }
            else
            {
                string stateKey = $"RimMind.Agent.State.{agent.State}";
                stateDisplay = RimMindAPI.Text.TrOr(stateKey, agent.State.ToString());
            }

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 24f),
                RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.Agent.State", "State: {0}", stateDisplay));
            y += 34f;

            const float gap = 8f;
            float btnWidth = Mathf.Floor((inRect.width - gap * 3f) / 4f);
            float bx = inRect.x;

            if (Widgets.ButtonText(new Rect(bx, y, btnWidth, 32f),
                RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.Agent.Start", "Start")))
            {
                StorytellerAgentController.Start();
            }
            bx += btnWidth + gap;

            if (Widgets.ButtonText(new Rect(bx, y, btnWidth, 32f),
                RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.Agent.Pause", "Pause")))
            {
                StorytellerAgentController.Pause();
            }
            bx += btnWidth + gap;

            if (Widgets.ButtonText(new Rect(bx, y, btnWidth, 32f),
                RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.Agent.ForceThink", "Force Think")))
            {
                StorytellerAgentController.ForceThink();
            }
            bx += btnWidth + gap;

            float lastBtnWidth = Mathf.Max(btnWidth, inRect.xMax - bx);
            if (Widgets.ButtonText(new Rect(bx, y, lastBtnWidth, 32f),
                RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.Agent.OpenRequests", "Requests")))
            {
                RimMindAPI.Debug.OpenAIRequests();
            }

            y += 42f;
            Rect scrollOuter = new Rect(inRect.x, y, inRect.width, Mathf.Max(40f, inRect.yMax - y));
            string debugText = agent?.GetDebugInfo() ?? notCreated;
            float contentWidth = Mathf.Max(20f, scrollOuter.width - 16f);
            float contentHeight = Mathf.Max(scrollOuter.height, Text.CalcHeight(debugText, contentWidth) + 8f);
            Rect scrollView = new Rect(0f, 0f, contentWidth, contentHeight);

            Widgets.BeginScrollView(scrollOuter, ref _debugScrollPos, scrollView);
            Widgets.Label(scrollView, debugText);
            Widgets.EndScrollView();
        }
    }
}
