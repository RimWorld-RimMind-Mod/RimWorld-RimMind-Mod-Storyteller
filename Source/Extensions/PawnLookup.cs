using System;
using RimMind.Presentation.Api;
using Verse;

namespace RimMind.Storyteller.Extensions
{
    /// <summary>
    /// Legacy compatibility forwarder. Delegates directly to Core <see cref="RimMindPawnLookup"/>.
    /// </summary>
    [Obsolete("Use RimMind.Presentation.Api.RimMindPawnLookup directly.")]
    public static class PawnLookup
    {
        public static Pawn? FindPawnById(int pawnId)
            => pawnId <= 0 ? null : RimMindPawnLookup.FindPawnByNumber(pawnId);

        public static Pawn? FindById(string? pawnId)
            => RimMindPawnLookup.FindPawnById(pawnId);
    }
}
