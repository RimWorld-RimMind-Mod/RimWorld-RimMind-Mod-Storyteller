using System.Collections.Generic;
using RimMind.Presentation.Api;
using RimMind.Storyteller.UI;
using Verse;

namespace RimMind.Storyteller.Comps
{
    public class CompStorytellerAltar : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                defaultLabel = RimMindAPI.Text.TrOr(
                    "RimMind.Storyteller.Altar.Communicate",
                    "Communicate with Storyteller"),
                defaultDesc = RimMindAPI.Text.TrOr(
                    "RimMind.Storyteller.Altar.CommunicateDesc",
                    "Open dialogue with the AI storyteller"),
                icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/StorytellerAltarIcon", reportFailure: false),
                action = () =>
                {
                    var map = parent.Map;
                    if (map == null) return;
                    Find.WindowStack.Add(new Window_StorytellerDialogue(map));
                }
            };
        }
    }

    public class CompProperties_StorytellerAltar : CompProperties
    {
        public CompProperties_StorytellerAltar()
        {
            compClass = typeof(CompStorytellerAltar);
        }
    }
}
