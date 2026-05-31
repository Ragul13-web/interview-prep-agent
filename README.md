# 🤖 Interview Prep Agent

An AI-powered agent that searches across multiple study files and delivers consolidated, interview-ready answers — built with **ASP.NET Core**, **Semantic Kernel**, and your choice of **Groq (cloud)** or **Ollama (local)** AI provider.

> Built as a hands-on project to demonstrate AI agent development using Microsoft's Semantic Kernel framework — the same technology stack used in Azure AI Foundry agent development.

---

## 📌 Overview

As a .NET developer preparing for interviews at top companies like TCS, Cognizant, Accenture, HCLTech, LTIMindtree, EY, PwC, Deloitte, and BNY Mellon, I built this agent to:

- Search across multiple interview study documents simultaneously
- Return structured, interview-ready answers with code examples
- Suggest follow-up questions the interviewer is likely to ask
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
Structured Response
(Answer + Topic + Code Example + Follow-up Questions)
```

---

## 🔄 How the Agent Works — Flow Diagram

┌─────────────────────────────────────────────┐
│              User sends question             │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│         AgentController receives request     │
│         POST /api/agent/ask                  │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│              AgentService                    │
│                                              │
│  1. Score paragraphs by keyword relevance    │
│  2. Extract top 1500 chars per file          │
│  3. Build structured prompt with context     │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│           Semantic Kernel                    │
│                                              │
│   Provider = "Groq"  → Groq Cloud API        │
│   Provider = "Ollama"→ Local LLaMA 3         │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│         LLM returns pure JSON                │
│         StripCodeFences() cleans it          │
│         JsonSerializer deserializes it       │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│  AgentResponse returned to caller            │
│  { topic, answer, codeExample,               │
│    sources, followUpQuestions }              │
└─────────────────────────────────────────────┘

## 🎯 Mock Interview Flow

POST /api/agent/mock-interview  →  Agent generates question
          ↓
User answers the question
          ↓
POST /api/agent/evaluate  →  Agent scores answer out of 10
          ↓
Returns { score, feedback, missingPoints, idealAnswer, verdict }

## 🛠️ Tech Stack

| Layer | Technology | Purpose |
|---|---|---|
| Backend Framework | ASP.NET Core Web API (.NET 9) | REST API host |
| AI Orchestration | Microsoft Semantic Kernel 1.30.0 | Agent coordination |
| AI Provider (Cloud) | Groq API — LLaMA 3 8B | Fast free cloud inference |
| AI Provider (Local) | Ollama — LLaMA 3 4.7B | Fully offline local inference |
| Document Reading | DocumentFormat.OpenXml 3.0.2 | Read .docx study files |
| API Documentation | Swashbuckle / Swagger 6.9.0 | API testing UI |
| Language | C# 13 / .NET 9 | Primary language |

---

## 💡 Key Design Decisions

**Why support both Groq and Ollama?**
Different machines have different capabilities. On a machine with limited RAM or no GPU, Ollama times out. Groq provides the same LLaMA 3 model via a free cloud API with 2–3 second responses. Switching between them requires changing one line in appsettings.json — no code change needed. This is the strategy pattern applied to AI provider selection.

**Why Semantic Kernel over direct API calls?**
Semantic Kernel is Microsoft's official AI orchestration SDK for .NET — the same framework powering Azure AI Foundry. Using it means the provider swap (Ollama ↔ Groq ↔ Azure OpenAI) is handled by the framework, not by custom code. It also demonstrates enterprise-grade AI development patterns relevant to Microsoft-stack companies.

**Why smart paragraph extraction instead of full file content?**
Study files can be 200KB+ (100,000+ characters). Sending full files to an AI model overloads the context window and causes timeouts. The agent scores each paragraph by keyword relevance to the question and sends only the top-scoring paragraphs (max 1500 chars per file). This makes responses faster and more accurate.

**Why ASP.NET Core Web API instead of a console app?**
A REST API makes the agent reusable across any frontend — browser, mobile, or Postman — and mirrors real-world enterprise architecture. It also demonstrates controller/service separation, dependency injection, and configuration management patterns that interviewers directly ask about.

---

## 🔀 Switching AI Provider

Just change one line in `appsettings.json`:

```json
"AI": {
  "Provider": "Groq"    ← fast cloud, free API key required
  "Provider": "Ollama"  ← fully local, no internet, needs 8GB+ free RAM
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
    "ModelId": "llama3-8b-8192"
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

### POST /api/agent/ask
Send an interview question and get a structured AI answer sourced from your study files.

**Request:**
```json
{
  "question": "What is dependency injection in ASP.NET Core?"
}
```

**Response:**
```json
{
  "answer": "Dependency Injection (DI) is a design pattern where dependencies are provided to a class rather than created by it. ASP.NET Core has a built-in DI container registered in Program.cs. Services are registered with a lifetime — Scoped, Transient, or Singleton — and injected via constructors.\n\nCode Example:\nbuilder.Services.AddScoped<IMyService, MyService>();",
  "sources": [
    "MVCInterviewQuestions.docx",
    "TechnicalInterview_Complete.docx"
  ],
  "topic": "ASPNET",
  "followUpQuestions": [
    "What is the difference between AddScoped, AddTransient, and AddSingleton?",
    "How does the DI container resolve circular dependencies?"
  ]
}
```

---

### GET /api/agent/files
Returns all study files currently loaded by the agent.

**Response:**
```json
[
  "HR_Interview_QA.docx",
  "MVCInterviewQuestions.docx",
  "TechnicalInterview_Complete.docx"
]
```

---

## 🗂️ Project Structure

```
InterviewPrepAgent/
├── Controllers/
│   └── AgentController.cs        # API endpoints — ask + files
├── Models/
│   └── Question.cs               # AgentResponse + AskRequest models
├── Services/
│   └── AgentService.cs           # Core agent logic:
│                                 #   - Provider selection (Groq/Ollama)
│                                 #   - .docx reading (OpenXml)
│                                 #   - Smart paragraph extraction
│                                 #   - Semantic Kernel orchestration
│                                 #   - Prompt engineering
├── StudyFiles/                   # Drop your .docx study files here
├── Properties/
│   └── launchSettings.json
├── appsettings.json              # AI provider config (switch here)
├── appsettings.Development.json
└── Program.cs                    # DI registration + app pipeline
```

---

## 📈 What This Project Demonstrates

| Concept | Implementation |
|---|---|
| AI Agent Development | Semantic Kernel orchestrating LLM calls with structured prompts |
| Strategy Pattern | AI provider selected at runtime via configuration |
| Dependency Injection | AgentService injected with IConfiguration via DI container |
| ASP.NET Core Web API | Clean controller/service separation, RESTful endpoints |
| Prompt Engineering | Structured output format (TOPIC/ANSWER/CODE/FOLLOWUP) |
| Document Processing | OpenXml reading .docx files, smart paragraph relevance scoring |
| Configuration Management | appsettings.json with environment-specific overrides |
| Error Handling | Timeout handling with provider-specific error messages |

---

## 🔮 Future Improvements

- Add vector search (Qdrant) for semantic similarity matching
- Add JWT authentication to secure the API
- Support PDF files using PdfPig
- Add a Blazor or React frontend
- Switch to Azure OpenAI for enterprise deployment
- Add multi-turn conversation memory using Semantic Kernel's ChatHistory

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
