using LLama.Abstractions;
using LLama.Common;
using System.Text;

namespace FastLlamaSharp.Llama
{
    public class ChatMlHistoryTransform : IHistoryTransform
    {
        public string HistoryToText(ChatHistory history)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var message in history.Messages)
            {
                // Mappt die C# Rollen auf die ChatML Bezeichnungen
                string role = message.AuthorRole switch
                {
                    AuthorRole.System => "system",
                    AuthorRole.User => "user",
                    AuthorRole.Assistant => "assistant",
                    _ => "user"
                };

                // Baut den exakten Qwen-String zusammen
                sb.Append($"<|im_start|>{role}\n{message.Content}<|im_end|>\n");
            }

            // Sagt dem Modell, dass es jetzt als Assistant antworten soll
            sb.Append("<|im_start|>assistant\n");
            return sb.ToString();
        }

        public ChatHistory TextToHistory(AuthorRole role, string text)
        {
            var history = new ChatHistory();

            // Wir bereinigen den Text sicherheitshalber von ChatML-Steuerzeichen,
            // falls das Modell oder der Stream sie nicht sauber abgeschnitten haben.
            string cleanText = text.Replace("<|im_end|>", "")
                                   .Replace("<|im_start|>", "")
                                   .Replace("assistant\n", "")
                                   .Trim();

            // Die saubere Nachricht wieder dem C#-Objekt hinzufügen
            history.AddMessage(role, cleanText);

            return history;
        }

        public IHistoryTransform Clone()
        {
            return new ChatMlHistoryTransform();
        }
    }

    public class GemmaHistoryTransform : IHistoryTransform
    {
        public string HistoryToText(ChatHistory history)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var message in history.Messages)
            {
                // Gemma kennt offiziell nur "user" und "model"
                string role = message.AuthorRole == AuthorRole.User ? "user" : "model";

                // WICHTIG: Gemma 3 hat keinen echten System-Prompt. 
                // Wenn es einen System-Prompt gibt, wird er bei Gemma einfach als erster User-Prompt Prefix gesendet.
                if (message.AuthorRole == AuthorRole.System)
                {
                    role = "user";
                }

                // Gemma Format: <start_of_turn>user\nText<end_of_turn>\n
                sb.Append($"<start_of_turn>{role}\n{message.Content}<end_of_turn>\n");
            }

            // Trigger für das Modell, jetzt als "model" zu antworten
            sb.Append("<start_of_turn>model\n");
            return sb.ToString();
        }

        public ChatHistory TextToHistory(AuthorRole role, string text)
        {
            var history = new ChatHistory();

            // Bereinigen von Gemma Steuerzeichen, falls der Stream sie durchlässt
            string cleanText = text.Replace("<end_of_turn>", "")
                                   .Replace("<start_of_turn>", "")
                                   .Replace("model\n", "")
                                   .Trim();

            history.AddMessage(role, cleanText);

            return history;
        }

        public IHistoryTransform Clone()
        {
            return new GemmaHistoryTransform();
        }
    }
}