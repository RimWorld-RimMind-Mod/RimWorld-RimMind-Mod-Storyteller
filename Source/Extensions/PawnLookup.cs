using System.Linq;
using RimMind.Presentation.Api;
using Verse;

namespace RimMind.Storyteller.Extensions
{
    /// <summary>
    /// ContextKey 提供者共享的小人解析逻辑。
    /// 优先通过 RimMindPawnLookup 与当前地图的自由殖民者查找，最后回退到 WorldPawns。
    /// </summary>
    public static class PawnLookup
    {
        /// <summary>
        /// 按 thingIDNumber 查找小人。id 无效（&lt;= 0）或未找到时返回 null。
        /// </summary>
        public static Pawn? FindPawnById(int pawnId)
        {
            if (pawnId <= 0)
                return null;

            Pawn? activePawn = RimMindPawnLookup.FindPawnByNumber(pawnId);
            if (activePawn != null)
                return activePawn;

            return PawnLookupCore.FindById(
                pawnId,
                () => Find.CurrentMap?.mapPawns?.FreeColonists ?? Enumerable.Empty<Pawn>(),
                () => Find.WorldPawns?.AllPawnsAlive,
                pawn => pawn.thingIDNumber);
        }
    }
}
