# SmartPrep Modern

SmartPrep Modern is the Windows desktop client for **SmartPrep**, a criminology review and examination platform built as a client-server system. The application supports review directors, reviewees, and administrators through role-specific workflows for source management, exam generation and delivery, user administration, and performance analytics.

> **Project context:** SmartPrep was developed in under two months as an end-to-end desktop and backend system combining exam workflows, analytics, PDF processing, background jobs, real-time updates, and LLM-assisted analysis.

## Screenshots

> Screenshots will be added after the local environment is restored.

<!-- Suggested screenshots:
1. Login / main dashboard
2. Topic/source management
3. Exam generation or exam session
4. Exam analytics / growth trend
5. Question forensics / AI analysis
6. Leaderboard or admin user management
-->

## Key features

- **Source-based exam generation** — organize review material by category/topic, upload questionnaire PDFs, and generate exams from selected content.
- **Exam delivery** — reviewees can take configured examinations and submit answers through the desktop client.
- **Performance analytics** — dashboards for exam results, comparative performance, growth trends, and leaderboards.
- **Question forensics** — per-question distributions and deeper attempt/item analysis for identifying strengths and problem areas.
- **AI-assisted analysis** — surfaces structured performance summaries and recommendations produced by the backend.
- **Role-based workflows** — dedicated interfaces for administrators, review directors, and reviewees.

## Architecture

```text
┌──────────────────────────────┐
│ SmartPrep Modern             │
│ WPF / VB.NET / .NET 9        │
│                              │
│ Views + reusable components  │
│ Repository API layer         │
│ Shared HTTP service          │
└──────────────┬───────────────┘
               │ REST / WebSocket
               ▼
┌──────────────────────────────┐
│ SmartPrep Python             │
│ FastAPI                      │
│ Auth / Slots / Exams         │
│ Analytics / WebSocket        │
└───────┬──────────┬───────────┘
        │          │
        ▼          ▼
   Relational   Redis + Celery
   database     background jobs
                   │
                   ▼
              LLM analytics
```

The client deliberately keeps API communication separate from the UI. `APISync/Repositories` exposes domain-oriented operations, while `APISync/Services/ApiService.vb` provides the shared HTTP transport and `APISync/Models` contains request/response contracts.

## Technology stack

| Area | Technology |
| --- | --- |
| Desktop | WPF, VB.NET, .NET 9 |
| UI | MaterialDesignThemes, MaterialDesignColors |
| Charts | LiveCharts.Wpf |
| API communication | `HttpClient`, JSON REST API |
| Backend | FastAPI / Python |
| Background processing | Celery + Redis |
| Database | MySQL in the original backend implementation |
| AI analysis | Ollama-backed LLM workflows |
| Documents | PDF questionnaire/material ingestion |

## Project structure

```text
SmartPrepModern/
├── APISync/
│   ├── Models/          # API request/response contracts
│   ├── Repositories/    # Domain-specific API access
│   └── Services/        # Shared HTTP transport
├── Components/          # Reusable WPF components and analytics views
├── Layout/              # Main application shell/navigation
├── Services/            # Client-side application services/session state
├── Views/
│   ├── Account/
│   ├── Admin/
│   ├── Analytics/
│   ├── Auth/
│   ├── ReviewDirector/
│   └── Reviewee/
├── Application.xaml
├── MainWindow.xaml
└── SmartPrepModern.vbproj
```

## Running the desktop client locally

### Requirements

- Windows
- .NET 9 SDK
- Visual Studio with .NET desktop development workload, or the .NET CLI
- A running SmartPrep Python backend

The client now reads the backend address from the `SMARTPREP_API_BASE_URL` environment variable and falls back to `http://127.0.0.1:8000/` for local development.

PowerShell example:

```powershell
$env:SMARTPREP_API_BASE_URL="http://127.0.0.1:8000/"
dotnet restore
dotnet run
```


Start the backend first. See the companion repository for its dependencies and worker processes.

### First administrator setup

On a fresh SmartPrep installation, create the first administrator **through SmartPrep Modern's registration interface**. New accounts, including Admin accounts, are initially created with:

```text
status = locked
```

Because there is no active administrator yet, connect to the SmartPrep MySQL database using a database-management tool such as MySQL Workbench, DBeaver, DataGrip, or another MySQL client.

When using the provided Docker setup, MySQL is exposed to Windows at:

```text
Host:     127.0.0.1
Port:     3307
Database: smartprep
User:     smartprep
Password: <your configured DB password>
```

Open the `users` table, locate the administrator you just registered, and change the `status` column from:

```text
locked
```

to:

```text
active
```

You can also perform the same activation with SQL:

```sql
UPDATE users
SET status = 'active'
WHERE username = 'YOUR_ADMIN_USERNAME';
```

The administrator can then sign in through SmartPrep Modern normally.

> Do not create the first user manually in the database. Register through SmartPrep Modern first so the normal registration flow creates the account, then use the database only to perform the initial activation.


## Local documents

Questionnaires, examination materials, and other confidential source documents are runtime/local data and are intentionally **not included in this repository**. Use your own authorized test documents when restoring or demonstrating the application.

## Backend

SmartPrep Modern is designed to work with the separate **SmartPrepPython** FastAPI backend:

https://github.com/SaintRelion/SmartPrepPython

## Engineering notes

This repository reflects a project delivered under a short development window. It is preserved as a working example of integrating a native Windows client with a separate API/backend architecture and multiple supporting services. The architecture and coding practices in newer projects have continued to evolve since this implementation.

## Author

**June Aurelius Jacinto**  
Full-Stack Software Developer

GitHub: https://github.com/SaintRelion
