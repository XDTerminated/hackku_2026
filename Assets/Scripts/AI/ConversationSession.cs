using System;
using System.Collections.Generic;

namespace HackKU.AI
{
    // Stateful conversation holder: system prompt + tool defs + rolling message history.
    // Not a MonoBehaviour — owned by whatever NPC / controller drives the chat loop.
    public class ConversationSession
    {
        private readonly List<ChatMessage> _history = new();

        public string SystemPrompt { get; set; }
        public ToolDef[] Tools { get; }

        public IReadOnlyList<ChatMessage> History => _history;

        public ConversationSession(string systemPrompt, ToolDef[] tools = null)
        {
            SystemPrompt = systemPrompt ?? string.Empty;
            Tools = tools ?? Array.Empty<ToolDef>();
        }

        public void AppendUser(string text)
        {
            if (text == null) text = string.Empty;
            _history.Add(new ChatMessage("user", text));
        }

        public void AppendAssistant(ChatMessage msg)
        {
            if (msg == null) throw new ArgumentNullException(nameof(msg));
            if (string.IsNullOrEmpty(msg.role)) msg.role = "assistant";
            _history.Add(msg);
        }

        public void AppendToolResult(string toolCallId, string resultJson)
        {
            if (string.IsNullOrEmpty(toolCallId)) throw new ArgumentException("toolCallId empty", nameof(toolCallId));
            _history.Add(new ChatMessage
            {
                role = "tool",
                tool_call_id = toolCallId,
                content = resultJson ?? string.Empty,
            });
        }

        public void Clear()
        {
            _history.Clear();
        }

        // Returns a fresh list: [system, ...history]. Safe to mutate without touching history.
        public List<ChatMessage> BuildRequestMessages()
        {
            var list = new List<ChatMessage>(_history.Count + 1);
            if (!string.IsNullOrEmpty(SystemPrompt))
            {
                list.Add(new ChatMessage("system", SystemPrompt));
            }
            list.AddRange(_history);
            return list;
        }
    }
}
