using Microsoft.SemanticKernel;
using System.ComponentModel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace InterviewPrepAgent.Plugins;

public class StudyFilePlugin
{
    private readonly Dictionary<string, string> _fileCache;

    public StudyFilePlugin(Dictionary<string, string> fileCache)
    {
        _fileCache = fileCache;
    }

    [KernelFunction("search_study_files")]
    [Description("Searches across all study files to find relevant content for an interview question. Call this when you need to find information about a specific topic.")]
    public string SearchStudyFiles(
        [Description("The topic or question to search for")]
        string query)
    {
        if (!_fileCache.Any())
            return "No study files loaded.";

        var keywords = query
            .ToLower()
            .Split(new[] { ' ', '?', '.', ',', '-' },
                StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .ToHashSet();

        var results = new List<string>();

        foreach (var (fileName, content) in _fileCache)
        {
            var paragraphs = content
                .Split(new[] { "\n\n", "\r\n\r\n" },
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Trim().Length > 20);

            var relevant = paragraphs
                .Select(p => new
                {
                    Text = p,
                    Score = keywords.Count(k =>
                        p.ToLower().Contains(k))
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(3)
                .Select(x => x.Text);

            var joined = string.Join("\n", relevant);
            if (!string.IsNullOrWhiteSpace(joined))
                results.Add($"[{fileName}]\n{joined}");
        }

        return results.Any()
            ? string.Join("\n\n", results)
            : "No relevant content found in study files.";
    }

    [KernelFunction("list_study_files")]
    [Description("Returns the list of all available study files loaded in the agent.")]
    public string ListStudyFiles()
    {
        return _fileCache.Any()
            ? string.Join(", ", _fileCache.Keys)
            : "No study files loaded.";
    }
}