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
}