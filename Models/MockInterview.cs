namespace InterviewPrepAgent.Models
{
    public record MockInterviewRequest(string Topic);

    public record EvaluateRequest(string Question, string Answer);

    public class MockQuestion
    {
        public string Question { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string Topic { get; set; } = "";
    }

    public class EvaluationResult
    {
        public int Score { get; set; }
        public string Feedback { get; set; } = "";
        public List<string> MissingPoints { get; set; } = new();
        public string IdealAnswer { get; set; } = "";
        public string Verdict { get; set; } = "";
    }
}
