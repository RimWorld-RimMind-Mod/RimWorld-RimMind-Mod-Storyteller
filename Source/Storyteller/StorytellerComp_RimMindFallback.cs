using System.Collections.Generic;
using System.Linq;
using RimMind.Presentation.Api;
using RimMind.Storyteller.Settings;
using RimWorld;
using Verse;

namespace RimMind.Storyteller
{
    public class StorytellerComp_RimMindFallback : RimWorld.StorytellerComp
    {
        private static IncidentCategoryDef? _factionArrivalCat;

        private static IncidentCategoryDef? FactionArrivalCat =>
            _factionArrivalCat ??= DefDatabase<IncidentCategoryDef>.GetNamedSilentFail(
                IncidentSelectionPolicy.FactionArrivalCategoryDefName);

        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            if (target == null || !target.IncidentTargetTags().Contains(IncidentTargetTagDefOf.Map_PlayerHome))
                yield break;

            if (target is not Map)
                yield break;

            var settings = RimMindStorytellerMod.Settings;
            if (settings == null) yield break;

            var mode = settings.fallbackMode;
            if (mode == FallbackMode.None) yield break;

            if (RimMindAPI.Ext.ShouldSkipStorytellerIncident())
                yield break;

            var aiComp = Find.Storyteller?.storytellerComps?
                .OfType<StorytellerComp_RimMindDirector>()
                .FirstOrDefault();

            if (aiComp != null && aiComp.IsActive)
                yield break;

            if (RimMindAPI.Settings.IsConfigured() && settings.enableIntervalTrigger && aiComp != null)
            {
                int now = Find.TickManager?.TicksGame ?? 0;
                float directorMtb = settings.mtbDays;
                int healthyThreshold = (int)(directorMtb * 60000f * 2f);

                if (aiComp.IsDirectorHealthy(now, healthyThreshold))
                    yield break;
            }

            FallbackProfile profile = IncidentSelectionPolicy.ResolveFallbackProfile(mode.ToString());
            if (!Rand.MTBEventOccurs(profile.MtbDays, 60000f, 1000f))
                yield break;

            IncidentCategoryDef category = ResolveCategory(profile.ChooseCategorySlot(Rand.Value));
            IncidentParms parms = GenerateParms(category, target);

            var usable = UsableIncidentsInCategory(category, parms);
            if (!usable.Any()) yield break;

            if (TrySelectRandomIncident(usable, out IncidentDef def, target))
                yield return new FiringIncident(def, this, parms);
        }

        private static IncidentCategoryDef ResolveCategory(FallbackCategorySlot slot) => slot switch
        {
            FallbackCategorySlot.ThreatBig => IncidentCategoryDefOf.ThreatBig,
            FallbackCategorySlot.ThreatSmall => IncidentCategoryDefOf.ThreatSmall,
            FallbackCategorySlot.Misc => IncidentCategoryDefOf.Misc,
            FallbackCategorySlot.FactionArrival => FactionArrivalCat ?? IncidentCategoryDefOf.ThreatSmall,
            _ => IncidentCategoryDefOf.ThreatBig,
        };
    }
}
