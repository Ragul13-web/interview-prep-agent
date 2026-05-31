using Microsoft.AspNetCore.Mvc;
using InterviewPrepAgent.Services;
using InterviewPrepAgent.Models;

namespace InterviewPrepAgent.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly AgentService _agent;

    public AgentController(AgentService agent)
    {
        _agent = agent;
    }

    // POST api/agent/ask
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { error = "Question cannot be empty." });

        try
        {
            var response = await _agent.AskAsync(request.Question);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // GET api/agent/files
    [HttpGet("files")]
    public IActionResult Files()
    {
        var folder = Path.Combine(
            Directory.GetCurrentDirectory(), "StudyFiles");

        var files = Directory.Exists(folder)
            ? Directory.GetFiles(folder)
                .Select(f => Path.GetFileName(f))
                .ToList()
            : new List<string>();

        return Ok(files);
    }
    // POST api/agent/mock-interview
    // Generate a random question for a topic
    [HttpPost("mock-interview")]
    public async Task<IActionResult> MockInterview(
        [FromBody] MockInterviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
            return BadRequest(new { error = "Topic cannot be empty." });

        try
        {
            var question = await _agent.GetMockQuestionAsync(request.Topic);
            return Ok(question);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // POST api/agent/evaluate
    // Submit your answer and get scored
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate(
        [FromBody] EvaluateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question) ||
            string.IsNullOrWhiteSpace(request.Answer))
            return BadRequest(new { error = "Question and Answer required." });

        try
        {
            var result = await _agent.EvaluateAnswerAsync(
                request.Question, request.Answer);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}