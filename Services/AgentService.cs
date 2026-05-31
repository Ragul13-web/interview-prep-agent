using Microsoft.SemanticKernel;
using InterviewPrepAgent.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace InterviewPrepAgent.Services;

public class AgentService
{
    private readonly Kernel _kernel;
    private readonly string _filesFolder;
    private readonly string _provider;
    private readonly Dictionary<string, string> _fileCache = new();

    public AgentService(IConfiguration config)
    {
        _filesFolder = Path.Combine(
            Directory.GetCurrentDirectory(), "StudyFiles");
        Directory.CreateDirectory(_filesFolder);

        _provider = config["AI:Provider"] ?? "Groq";

        var builder = Kernel.CreateBuilder();

        if (_provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            var endpoint = config["AI:Ollama:Endpoint"]
                           ?? "http://localhost:11434";
            var modelId = config["AI:Ollama:ModelId"]
                           ?? "llama3";

#pragma warning disable SKEXP0070
            builder.AddOllamaChatCompletion(
                modelId: modelId,
                endpoint: new Uri(endpoint)
            );
#pragma warning restore SKEXP0070

            Console.WriteLine(
                $"[AI Provider] Ollama — {modelId} @ {endpoint}");
        }
        else
        {
            var apiKey = config["AI:Groq:ApiKey"]
                          ?? throw new Exception(
                              "Groq API key missing in appsettings.");
            var modelId = config["AI:Groq:ModelId"]
                          ?? "llama-3.3-70b-versatile";

            builder.AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: apiKey,
                httpClient: new HttpClient
                {
                    BaseAddress = new Uri(
                        "https://api.groq.com/openai/v1/"),
                    Timeout = TimeSpan.FromSeconds(30)
                }
            );

            Console.WriteLine($"[AI Provider] Groq — {modelId}");
        }

        _kernel = builder.Build();
        LoadFilesIntoCache();
    }

    // ─── File Loading ────────────────────────────────────────────

    private void LoadFilesIntoCache()
    {
        foreach (var file in Directory.GetFiles(_filesFolder, "*.*"))
        {
            var name = Path.GetFileName(file);
            var ext = Path.GetExtension(file).ToLower();

            if (ext == ".docx")
                _fileCache[name] = ReadDocx(file);
            else if (ext == ".txt" || ext == ".md")
                _fileCache[name] = File.ReadAllText(file);
        }

        Console.WriteLine(
            $"[Files Loaded] {_fileCache.Count} file(s): " +
            string.Join(", ", _fileCache.Keys));
    }

    // ─── Main Agent Method ───────────────────────────────────────

    public async Task<AgentResponse> AskAsync(string question)
    {
        var isRelevant = await IsRelevantQuestionAsync(question);

        if (!isRelevant)
        {
            return new AgentResponse
            {
                Topic = "OutOfScope",
                Answer = "This topic is outside my scope. I'm configured for " +
                 "your specific profile: C#, .NET, ASP.NET Core, " +
                 "Entity Framework, SQL Server, Azure, and Python. " +
                 "Please ask questions relevant to your experience.",
                CodeExample = "",
                Sources = new List<string>(),
                FollowUpQuestions = new List<string>()
            };
        }

        var fileNames = _fileCache.Keys.ToList();
        var relevantChunks = new List<string>();

        // Step 1 — Find relevant paragraphs from each file
        foreach (var (fileName, fullContent) in _fileCache)
        {
            var relevant = ExtractRelevantParagraphs(
                fullContent, question, maxChars: 1500);

            if (!string.IsNullOrWhiteSpace(relevant))
                relevantChunks.Add(
                    $"=== SOURCE: {fileName} ===\n{relevant}");
        }

        var hasRelevant = relevantChunks.Any();
        var context = hasRelevant
            ? string.Join("\n\n", relevantChunks)
            : "No matching content in study files.";

        // Step 2 — Build prompt instructing LLM to return pure JSON
        var jsonSchema = @"{
          ""topic"": ""one word: CSharp | DotNet | EF | SQL | ASPNET | HR | Python | General"",
          ""answer"": ""clear 4-6 line interview answer here"",
          ""codeExample"": ""short C# code example if relevant, empty string if not applicable"",
          ""followUpQuestions"": [
            ""first follow-up question the interviewer will likely ask"",
            ""second follow-up question the interviewer will likely ask""
          ]
        }";

        var prompt = $"""
        You are an expert .NET interview coach.
        A developer with 5 years of C# and .NET experience
        is preparing for interviews at companies like TCS,
        Cognizant, Accenture, HCLTech, LTIMindtree,
        EY, PwC, Deloitte, and BNY Mellon.

        {(hasRelevant
                    ? "Relevant content from study files:"
                    : "No matching content found. Answer from your expert .NET knowledge.")}

        {context}

        Interview Question: {question}

        You MUST respond with ONLY a valid JSON object.
        No explanation, no markdown, no code fences.
        Exactly this structure:

        {jsonSchema}
        """;
        // Step 3 — Set timeout based on provider
        var timeoutSeconds = _provider.Equals(
            "Ollama", StringComparison.OrdinalIgnoreCase) ? 120 : 30;

        using var cts = new CancellationTokenSource(
            TimeSpan.FromSeconds(timeoutSeconds));

        // Step 4 — Call the AI
        FunctionResult? result = null;

        try
        {
            result = await _kernel.InvokePromptAsync(
                prompt, cancellationToken: cts.Token);

            var raw = result.ToString().Trim();

            // Step 5 — Strip markdown fences if LLM adds them anyway
            raw = StripCodeFences(raw);

            Console.WriteLine($"[Raw AI Response] {raw}");

            // Step 6 — Deserialize JSON directly into AgentResponse
            var response = System.Text.Json.JsonSerializer
                .Deserialize<AgentResponse>(raw,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (response == null)
                throw new Exception("Deserialization returned null.");

            // Step 7 — Attach matched source filenames
            response.Sources = hasRelevant
                ? relevantChunks
                    .Select(c => c.Split('\n')[0]
                        .Replace("=== SOURCE: ", "")
                        .Replace(" ===", ""))
                    .ToList()
                : new List<string>();

            return response;
        }
        catch (OperationCanceledException)
        {
            return new AgentResponse
            {
                Topic = "Error",
                Answer = _provider.Equals("Ollama",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Ollama timed out. Switch AI:Provider " +
                      "to Groq in appsettings.json."
                    : "Groq timed out. Check your API key " +
                      "or internet connection.",
                CodeExample = "",
                Sources = fileNames,
                FollowUpQuestions = new List<string>()
            };
        }
        catch (System.Text.Json.JsonException ex)
        {
            // Fallback — LLM didn't return valid JSON
            // Return raw text as the answer instead of crashing
            Console.WriteLine($"[JSON Parse Error] {ex.Message}");

            return new AgentResponse
            {
                Topic = "General",
                Answer = result?.ToString()
                              ?? "Could not parse AI response.",
                CodeExample = "",
                Sources = hasRelevant
                    ? relevantChunks
                        .Select(c => c.Split('\n')[0]
                            .Replace("=== SOURCE: ", "")
                            .Replace(" ===", ""))
                        .ToList()
                    : new List<string>(),
                FollowUpQuestions = new List<string>()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] {ex.Message}");

            return new AgentResponse
            {
                Topic = "Error",
                Answer = $"Error: {ex.Message}",
                CodeExample = "",
                Sources = fileNames,
                FollowUpQuestions = new List<string>()
            };
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────

    // Scores paragraphs by keyword relevance and returns top matches
    private string ExtractRelevantParagraphs(
        string content, string question, int maxChars)
    {
        var paragraphs = content
            .Split(new[] { "\n\n", "\r\n\r\n" },
                StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Trim().Length > 20)
            .ToList();

        var keywords = question
            .ToLower()
            .Split(new[] { ' ', '?', '.', ',', '-' },
                StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .ToHashSet();

        var scored = paragraphs
            .Select(p => new
            {
                Text = p,
                Score = keywords.Count(k =>
                    p.ToLower().Contains(k))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ToList();

        var result = new System.Text.StringBuilder();
        foreach (var item in scored)
        {
            if (result.Length + item.Text.Length > maxChars)
                break;
            result.AppendLine(item.Text);
        }

        return result.ToString();
    }

    // Reads all paragraph text from a .docx file using OpenXml
    private string ReadDocx(string filePath)
    {
        try
        {
            using var doc = WordprocessingDocument
                .Open(filePath, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return "";

            return string.Join("\n\n",
                body.Descendants<Paragraph>()
                    .Select(p => p.InnerText.Trim())
                    .Where(t => t.Length > 0));
        }
        catch { return ""; }
    }

    // Removes ```json ... ``` or ``` ... ``` that LLMs sometimes add
    private string StripCodeFences(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Remove opening fence and language tag (```json or ```)
        if (text.StartsWith("```"))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline > 0)
                text = text.Substring(firstNewline + 1);
        }

        // Remove closing fence
        if (text.EndsWith("```"))
            text = text
                .Substring(0, text.LastIndexOf("```"))
                .Trim();

        return text.Trim();
    }
    // Generates a random interview question for a given topic
    public async Task<MockQuestion> GetMockQuestionAsync(string topic)
    {
        var isRelevant = await IsRelevantQuestionAsync(topic);

        if (!isRelevant)
        {
            return new MockQuestion
            {
                Question = "This topic is outside your profile. Please use topics " +
                   "like CSharp, DotNet, ASPNET, EntityFramework, " +
                   "SQL, Azure, Python, DesignPatterns, or HR.",
                Difficulty = "N/A",
                Topic = "OutOfScope"
            };
        }
        var mockSchema = @"{""question"": ""your question here"", ""difficulty"": ""Easy | Intermediate | Hard"", ""topic"": """ + topic + @"""}";

        var prompt = $"""
        You are an interviewer at a top IT company interviewing
        a .NET developer with 5 years of experience.

        Generate ONE interview question about: {topic}

        Respond with ONLY valid JSON, no markdown, no explanation:

        {mockSchema}
        """;

        try
        {
            var result = await _kernel.InvokePromptAsync(prompt);
            var raw = StripCodeFences(result.ToString().Trim());

            var question = System.Text.Json.JsonSerializer
                .Deserialize<MockQuestion>(raw,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            return question ?? new MockQuestion
            {
                Question = "What is dependency injection in ASP.NET Core?",
                Difficulty = "Intermediate",
                Topic = topic
            };
        }
        catch
        {
            return new MockQuestion
            {
                Question = "What is dependency injection in ASP.NET Core?",
                Difficulty = "Intermediate",
                Topic = topic
            };
        }
    }

    // Evaluates the candidate's answer and scores it
    public async Task<EvaluationResult> EvaluateAnswerAsync(
        string question, string userAnswer)
    {
        var jsonSchema = @"{
        ""score"": 8,
        ""feedback"": ""overall feedback on the answer"",
        ""missingPoints"": [""point 1 that was missing"", ""point 2""],
        ""idealAnswer"": ""what a perfect answer would include"",
        ""verdict"": ""Strong | Good | Needs Improvement""
    }";

        var prompt = $"""
        You are a strict but fair technical interviewer at a top IT company.
        Evaluate this candidate's answer out of 10.

        Question: {question}

        Candidate's Answer: {userAnswer}

        Respond with ONLY valid JSON, no markdown, no explanation:

        {jsonSchema}
        """;

        try
        {
            var result = await _kernel.InvokePromptAsync(prompt);
            var raw = StripCodeFences(result.ToString().Trim());

            var evaluation = System.Text.Json.JsonSerializer
                .Deserialize<EvaluationResult>(raw,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            return evaluation ?? new EvaluationResult
            {
                Score = 0,
                Feedback = "Could not evaluate answer.",
                Verdict = "Needs Improvement"
            };
        }
        catch
        {
            return new EvaluationResult
            {
                Score = 0,
                Feedback = "Could not evaluate answer. Please try again.",
                Verdict = "Needs Improvement"
            };
        }
    }
    // Validates if the question is relevant to .NET interviews
    private async Task<bool> IsRelevantQuestionAsync(string question)
    {
        var prompt = $"""
        You are a gatekeeper for a personalized interview preparation 
        tool built for a specific candidate profile:

        Candidate Profile:
        - 5 years of C# and .NET development experience
        - Skills: ASP.NET Core, Web API, MVC, Entity Framework, 
          ADO.NET, SQL Server, Azure basics
        - 1 year of Python experience
        - Preparing for interviews at Indian IT companies

        ALLOWED topics (return RELEVANT):
        - C#, .NET, .NET Core, .NET Framework
        - ASP.NET Core, Web API, MVC, Razor Pages
        - Entity Framework Core, ADO.NET, LINQ
        - SQL Server, database design, query optimization
        - Azure services (App Service, Functions, SQL, Blob)
        - Python basics, OOP in Python, common libraries
        - Software design patterns (SOLID, Repository, Factory etc.)
        - Data structures and algorithms in C# or Python
        - HR and behavioral interview questions
        - General software engineering concepts
        - Git, version control basics
        - Microservices, REST API design

        NOT ALLOWED topics (return IRRELEVANT):
        - Java, Spring Boot, Hibernate, Maven
        - JavaScript, React, Angular, Vue, Node.js
        - PHP, Ruby, Go, Rust, Kotlin, Swift
        - DevOps, Kubernetes, Docker (unless basic)
        - Non-technical topics (weather, sports, food, news)
        - Any framework or language NOT in the candidate profile

        Input: {question}

        Respond with ONLY one word:
        RELEVANT
        IRRELEVANT
        """;

        try
        {
            var result = await _kernel.InvokePromptAsync(prompt);
            var response = result.ToString().Trim().ToUpper();
            Console.WriteLine($"[Topic Guard] {response}");
            return response.Trim() switch
            {
                "RELEVANT" => true,
                "IRRELEVANT" => false,
                _ => true // unknown response — allow through by default
            };
        }
        catch
        {
            return true;
        }
    }
}