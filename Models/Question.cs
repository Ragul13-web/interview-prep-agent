using System.Text.Json.Serialization;

namespace InterviewPrepAgent.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = "";
        public string Answer { get; set; } = "";
        public string Sources { get; set; } = "";
        public string Topic { get; set; } = "";
        public DateTime AskedAt { get; set; } = DateTime.Now;
    }
    public class AgentResponse
    {
        [JsonPropertyName("topic")]
        public string Topic { get; set; } = "";

        [JsonPropertyName("answer")]
        public string Answer { get; set; } = "";

        [JsonPropertyName("codeExample")]
        public string CodeExample { get; set; } = "";

        [JsonPropertyName("sources")]
        public List<string> Sources { get; set; } = new();

        [JsonPropertyName("followUpQuestions")]
        public List<string> FollowUpQuestions { get; set; } = new();
    }

    public record AskRequest(string Question);
}
