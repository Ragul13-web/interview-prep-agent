using InterviewPrepAgent.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<AgentService>(sp =>
    new AgentService(
        sp.GetRequiredService<IConfiguration>()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Interview Prep Agent",
        Version = "v1",
        Description = "AI Agent — Semantic Kernel + Groq/Ollama + LLaMA3"
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json",
        "Interview Prep Agent v1");
    c.RoutePrefix = string.Empty;
});

app.UseAuthorization();
app.MapControllers();
app.Run();