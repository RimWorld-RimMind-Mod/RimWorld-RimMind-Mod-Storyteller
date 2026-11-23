using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RimMind.Testing;
using Xunit;

namespace RimMind.Storyteller.Tests.Contracts
{
    /// <summary>
    /// Regression contract for queue identity & cooldown separation:
    /// - Incident selection uses <see cref="StorytellerRequestIds.RequestModId"/> matching <c>StorytellerModCooldown</c>.
    /// - Altar chat uses <see cref="StorytellerRequestIds.ChatModId"/> which MUST NOT match or prefix-match
    ///   <see cref="StorytellerRequestIds.RequestModId"/> or <see cref="StorytellerRequestIds.OwnerModId"/>
    ///   under Core's <c>QueueCircuitBreaker.MatchesModId</c>.
    /// </summary>
    public sealed class StorytellerQueueIdentityContracts
    {
        [Fact]
        public void Queue_cooldown_key_and_chat_mod_id_stay_isolated()
        {
            ContractCaseRunner.Run(
                ("incident request envelope uses the module-owned incident request id", () =>
                {
                    var envelope = StorytellerIncidentRequestFactory.Create("Storyteller", "{}", StorytellerRequestIds.DefaultNpcId);
                    Assert.Equal(StorytellerRequestIds.RequestModId, envelope.ModId);
                    Assert.Equal("Storyteller", envelope.ScenarioId);
                    Assert.Equal(StorytellerRequestIds.DefaultNpcId, envelope.NpcId);
                    Assert.Equal(StorytellerIncidentRequestFactory.MaxTokens, envelope.MaxTokens);
                }),
                ("altar chat ModId never collides with incident cooldown key or OwnerModId prefix", () =>
                {
                    Assert.False(MatchesQueueCooldownKey(StorytellerRequestIds.ChatModId, StorytellerRequestIds.RequestModId));
                    Assert.False(MatchesQueueCooldownKey(StorytellerRequestIds.ChatModId, StorytellerRequestIds.OwnerModId));
                    Assert.True(MatchesQueueCooldownKey(StorytellerRequestIds.RequestModId, StorytellerRequestIds.RequestModId));
                }),
                ("source never clears or reads a cooldown with a literal key or diverging ModId", () =>
                {
                    var literalCooldownKey = new Regex(@"(ClearModCooldown|GetModCooldownTicksLeft)\(\s*""");
                    var literalModId = new Regex(@"WithModId\(\s*""(?<id>[^""]*)""");
                    string sourceRoot = SourceRoot();
                    var files = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
                        .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                        .ToList();
                    Assert.NotEmpty(files);
                    foreach (var file in files)
                    {
                        string text = File.ReadAllText(file);
                        Assert.False(literalCooldownKey.IsMatch(text),
                            $"{Path.GetFileName(file)} passes a literal cooldown key; use StorytellerRequestIds.RequestModId.");
                        Assert.False(literalModId.IsMatch(text),
                            $"{Path.GetFileName(file)} passes a raw literal ModId; use StorytellerRequestIds constants.");
                    }
                }));
        }

        private static bool MatchesQueueCooldownKey(string modId, string candidate)
        {
            if (string.Equals(modId, candidate, StringComparison.OrdinalIgnoreCase))
                return true;

            if (modId.StartsWith(candidate, StringComparison.OrdinalIgnoreCase) && modId.Length > candidate.Length)
            {
                char sep = modId[candidate.Length];
                if (sep == '.' || sep == '-' || sep == '_' || sep == ':' || sep == '/')
                    return true;
            }
            return false;
        }

        private static string SourceRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null &&
                   !Directory.Exists(Path.Combine(directory.FullName, "RimMind-Storyteller", "Source")))
                directory = directory.Parent;

            return Path.Combine(
                directory?.FullName ?? throw new InvalidOperationException("Repository root not found."),
                "RimMind-Storyteller",
                "Source");
        }
    }
}
