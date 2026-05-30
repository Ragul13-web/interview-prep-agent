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
        public string Answer { get; set; } = "";
        public List<string>? Sources { get; set; } = new();
        public string Topic { get; set; } = "";
        public List<string>? FollowUpQuestions { get; set; } = new();
    }

    public record AskRequest(string Question);
}
