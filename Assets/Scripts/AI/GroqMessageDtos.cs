using System;
using System.Collections.Generic;

namespace HackKU.AI
{
    // DTOs for Groq (OpenAI-compatible) chat & whisper responses.
    // Kept JsonUtility-friendly: only serializable primitives, strings, and List<T>.
    // Note: tool "parameters" is schema JSON — JsonUtility can't serialize generic object, so
    // we carry it as a pre-serialized JSON string and splice it into the request manually in GroqClient.

    [Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;
        public string name;
        public string tool_call_id;
        public List<ToolCall> tool_calls;

        public ChatMessage() { }

        public ChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    [Serializable]
    public class ToolCall
    {
        public string id;
        public string type;
        public ToolCallFunction function;
    }

    [Serializable]
    public class ToolCallFunction
    {
        public string name;
        // Groq/OpenAI return this as a JSON-encoded string — parse downstream.
        public string arguments;
    }

    [Serializable]
    public class ToolDef
    {
        public string type = "function";
        public ToolFunctionDef function;
    }

    [Serializable]
    public class ToolFunctionDef
    {
        public string name;
        public string description;
        // Pre-serialized JSON-Schema for the function parameters.
        // Example: "{\"type\":\"object\",\"properties\":{\"city\":{\"type\":\"string\"}},\"required\":[\"city\"]}"
        public string parametersJson;
    }

    [Serializable]
    public class ChatRequest
    {
        public string model;
        public List<ChatMessage> messages;
        public List<ToolDef> tools;
        public string tool_choice = "auto";
        public float temperature = 0.7f;
        public bool stream = false;
    }

    [Serializable]
    public class ChatResponse
    {
        public List<ChatChoice> choices;
        public Usage usage;
    }

    [Serializable]
    public class ChatChoice
    {
        public int index;
        public ChatMessage message;
        public string finish_reason;
    }

    [Serializable]
    public class Usage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }

    [Serializable]
    public class TranscriptionResponse
    {
        public string text;
    }
}
