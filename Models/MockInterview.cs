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
    // Study Plan Generator
    public record StudyPlanRequest(string Topic, int Days);

    public class StudyPlanDay
    {
        public int Day { get; set; }
        public string Topic { get; set; } = "";
        public string Focus { get; set; } = "";
        public List<string> KeyConcepts { get; set; } = new();
        public string PracticeQuestion { get; set; } = "";
    }

    public class StudyPlan
    {
        public string OverallTopic { get; set; } = "";
        public int TotalDays { get; set; }
        public List<StudyPlanDay> Days { get; set; } = new();
        public string Tip { get; set; } = "";
    }

    // System Design
    public record SystemDesignRequest(string Problem);

    public class SystemDesignResponse
    {
        public string Problem { get; set; } = "";
        public List<string> Requirements { get; set; } = new();
        public List<string> Components { get; set; } = new();
        public string DatabaseDesign { get; set; } = "";
        public string ScalingStrategy { get; set; } = "";
        public string CachingStrategy { get; set; } = "";
        public List<string> Microservices { get; set; } = new();
        public List<string> TradeOffs { get; set; } = new();
        public List<string> FollowUpQuestions { get; set; } = new();
    }
}
