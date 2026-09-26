# SmartPrep Modern

SmartPrep Modern is the Windows desktop client for **SmartPrep**, a criminology review and examination platform built as a client-server system. It provides role-specific workflows for review directors, reviewees, and administrators.

> **Project context:** SmartPrep was developed in under two months as an end-to-end desktop and backend system combining exam workflows, analytics, PDF processing, background jobs, real-time updates, and LLM-assisted analysis.

## Key features

- **Source-based exam generation** — organize review material by category/topic, upload questionnaire PDFs, and generate exams from selected content.
- **Exam delivery** — reviewees can take configured examinations and submit answers through the desktop client.
- **Performance analytics** — dashboards for exam results, comparative performance, growth trends, and leaderboards.
- **Question forensics** — per-question distributions and deeper attempt/item analysis for identifying strengths and problem areas.
- **AI-assisted analysis** — displays structured performance summaries and recommendations produced by the backend.
- **Role-based workflows** — dedicated interfaces for administrators, review directors, and reviewees.

## Screenshots

> Screenshots will be added after the local environment is restored.

<!-- Suggested screenshots:
1. Main dashboard
2. Topic/source management
3. Exam generation or exam session
4. Performance analytics / growth trends
5. Question forensics / AI analysis
-->

## Technology stack

- WPF / VB.NET / .NET 9
- Material Design
- LiveCharts
- REST API + WebSockets
- FastAPI backend
- MySQL
- Redis + Celery
- Ollama-backed LLM workflows

The desktop client keeps API communication separate from the UI through domain repositories, shared HTTP services, and request/response models.

## Run SmartPrep

SmartPrep Modern requires the separate **SmartPrepPython** backend:

https://github.com/SaintRelion/SmartPrepPython

For the quickest setup, start the backend using its Docker Compose configuration:

```powershell
docker compose up -d --build
```

Then run SmartPrep Modern on Windows.

The backend address is configured in `APISync/Services/ApiService.vb` through `ApiService.BaseUrl`. Update that value if you are running the backend at a different address.

## First administrator

On a fresh installation, register the first administrator through SmartPrep Modern. New accounts are initially created in a locked state.

After registration, connect to the SmartPrep MySQL database, locate the new administrator in the `users` table, and change `status` from `locked` to `active`. When using the provided backend Docker setup, MySQL is exposed to the host on port `3307` by default.

Do not create the first administrator directly in the database; use the application's registration flow first, then perform only the initial activation.

## Questionnaire documents

Questionnaires and other source documents are runtime/local data and are intentionally not included in the repository.

The preserved SmartPrep backend expects questionnaires in a structured format because its ingestion pipeline uses deterministic parsing. See the **SmartPrepPython** README for the supported format, the reasoning behind the original design, and the planned modernization toward LLM-assisted document extraction.

## Local setup

This section is only needed when running the desktop client directly during development.

Requirements:

- Windows
- .NET 9 SDK
- Visual Studio with the .NET desktop development workload, or the .NET CLI
- A running SmartPrepPython backend

From the repository root:

```powershell
dotnet restore
dotnet run
```

Before starting the client, make sure `ApiService.BaseUrl` points to the backend instance you want to use.

## Author

**June Aurelius Jacinto**  
Full-Stack Software Developer

GitHub: https://github.com/SaintRelion
