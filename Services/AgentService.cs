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

        // Read provider from appsettings — "Groq" or "Ollama"
        _provider = config["AI:Provider"] ?? "Groq";

        var builder = Kernel.CreateBuilder();

        if (_provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            // Local Ollama — no API key, runs on your machine
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

            Console.WriteLine($"[AI Provider] Ollama — {modelId} @ {endpoint}");
        }
        else
        {
            // Groq cloud — free API, fast responses
            var apiKey = config["AI:Groq:ApiKey"]
                          ?? throw new Exception(
                              "Groq API key missing in appsettings.");
            var modelId = config["AI:Groq:ModelId"]
                          ?? "llama3-8b-8192";

            builder.AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: apiKey,
                httpClient: new HttpClient
                {
                    BaseAddress = new Uri("https://api.groq.com/openai/v1/"),
                    Timeout = TimeSpan.FromSeconds(30)
                }
            );

            Console.WriteLine($"[AI Provider] Groq — {modelId}");
        }

        _kernel = builder.Build();
        LoadFilesIntoCache();
    }

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

    public async Task<AgentResponse> AskAsync(string question)
    {
        var fileNames = _fileCache.Keys.ToList();
        var relevantChunks = new List<string>();

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

            Respond in EXACTLY this format, no extra text:

            TOPIC: [one word: CSharp, DotNet, EF, SQL, ASPNET, HR, Python, or General]

            ANSWER:
            [Clear 4-6 line answer suitable for an interview]

            CODE:
            [Short C# code example if relevant, else write NONE]

            FOLLOWUP:
            1. [follow-up question the interviewer will likely ask]
            2. [another likely follow-up question]
            """;

        // Timeout: 120s for Ollama (slow/local), 30s for Groq (fast/cloud)
        var timeoutSeconds = _provider.Equals(
            "Ollama", StringComparison.OrdinalIgnoreCase) ? 120 : 30;

        using var cts = new CancellationTokenSource(
            TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var result = await _kernel.InvokePromptAsync(
                prompt, cancellationToken: cts.Token);
            var raw = result.ToString();

            var topic = ExtractSection(raw, "TOPIC:", "ANSWER:").Trim();
            var answer = ExtractSection(raw, "ANSWER:", "CODE:").Trim();
            var code = ExtractSection(raw, "CODE:", "FOLLOWUP:").Trim();
            var followup = ExtractSection(raw, "FOLLOWUP:", null).Trim();

            var fullAnswer = (code != "NONE" &&
                             !string.IsNullOrWhiteSpace(code))
                ? $"{answer}\n\nCode Example:\n{code}"
                : answer;

            var followUpList = followup
                .Split('\n')
                .Where(l => l.TrimStart().StartsWith("1.") ||
                            l.TrimStart().StartsWith("2."))
                .Select(l => l.Trim().TrimStart('1', '2', '.', ' '))
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();

            return new AgentResponse
            {
                Answer = fullAnswer,
                Sources = hasRelevant
                    ? relevantChunks
                        .Select(c => c.Split('\n')[0]
                            .Replace("=== SOURCE: ", "")
                            .Replace(" ===", ""))
                        .ToList()
                    : new List<string>(),
                Topic = topic,
                FollowUpQuestions = followUpList
            };
        }
        catch (OperationCanceledException)
        {
            return new AgentResponse
            {
                Answer = _provider.Equals("Ollama",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Ollama timed out. Your machine may not have enough free RAM. " +
                      "Try switching AI:Provider to Groq in appsettings.json."
                    : "Groq timed out. Check your API key or internet connection.",
                Sources = fileNames,
                Topic = "Error",
                FollowUpQuestions = new List<string>()
            };
        }
        catch (Exception ex)
        {
            return new AgentResponse
            {
                Answer = $"Error: {ex.Message}",
                Sources = fileNames,
                Topic = "Error",
                FollowUpQuestions = new List<string>()
            };
        }
    }

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
                Score = keywords.Count(k => p.ToLower().Contains(k))
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

    private string ReadDocx(string filePath)
    {
        try
        {
            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return "";

            return string.Join("\n\n",
                body.Descendants<Paragraph>()
                    .Select(p => p.InnerText.Trim())
                    .Where(t => t.Length > 0));
        }
        catch { return ""; }
    }

    private string ExtractSection(string text, string start, string? end)
    {
        var startIdx = text.IndexOf(start,
            StringComparison.OrdinalIgnoreCase);
        if (startIdx < 0) return "";
        startIdx += start.Length;

        if (end == null) return text.Substring(startIdx);

        var endIdx = text.IndexOf(end, startIdx,
            StringComparison.OrdinalIgnoreCase);
        return endIdx < 0
            ? text.Substring(startIdx)
            : text.Substring(startIdx, endIdx - startIdx);
    }
}