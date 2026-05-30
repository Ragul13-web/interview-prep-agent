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
                .Select(Path.GetFileName)
                .ToList()
            : new List<string>();

        return Ok(files);
    }
}