using System.Linq;
using HarmonyLib;
using RimMind.Presentation.Api;
using RimMind.Storyteller.Memory;
using RimWorld;
using Verse;

namespace RimMind.Storyteller.Patch
{
    [HarmonyPatch(typeof(IncidentWorker), "TryExecute")]
    public static class Patch_IncidentWorker_TryExecute
    {
        static void Postfix(IncidentWorker __instance, IncidentParms parms, bool __result)
        {
            if (!__result) return;

            if (RimMindStorytellerMod.Settings == null) return;

            var memory = StorytellerMemory.Instance;
            if (memory == null) return;

            memory.RecordIncident(__instance.def, parms.target, Find.TickManager.TicksGame);
            memory.UpdateTension(__instance.def.category);

            bool isThreat = __instance.def.category == IncidentCategoryDefOf.ThreatBig
                || __instance.def.category == IncidentCategoryDefOf.ThreatSmall;
            if (!isThreat || parms?.quest != null)
                return;

            RimMindAPI.NotifyIncidentExecuted();

            if (RimMindAPI.CanTriggerDialogue)
            {
                var map = parms?.target as Map;
                if (map != null)
                {
                    var colonists = RimMindPawnLookup.GetEligibleColonists(map).ToList();
                    if (colonists.Count > 0)
                    {
                        Pawn pawn;
                        Rand.PushState(GenTicks.TicksAbs ^ __instance.def.shortHash);
                        try
                        {
                            pawn = colonists.RandomElement();
                        }
                        finally
                        {
                            Rand.PopState();
                        }
                        string incidentContext = "RimMind.Storyteller.Context.IncidentOccurred".CanTranslate()
                            ? "RimMind.Storyteller.Context.IncidentOccurred".Translate(__instance.def.label).ToString()
                            : $"Incident occurred: {__instance.def.label}";
                        RimMindAPI.TriggerDialogue(pawn, incidentContext);
                    }
                }
            }
        }
    }
}
