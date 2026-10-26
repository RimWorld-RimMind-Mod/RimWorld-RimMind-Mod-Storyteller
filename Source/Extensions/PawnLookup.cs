using RimMind.Presentation.Api;
using Verse;

namespace RimMind.Storyteller.Extensions
{
    /// <summary>
    /// ContextKey 提供者共享的小人解析逻辑，统一委托给 <see cref="RimMindPawnLookup"/>（不扫描 WorldPawns）。
    /// </summary>
    public static class PawnLookup
    {
        /// <summary>
        /// 按 thingIDNumber 查找小人。id 无效（&lt;= 0）或未找到时返回 null。
        /// </summary>
        public static Pawn? FindPawnById(int pawnId)
            => pawnId <= 0 ? null : RimMindPawnLookup.FindPawnByNumber(pawnId);

        /// <summary>
        /// 按字符串 ThingID 查找小人。id 为空或未找到时返回 null。
        /// </summary>
        public static Pawn? FindById(string? pawnId)
            => RimMindPawnLookup.FindPawnById(pawnId);
    }
}
