using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RimMind.Storyteller
{
    /// <summary>
    /// 纯逻辑版本的叙事者响应解析器，不依赖 RimMindAPI 或 RimWorld 运行时。
    /// 仅负责 JSON 修复与反序列化，不涉及 DefDatabase/FiringIncident 等游戏逻辑。
    /// 供测试项目直接编译使用。
    /// </summary>
    public static class StorytellerResponseParserPure
    {
        private static readonly Regex TrailingCommaRegex = new Regex(
            @",\s*([}\]])",
            RegexOptions.Compiled);

        private static readonly Regex CodeFenceRegex = new Regex(
            @"```(?:json)?\s*([\s\S]*?)\s*```",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 从AI响应中剥离 markdown 代码块标记并提取干净的 JSON 子串。
        /// </summary>
        public static string ExtractJsonSubstring(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            string text = input.Trim();

            var match = CodeFenceRegex.Match(text);
            if (match.Success)
            {
                text = match.Groups[1].Value.Trim();
            }
            else if (text.StartsWith("```"))
            {
                int firstNewline = text.IndexOf('\n');
                text = firstNewline >= 0 ? text.Substring(firstNewline + 1) : text.TrimStart('`');
                int lastFence = text.LastIndexOf("```", System.StringComparison.Ordinal);
                if (lastFence >= 0)
                    text = text.Substring(0, lastFence);
                text = text.Trim();
            }

            int firstBrace = text.IndexOf('{');
            if (firstBrace < 0)
                return text.Trim();

            int openBraces = 0;
            int closeBraces = 0;
            bool inString = false;
            bool escape = false;
            for (int i = firstBrace; i < text.Length; i++)
            {
                char c = text[i];
                if (escape)
                {
                    escape = false;
                    continue;
                }
                if (c == '\\' && inString)
                {
                    escape = true;
                    continue;
                }
                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }
                if (!inString)
                {
                    if (c == '{') openBraces++;
                    else if (c == '}') closeBraces++;
                }
            }

            int lastBrace = text.LastIndexOf('}');
            if (lastBrace > firstBrace && openBraces <= closeBraces)
            {
                text = text.Substring(firstBrace, lastBrace - firstBrace + 1);
            }
            else
            {
                text = text.Substring(firstBrace);
            }

            return text.Trim();
        }

        /// <summary>
        /// 解析AI响应内容，尝试反序列化为 IncidentResponse。
        /// 失败时尝试修复截断JSON后重试。若 defName 为空则返回 null。
        /// </summary>
        public static IncidentResponse? ParseResponse(string aiContent)
        {
            if (string.IsNullOrEmpty(aiContent)) return null;

            string clean = ExtractJsonSubstring(aiContent);
            if (string.IsNullOrEmpty(clean)) return null;

            IncidentResponse? result;
            try
            {
                result = JsonConvert.DeserializeObject<IncidentResponse>(clean);
            }
            catch
            {
                string? repaired = TryRepairTruncatedJson(clean);
                if (repaired != null)
                {
                    try { result = JsonConvert.DeserializeObject<IncidentResponse>(repaired); }
                    catch { result = null; }
                }
                else
                {
                    result = null;
                }
            }

            if (result == null || string.IsNullOrEmpty(result.defName)) return null;
            return result;
        }

        /// <summary>
        /// 尝试修复截断的JSON。若输入已是合法JSON则返回null（无需修复），
        /// 若输入为空白或不可修复则返回null。
        /// </summary>
        public static string? TryRepairTruncatedJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            string clean = ExtractJsonSubstring(input);
            if (string.IsNullOrWhiteSpace(clean)) return null;
            try
            {
                JToken.Parse(clean);
                return null; // 已是合法JSON，无需修复
            }
            catch
            {
                return Repair(clean);
            }
        }

        private static string Repair(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "{}";

            var balanced = BalanceBrackets(input);
            return TrailingCommaRegex.Replace(balanced, "$1");
        }

        private static string BalanceBrackets(string input)
        {
            int openCurly = 0, openSquare = 0;
            bool inString = false;
            bool escape = false;
            foreach (char c in input)
            {
                if (escape)
                {
                    escape = false;
                    continue;
                }
                if (c == '\\' && inString)
                {
                    escape = true;
                    continue;
                }
                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }
                if (!inString)
                {
                    if (c == '{') openCurly++;
                    else if (c == '}') openCurly--;
                    else if (c == '[') openSquare++;
                    else if (c == ']') openSquare--;
                }
            }

            var sb = new StringBuilder(input);
            if (inString)
            {
                if (escape)
                    sb.Append('\\');
                sb.Append('"');
            }

            string current = sb.ToString().TrimEnd();
            current = TrailingCommaRegex.Replace(current, "$1").TrimEnd();
            if (current.EndsWith(","))
                current = current.Substring(0, current.Length - 1).TrimEnd();
            else if (current.EndsWith(":"))
                current += "null";

            sb = new StringBuilder(current);
            while (openSquare > 0) { sb.Append(']'); openSquare--; }
            while (openCurly > 0) { sb.Append('}'); openCurly--; }
            return sb.ToString();
        }
    }
}
