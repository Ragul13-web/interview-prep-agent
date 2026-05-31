# 🤖 Interview Prep Agent

An AI-powered agent that searches across multiple study files and delivers consolidated, interview-ready answers — built with **ASP.NET Core**, **Semantic Kernel**, and your choice of **Groq (cloud)** or **Ollama (local)** AI provider.

> Built as a hands-on project to demonstrate AI agent development using Microsoft's Semantic Kernel framework — the same technology stack used in Azure AI Foundry agent development.

---

## 📌 Overview

As a .NET developer preparing for interviews at top companies like TCS, Cognizant, Accenture, HCLTech, LTIMindtree, EY, PwC, Deloitte, and BNY Mellon, I built this agent to:

- Search across multiple interview study documents simultaneously
- Return structured, interview-ready answers with code examples
- Suggest follow-up questions the interviewer is likely to ask
- Generate personalized day-by-day study plans for any topic
- Simulate system design interviews with full architectural guidance
- Run mock interviews and evaluate answers with scoring
- Block irrelevant questions using a profile-aware topic guard
- Support two AI providers — switch with a single config change
- Run fully locally (Ollama) or via free cloud API (Groq)

---

## 🏗️ Architecture

```
User Question (Swagger / API Client)
        │
        ▼
AgentController (ASP.NET Core Web API)
        │
        ▼
AgentService (Semantic Kernel Orchestration)
        │
        ├──► Topic Guard (IsRelevantQuestionAsync)
        │         │
        │         └── Blocks irrelevant topics
        │             (Java, JS, weather, sports etc.)
        │
        ├──► Smart Paragraph Extractor
        │         │
        │         ├── HR_Interview_QA.docx
        │         ├── MVCInterviewQuestions.docx
        │         └── TechnicalInterview_Complete.docx
        │
        ▼
Semantic Kernel
        │
        ├── Provider: "Groq"   → Groq Cloud API (fast, free)
        └── Provider: "Ollama" → Local LLaMA 3 (private, offline)
        │
        ▼
LLM returns pure JSON
        │
        ├── StripCodeFences() cleans markdown wrapping
        ├── JsonSerializer deserializes into typed model
        └── Graceful fallback on parse failure
        │
        ▼
Structured Response returned to caller
```

---

## 🔄 How the Agent Works — Flow Diagram

```
┌─────────────────────────────────────────────┐
│           User sends question                │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│      Topic Guard validates relevance         │
│      Blocks: Java, JS, non-tech topics       │
│      Allows: C#, .NET, SQL, Azure, Python    │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│           AgentService                       │
│  1. Score paragraphs by keyword relevance    │
│  2. Extract top 1500 chars per file          │
│  3. Build structured JSON prompt             │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│         Semantic Kernel + LLM                │
│   Provider = "Groq"   → Groq Cloud API       │
│   Provider = "Ollama" → Local LLaMA 3        │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│  AgentResponse { topic, answer,              │
│  codeExample, sources, followUpQuestions }   │
└─────────────────────────────────────────────┘
```

---

## 🚀 Enhanced Features

### 🧠 AI Agent Capabilities

Unlike a traditional chatbot, the Interview Prep Agent follows an agentic workflow:

1. Understands the user's interview question
2. Validates relevance against the candidate's profile (Topic Guard)
3. Identifies and extracts relevant study materials
4. Ranks contextual information by keyword relevance score
5. Uses Semantic Kernel orchestration to reason over retrieved content
6. Generates structured interview-ready answers in pure JSON
7. Produces likely follow-up questions and coding examples

This mimics how enterprise AI agents perform retrieval, reasoning, and response generation.

---

### 🔧 Semantic Kernel Tool Calling

The agent architecture supports tool invocation through Semantic Kernel Plugins.

| Tool | Purpose |
|---|---|
| DocumentSearchTool | Locate relevant interview content across study files |
| FileDiscoveryTool | List available study materials |
| MockInterviewTool | Generate topic-specific interview questions |
| AnswerEvaluationTool | Score candidate responses out of 10 |

Future versions will allow the agent to dynamically decide which tool to invoke based on user intent — true agentic behavior.

---

### 🎯 Interview Coach Mode

The agent supports a simulated interviewer workflow:

```
Candidate → Agent → Evaluation Engine
```

Features:
- Generate topic-specific interview questions
- Evaluate candidate answers out of 10
- Identify missing concepts
- Suggest ideal answer
- Recommend improvement areas

**Example:**

Topic: ASP.NET Core

Question: Explain Dependency Injection and service lifetimes.

```json
{
  "score": 8,
  "feedback": "Good understanding of DI fundamentals.",
  "missingPoints": [
    "Scoped lifetime behavior per HTTP request",
    "Circular dependency considerations"
  ],
  "idealAnswer": "...",
  "verdict": "Strong"
}
```

---

### 📅 Study Plan Generator

Generate a personalized day-by-day study plan for any topic in your profile.

**Request:**
```json
{
  "topic": "ASP.NET Core",
  "days": 7
}
```

**Response:**
```json
{
  "overallTopic": "ASP.NET Core",
  "totalDays": 7,
  "days": [
    {
      "day": 1,
      "topic": "Middleware Pipeline",
      "focus": "Understanding request/response pipeline",
      "keyConcepts": ["UseMiddleware", "Run vs Use vs Map", "Order matters"],
      "practiceQuestion": "What happens if you call next() twice in middleware?"
    },
    {
      "day": 2,
      "topic": "Dependency Injection",
      "focus": "Service lifetimes and DI container",
      "keyConcepts": ["AddScoped", "AddTransient", "AddSingleton"],
      "practiceQuestion": "When would you use AddSingleton over AddScoped?"
    }
  ],
  "tip": "Build a small project for each concept to reinforce learning"
}
```

---

### 🏛️ System Design Interview Mode

Get complete architectural guidance for system design interview questions.

**Request:**
```json
{
  "problem": "Design an E-Commerce Platform"
}
```

**Response includes:**
- Functional and non-functional requirements
- System components breakdown
- Database design approach
- Scaling strategy
- Caching approach
- Microservices breakdown
- Trade-offs to discuss
- Likely follow-up questions from the interviewer

---

### 📚 Multi-Document Knowledge Aggregation

A single question may exist across multiple study sources. The agent:
- Searches all documents simultaneously on every request
- Scores each paragraph by keyword match count
- Extracts only the top-scoring paragraphs (max 1500 chars per file)
- Consolidates into a single unified response
- Cites which source files contributed to the answer

---

### 🏷️ Intelligent Topic Classification

Every answer is automatically categorized into one of:
`CSharp` | `DotNet` | `ASPNET` | `EF` | `SQL` | `Azure` | `Python` | `HR` | `General`

Enables future topic-wise analytics and progress tracking.

---

### 🛡️ Profile-Specific Topic Guard

Every question is validated against the candidate's actual experience profile before processing:

**Allowed topics:**
C#, .NET, ASP.NET Core, Entity Framework, ADO.NET, SQL Server, Azure, Python, Design Patterns, Data Structures, HR/Behavioral, REST API, Microservices, Git

**Blocked topics:**
Java, Spring Boot, JavaScript, React, Angular, Node.js, PHP, non-technical topics (weather, sports, food etc.)

If blocked, returns a clear message instead of wasting an API call.

---

### 📊 Interview Readiness Analytics (Planned)

Future versions will track:
- Questions attempted per topic
- Average score per topic
- Strong vs weak areas
- Recommended study areas

---

### 🤖 Future Multi-Agent Architecture

Planned architecture:
```
              Orchestrator Agent
                      │
  ┌──────────┬─────────┼──────────┬──────────┐
  ▼          ▼         ▼          ▼          ▼
C# Agent  ASPNET    SQL Agent  Python    HR Agent
          Agent               Agent
```

Each specialized agent maintains its own knowledge domain while the Orchestrator combines results into a single response. This mirrors enterprise multi-agent systems used in Azure AI Foundry and modern agentic AI platforms.

---

## 🛠️ Tech Stack

| Layer | Technology | Purpose |
|---|---|---|
| Backend Framework | ASP.NET Core Web API (.NET 9) | REST API host |
| AI Orchestration | Microsoft Semantic Kernel 1.30.0 | Agent coordination |
| AI Provider (Cloud) | Groq API — LLaMA 3.3 70B Versatile | Fast free cloud inference |
| AI Provider (Local) | Ollama — LLaMA 3 4.7B | Fully offline local inference |
| Document Reading | DocumentFormat.OpenXml 3.0.2 | Read .docx study files |
| API Documentation | Swashbuckle / Swagger 6.9.0 | API testing UI |
| Language | C# 13 / .NET 9 | Primary language |

---

## 💡 Key Design Decisions

**Why support both Groq and Ollama?**
Different machines have different capabilities. On a machine with limited RAM or no GPU, Ollama times out. Groq provides the same LLaMA 3 model via a free cloud API with 2–3 second responses. Switching between them requires changing one line in appsettings.json — no code change needed. This is the strategy pattern applied to AI provider selection.

**Why Semantic Kernel over direct API calls?**
Semantic Kernel is Microsoft's official AI orchestration SDK for .NET — the same framework powering Azure AI Foundry. Using it means the provider swap (Ollama ↔ Groq ↔ Azure OpenAI) is handled by the framework, not custom code. It demonstrates enterprise-grade AI development patterns relevant to Microsoft-stack companies.

**Why smart paragraph extraction instead of full file content?**
Study files can be 200KB+ (100,000+ characters). Sending full files to an AI model overloads the context window and causes timeouts. The agent scores each paragraph by keyword relevance and sends only the top-scoring paragraphs (max 1500 chars per file), making responses faster and more accurate.

**Why structured JSON output instead of free-form text?**
Unstructured AI responses are fragile to parse. Instructing the LLM to return pure JSON matching the AgentResponse model, combined with StripCodeFences() and a JsonException fallback, makes the API contract reliable and predictable regardless of LLM behavior.

**Why a profile-specific topic guard?**
Without a guard, the agent answers anything — Java, Spring Boot, weather. The guard validates every question against the candidate's actual skill profile before processing, preventing irrelevant API calls and keeping the tool focused on genuine interview preparation.

**Why ASP.NET Core Web API instead of a console app?**
A REST API makes the agent reusable across any frontend and mirrors real-world enterprise architecture. It also demonstrates controller/service separation, DI, and configuration management patterns that interviewers directly ask about.

---

## 🔀 Switching AI Provider

Just change one line in `appsettings.json`:

```json
"AI": {
  "Provider": "Groq"     <- fast cloud, free API key required
  "Provider": "Ollama"   <- fully local, no internet, needs 8GB+ free RAM
}
```

No code changes needed. The AgentService constructor reads this config and initializes the correct Semantic Kernel connector automatically.

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Git](https://git-scm.com/download/win)
- Visual Studio 2022 or VS Code
- For Groq: Free API key from [console.groq.com](https://console.groq.com)
- For Ollama: [Ollama](https://ollama.com/download) installed + `ollama pull llama3`

### 1. Clone the Repository

```bash
git clone https://github.com/Ragul13-web/interview-prep-agent.git
cd interview-prep-agent
```

### 2. Add Your Study Files

Copy your interview preparation documents (.docx, .txt, .md) into:

```
InterviewPrepAgent/StudyFiles/
├── HR_Interview_QA.docx
├── MVCInterviewQuestions.docx
└── TechnicalInterview_Complete.docx
```

### 3. Configure AI Provider

Open `appsettings.json` and set your preferred provider:

**Option A — Groq (Recommended: fast, free)**
```json
"AI": {
  "Provider": "Groq",
  "Groq": {
    "ApiKey": "your_groq_api_key_here",
    "ModelId": "llama-3.3-70b-versatile"
  }
}
```

**Option B — Ollama (Local, private)**
```json
"AI": {
  "Provider": "Ollama",
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "ModelId": "llama3"
  }
}
```

### 4. Run the Project

```bash
dotnet run
```

Or press **F5** in Visual Studio 2022.

Swagger UI opens at:
```
http://localhost:5000
```

---

## 📡 API Endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| POST | /api/agent/ask | Ask any interview question |
| GET | /api/agent/files | List all loaded study files |
| POST | /api/agent/mock-interview | Generate a practice question by topic |
| POST | /api/agent/evaluate | Submit answer and get scored out of 10 |
| POST | /api/agent/study-plan | Generate N-day personalized study plan |
| POST | /api/agent/system-design | Full system design interview guidance |

### POST /api/agent/ask

**Request:**
```json
{
  "question": "What is dependency injection in ASP.NET Core?"
}
```

**Response:**
```json
{
  "topic": "ASPNET",
  "answer": "Dependency Injection is a design pattern where dependencies are provided to a class rather than created inside it. ASP.NET Core has a built-in DI container configured in Program.cs. Services are registered with Scoped, Transient, or Singleton lifetimes and injected via constructors.",
  "codeExample": "builder.Services.AddScoped<IMyService, MyService>();",
  "sources": ["MVCInterviewQuestions.docx", "TechnicalInterview_Complete.docx"],
  "followUpQuestions": [
    "What is the difference between AddScoped, AddTransient, and AddSingleton?",
    "How does ASP.NET Core resolve circular dependencies?"
  ]
}
```

### POST /api/agent/mock-interview

**Request:**
```json
{ "topic": "Entity Framework" }
```

**Response:**
```json
{
  "question": "What is the difference between eager loading and lazy loading in EF Core?",
  "difficulty": "Intermediate",
  "topic": "Entity Framework"
}
```

### POST /api/agent/evaluate

**Request:**
```json
{
  "question": "What is dependency injection?",
  "answer": "It is a pattern where dependencies are passed into a class."
}
```

**Response:**
```json
{
  "score": 6,
  "feedback": "Basic understanding present but lacks depth.",
  "missingPoints": ["Service lifetimes", "Built-in DI container in ASP.NET Core"],
  "idealAnswer": "...",
  "verdict": "Needs Improvement"
}
```

### POST /api/agent/study-plan

**Request:**
```json
{ "topic": "ASP.NET Core", "days": 7 }
```

### POST /api/agent/system-design

**Request:**
```json
{ "problem": "Design a URL shortener like bit.ly" }
```

---

## 🗂️ Project Structure

```
InterviewPrepAgent/
├── Controllers/
│   └── AgentController.cs        # All 6 API endpoints
├── Models/
│   ├── Question.cs               # AgentResponse + AskRequest
│   └── MockInterview.cs          # MockQuestion, EvaluationResult,
│                                 # StudyPlan, SystemDesignResponse
├── Plugins/
│   └── StudyFilePlugin.cs        # Semantic Kernel tool calling plugin
├── Services/
│   └── AgentService.cs           # Core agent logic:
│                                 #   - Provider selection (Groq/Ollama)
│                                 #   - Topic guard (profile-aware)
│                                 #   - .docx reading (OpenXml)
│                                 #   - Smart paragraph extraction
│                                 #   - Semantic Kernel orchestration
│                                 #   - Structured JSON prompt engineering
│                                 #   - Mock interview + evaluation
│                                 #   - Study plan generation
│                                 #   - System design mode
├── StudyFiles/                   # Drop your .docx study files here
├── Properties/
│   └── launchSettings.json
├── appsettings.json              # AI provider config (switch here)
├── appsettings.Development.json  # Local dev config (gitignored)
└── Program.cs                    # DI registration + app pipeline
```

---

## 📈 What This Project Demonstrates

| Concept | Implementation |
|---|---|
| AI Agent Development | Semantic Kernel orchestrating LLM with structured prompts |
| Strategy Pattern | AI provider selected at runtime via configuration |
| Dependency Injection | AgentService injected with IConfiguration via DI container |
| ASP.NET Core Web API | Clean controller/service separation, RESTful endpoints |
| Prompt Engineering | Structured JSON output, profile-aware topic guard |
| Document Processing | OpenXml reading .docx, smart paragraph relevance scoring |
| Configuration Management | appsettings.json with environment-specific overrides |
| Error Handling | JSON parse fallback, timeout handling, graceful degradation |
| Git Workflow | Feature branches, pull requests, meaningful commit messages |
| Security Awareness | API key protection via .gitignore, GitHub secret scanning |

---

## 🔮 Future Improvements

- Add vector search (Qdrant) for semantic similarity matching
- Add JWT authentication to secure the API
- Support PDF files using PdfPig
- Add a Blazor or React frontend
- Switch to Azure OpenAI for enterprise deployment
- Add multi-turn conversation memory using Semantic Kernel ChatHistory
- Interview readiness analytics dashboard
- Multi-agent architecture with specialized agents per topic

---

## 👨‍💻 Author

**Ragul** — .NET Developer, 5 years experience
Microsoft AI Agents Specialization — Coursera (Completed May 30, 2026)

Courses completed:
- AI Agent Fundamentals with Azure AI Foundry
- Building Intelligent Agent Workflows
- Code and Framework Based Agent Development
- Building Multi-Agent Systems

---

## 📄 License

MIT License — free to use and modify.