using RimMind.Presentation.Api;

namespace RimMind.Storyteller
{
    /// <summary>
    /// 纯逻辑版本的叙事者响应解析器，统一委托 Core <see cref="RimMindAPI.Json"/> 进行
    /// 思考标签/代码块剥离、字符串感知 JSON 提取、截断修复与伪函数调用解析。
    /// 不涉及 DefDatabase/FiringIncident 等游戏逻辑，供测试项目直接编译使用。
    /// </summary>
    public static class StorytellerResponseParserPure
    {
        /// <summary>
        /// 从 AI 响应中剥离思考标签与 markdown 代码块标记并提取干净的 JSON 子串。
        /// </summary>
        public static string ExtractJsonSubstring(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            string? extracted = RimMindAPI.Json.ExtractJsonPayload(input);
            if (!string.IsNullOrEmpty(extracted))
                return extracted!.Trim();
            return RimMindAPI.Json.StripThinkingAndCodeFences(input);
        }

        /// <summary>
        /// 解析 AI 响应内容，尝试反序列化为 IncidentResponse。
        /// 失败时通过 Core JSON 修复与伪函数调用解析兜底。若 defName 为空则返回 null。
        /// </summary>
        public static IncidentResponse? ParseResponse(string aiContent)
        {
            if (string.IsNullOrWhiteSpace(aiContent)) return null;

            if (RimMindAPI.Json.TryDeserializeWithRepair<IncidentResponse>(aiContent, out var result)
                && result != null
                && !string.IsNullOrEmpty(result.defName))
            {
                return result;
            }

            var pseudoCalls = RimMindAPI.Json.ParsePseudoFunctionCalls(aiContent);
            if (pseudoCalls != null)
            {
                for (int i = 0; i < pseudoCalls.Count; i++)
                {
                    if (RimMindAPI.Json.TryDeserializeWithRepair<IncidentResponse>(
                            pseudoCalls[i].ArgumentsJson,
                            out var pseudoResult)
                        && pseudoResult != null
                        && !string.IsNullOrEmpty(pseudoResult.defName))
                    {
                        return pseudoResult;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 尝试修复截断的 JSON。若输入已是合法 JSON 则返回 null（无需修复），
        /// 若输入为空白或不可修复则返回 null。
        /// </summary>
        public static string? TryRepairTruncatedJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            string clean = ExtractJsonSubstring(input);
            if (string.IsNullOrWhiteSpace(clean)) return null;
            return RimMindAPI.Json.TryRepairTruncatedJson(clean);
        }
    }
}
