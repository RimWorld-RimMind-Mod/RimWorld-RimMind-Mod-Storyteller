namespace RimMind.Storyteller.Extensions
{
    public static class StorytellerContextPolicy
    {
        public static string ComposeTaskInstruction(
            string? customSystemPrompt,
            string generatedTaskInstruction)
        {
            if (string.IsNullOrWhiteSpace(customSystemPrompt))
                return generatedTaskInstruction;

            return $"{customSystemPrompt!.Trim()}\n\n{generatedTaskInstruction}";
        }
    }
}
