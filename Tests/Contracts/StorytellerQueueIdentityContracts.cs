using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RimMind.Testing;
using Xunit;

namespace RimMind.Storyteller.Tests.Contracts
{
    /// <summary>
    /// Regression contract for B7: the cooldown key cleared by a forced request must be the
    /// envelope ModId Core uses to store the Storyteller queue cooldown.
    /// </summary>
    public sealed class StorytellerQueueIdentityContracts
    {
        [Fact]
        public void Queue_cooldown_key_is_the_envelope_mod_id()
        {
            ContractCaseRunner.Run(
                ("incident request envelope uses the module-owned request id", () =>
                {
                    var envelope = StorytellerIncidentRequestFactory.Create("Storyteller", "{}", "NPC-storyteller");
                    Assert.Equal(StorytellerRequestIds.RequestModId, envelope.ModId);
                    Assert.Equal("Storyteller", envelope.ScenarioId);
                    Assert.Equal("NPC-storyteller", envelope.NpcId);
                    Assert.Equal(StorytellerIncidentRequestFactory.MaxTokens, envelope.MaxTokens);
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
                        foreach (Match match in literalModId.Matches(text))
                            Assert.Equal(StorytellerRequestIds.RequestModId, match.Groups["id"].Value);
                    }
                }));
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
