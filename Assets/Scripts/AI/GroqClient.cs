using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace HackKU.AI
{
    // Plain async client for Groq chat + whisper. Uses UnityWebRequest wrapped in a
    // TaskCompletionSource (same technique as HackKU.TTS.ElevenLabsClient coroutines, but async).
    // NOTE: ToolDef.function.parametersJson must be a pre-serialized JSON-Schema string; we splice
    // it into the request body manually because JsonUtility cannot serialize generic object graphs.
    public class GroqClient
    {
        public int TimeoutSeconds { get; set; } = 60;
        public bool VerboseLogging { get; set; } = false;

        // ---------- Chat ----------

        public async Task<ChatResponse> SendChatAsync(
            List<ChatMessage> messages,
            List<ToolDef> tools,
            CancellationToken ct)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));

            var body = BuildChatRequestJson(GroqConfig.ChatModel, messages, tools, temperature: 0.7f);
            var payload = Encoding.UTF8.GetBytes(body);

            using var req = new UnityWebRequest(GroqConfig.ChatUrl, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(payload) { contentType = "application/json" };
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + GroqConfig.ApiKey);
            req.timeout = TimeoutSeconds;

            if (VerboseLogging) Debug.Log($"[Groq] POST {GroqConfig.ChatUrl} ({messages.Count} msgs, {tools?.Count ?? 0} tools)");

            await SendAsync(req, ct);

            if (req.result != UnityWebRequest.Result.Success)
            {
                var msg = $"HTTP {(int)req.responseCode} {req.error}";
                if (req.downloadHandler?.data != null && req.downloadHandler.data.Length > 0 &&
                    req.downloadHandler.data.Length < 8192)
                {
                    msg += " | " + Encoding.UTF8.GetString(req.downloadHandler.data);
                }
                throw new Exception("[Groq] chat failed: " + msg);
            }

            var json = req.downloadHandler.text;
            if (VerboseLogging) Debug.Log("[Groq] response: " + Preview(json, 400));

            ChatResponse parsed;
            try { parsed = JsonUtility.FromJson<ChatResponse>(json); }
            catch (Exception ex) { throw new Exception("[Groq] chat JSON parse failed: " + ex.Message + "\n" + Preview(json, 400)); }

            return parsed;
        }

        // ---------- Whisper ----------

        public async Task<string> TranscribeAsync(byte[] wavBytes, CancellationToken ct)
        {
            if (wavBytes == null || wavBytes.Length == 0) throw new ArgumentException("wavBytes empty", nameof(wavBytes));

            var form = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("file", wavBytes, "speech.wav", "audio/wav"),
                new MultipartFormDataSection("model", GroqConfig.WhisperModel),
                new MultipartFormDataSection("response_format", "json"),
            };

            using var req = UnityWebRequest.Post(GroqConfig.TranscriptionsUrl, form);
            req.SetRequestHeader("Authorization", "Bearer " + GroqConfig.ApiKey);
            req.SetRequestHeader("Accept", "application/json");
            req.timeout = TimeoutSeconds;

            if (VerboseLogging) Debug.Log($"[Groq] POST {GroqConfig.TranscriptionsUrl} ({wavBytes.Length} bytes)");

            await SendAsync(req, ct);

            if (req.result != UnityWebRequest.Result.Success)
            {
                var msg = $"HTTP {(int)req.responseCode} {req.error}";
                if (req.downloadHandler?.data != null && req.downloadHandler.data.Length > 0 &&
                    req.downloadHandler.data.Length < 8192)
                {
                    msg += " | " + Encoding.UTF8.GetString(req.downloadHandler.data);
                }
                throw new Exception("[Groq] whisper failed: " + msg);
            }

            var json = req.downloadHandler.text;
            if (VerboseLogging) Debug.Log("[Groq] transcription: " + Preview(json, 400));

            TranscriptionResponse parsed;
            try { parsed = JsonUtility.FromJson<TranscriptionResponse>(json); }
            catch (Exception ex) { throw new Exception("[Groq] transcription JSON parse failed: " + ex.Message); }

            return parsed?.text ?? string.Empty;
        }

        // ---------- Request body builder ----------

        // Builds the chat-completions body by hand so we can inline the raw parametersJson for each tool.
        internal static string BuildChatRequestJson(
            string model,
            List<ChatMessage> messages,
            List<ToolDef> tools,
            float temperature)
        {
            var sb = new StringBuilder(1024);
            sb.Append('{');

            AppendStringProp(sb, "model", model); sb.Append(',');

            sb.Append("\"messages\":[");
            if (messages != null)
            {
                for (int i = 0; i < messages.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    AppendMessageJson(sb, messages[i]);
                }
            }
            sb.Append(']');

            if (tools != null && tools.Count > 0)
            {
                sb.Append(",\"tools\":[");
                for (int i = 0; i < tools.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    AppendToolDefJson(sb, tools[i]);
                }
                sb.Append(']');
                sb.Append(",\"tool_choice\":\"auto\"");
            }

            sb.Append(",\"temperature\":").Append(temperature.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(",\"stream\":false");

            sb.Append('}');
            return sb.ToString();
        }

        private static void AppendMessageJson(StringBuilder sb, ChatMessage m)
        {
            sb.Append('{');
            bool first = true;

            if (!string.IsNullOrEmpty(m.role)) { AppendComma(sb, ref first); AppendStringProp(sb, "role", m.role); }

            // content can legitimately be null (e.g., assistant tool_call response), so include null if no content.
            AppendComma(sb, ref first);
            sb.Append("\"content\":");
            if (m.content == null) sb.Append("null");
            else sb.Append(EncodeString(m.content));

            if (!string.IsNullOrEmpty(m.name)) { AppendComma(sb, ref first); AppendStringProp(sb, "name", m.name); }
            if (!string.IsNullOrEmpty(m.tool_call_id)) { AppendComma(sb, ref first); AppendStringProp(sb, "tool_call_id", m.tool_call_id); }

            if (m.tool_calls != null && m.tool_calls.Count > 0)
            {
                AppendComma(sb, ref first);
                sb.Append("\"tool_calls\":[");
                for (int i = 0; i < m.tool_calls.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    var tc = m.tool_calls[i];
                    sb.Append('{');
                    AppendStringProp(sb, "id", tc.id ?? string.Empty); sb.Append(',');
                    AppendStringProp(sb, "type", tc.type ?? "function"); sb.Append(',');
                    sb.Append("\"function\":{");
                    AppendStringProp(sb, "name", tc.function?.name ?? string.Empty); sb.Append(',');
                    AppendStringProp(sb, "arguments", tc.function?.arguments ?? "{}");
                    sb.Append('}');
                    sb.Append('}');
                }
                sb.Append(']');
            }

            sb.Append('}');
        }

        private static void AppendToolDefJson(StringBuilder sb, ToolDef t)
        {
            sb.Append('{');
            AppendStringProp(sb, "type", string.IsNullOrEmpty(t.type) ? "function" : t.type); sb.Append(',');

            sb.Append("\"function\":{");
            AppendStringProp(sb, "name", t.function?.name ?? string.Empty); sb.Append(',');
            AppendStringProp(sb, "description", t.function?.description ?? string.Empty); sb.Append(',');

            var paramsJson = t.function?.parametersJson;
            if (string.IsNullOrWhiteSpace(paramsJson)) paramsJson = "{\"type\":\"object\",\"properties\":{}}";
            sb.Append("\"parameters\":").Append(paramsJson);
            sb.Append('}');

            sb.Append('}');
        }

        private static void AppendComma(StringBuilder sb, ref bool first)
        {
            if (first) { first = false; return; }
            sb.Append(',');
        }

        private static void AppendStringProp(StringBuilder sb, string key, string value)
        {
            sb.Append('"').Append(key).Append("\":").Append(EncodeString(value));
        }

        private static string EncodeString(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        // ---------- UnityWebRequest -> Task bridge ----------

        private static Task SendAsync(UnityWebRequest req, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var op = req.SendWebRequest();

            CancellationTokenRegistration reg = default;
            if (ct.CanBeCanceled)
            {
                reg = ct.Register(() =>
                {
                    try { if (!req.isDone) req.Abort(); } catch { /* ignore */ }
                    tcs.TrySetCanceled(ct);
                });
            }

            op.completed += _ =>
            {
                reg.Dispose();
                if (ct.IsCancellationRequested) { tcs.TrySetCanceled(ct); return; }
                tcs.TrySetResult(true);
            };

            return tcs.Task;
        }

        private static string Preview(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }
    }
}
