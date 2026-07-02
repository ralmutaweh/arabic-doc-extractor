# Arabic Document Metadata Extraction Service

A .NET 10 Web API microservice that automatically extracts structured metadata fields from Arabic DOCX documents using GLiNER Arabic, a CPU-native named entity recognition model. Built for Bahraini government and business document processing contexts.

> **Important:** This service is a productivity tool — it pre-fills form fields to save time, but it does not replace the user reviewing the results. Overall extraction accuracy is 86% across names, entities, and countries. Every extracted value should be checked and confirmed by the user before being submitted. The goal is to reduce transcription time, not remove the user from the process.

---

## Architecture

Two Docker containers, orchestrated by Docker Compose:

```
Main System (external)
        │
        │ HTTP
        ▼
┌─────────────────────────────┐
│  .NET 10 Web API            │  Port 11467 (host) → 8080 (container)
│  arabic-doc-extractor       │
│                             │
│  ExtractionController       │
│  DashboardController        │
└────────────┬────────────────┘
             │ HTTP POST /extract
             ▼
┌─────────────────────────────┐
│  Python FastAPI             │  Port 8001 (internal only)
│  gliner-service             │
│                             │
│  GLiNER Arabic v2.1         │
│  (CPU-native NER model)     │
└─────────────────────────────┘
```

The GLiNER service is internal — it is not exposed to the main system directly. All traffic goes through the .NET API.

---

## Running Locally (Development)

### Prerequisites
- .NET 10 SDK
- Python 3.11+
- GLiNER dependencies (`pip install -r GlinerService/requirements.txt`)

### Start GLiNER sidecar manually
```bash
cd GlinerService
uvicorn main:app --host 0.0.0.0 --port 8001
```

### Start .NET API
```bash
dotnet watch
```

The API will be available at `http://localhost:5000`.

Add `appsettings.json` to the project root to point logs at the project folder during local development:
```json
{
  "CsvExtractionLogPath": "logs/extraction_log.csv",
  "CsvComparisonLogPath": "logs/feedback_log.csv",
  "PerformanceSummaryPath": "logs/performance_summary.json",
  "ComparisonFeatureEnabled": "true"
}
```

Without this file, the app falls back to hardcoded defaults that may write logs outside the project folder on Windows.

---

## Deploying with Docker

### First deployment
```bash
docker compose up -d --build
```

The GLiNER model (~300MB) downloads on first start and caches to `./gliner-cache/` on the host. Every restart after that reuses the cache — no re-download needed.

### Restart after a config change (no rebuild needed)
```bash
docker compose restart arabic-doc-extractor
```

### Rebuild after a code change
```bash
docker compose up -d --build
```

### View live logs
```bash
docker compose logs -f arabic-doc-extractor
docker compose logs -f gliner-service
```

---

## Environment Variables (`docker-compose.yml`)

| Variable | Default | Description |
|---|---|---|
| `GLINER_HOST` | `http://gliner-service:8001` | Internal GLiNER sidecar URL. Only change this if the service is renamed. |
| `CsvExtractionLogPath` | `/app/logs/extraction_log.csv` | Log file path inside the container. Mapped to `./logs/` on the host via volume mount. |
| `CsvComparisonLogPath` | `/app/logs/feedback_log.csv` | Comparison log path inside the container. |
| `PerformanceSummaryPath` | `/app/logs/performance_summary.json` | Performance summary path inside the container. |
| `ComparisonFeatureEnabled` | `true` | Set to `false` to silently disable the `/compare` endpoint. Restart the container to apply — no rebuild needed. |
| `OLLAMA_HOST` | `http://192.168.100.194:11434` | Legacy — Ollama is not active in production. Kept so that if the team decides to trial an LLM in the future, the host address is already in place and no `docker-compose.yml` changes are needed. |

---

## Ports

| Service | Host Port | Container Port | Called by |
|---|---|---|---|
| .NET API | `11467` | `8080` | Main system |
| GLiNER sidecar | `8001` | `8001` | .NET API only (internal) |

The GLiNER sidecar port `8001` is exposed for development convenience. In production it only needs to be reachable by the .NET API internally — it does not need to be accessible from outside the server.

---

## Endpoints

### POST `/api/extraction/upload`

Accepts a DOCX file and returns extracted metadata fields.

**Supported file types:** DOCX only. PDF support is already built into the codebase but blocked at the controller level. To enable it, remove the PDF check in `ExtractionController.Upload`.

**The default model is `gliner`** — no need to pass `?model=gliner` unless explicitly overriding.

**Request:** `multipart/form-data`

| Parameter | Type | Required | Description |
|---|---|---|---|
| `file` | file | Yes | DOCX document to extract from |
| `model` | string | No | Defaults to `gliner`. Only GLiNER is active in production. |

**Example:**
```bash
curl -X POST "http://server:11467/api/extraction/upload" \
  -F "file=@document.docx"
```

**Response:** `200 OK`
```json
{
  "extractionId": "690caf00-534f-4ab1-8552-353c46354187",
  "result": "{\"names\":[\"الدكتور سعيد أحمد فارس النعيمي\"],\"countries\":[\"البحرين\"],\"entities\":[\"وزارة المواصلات والاتصالات\"]}"
}
```

The `result` field is a JSON string — parse it to read the individual fields. The `extractionId` must be stored by the main system; it links all subsequent `/compare` calls back to this extraction.

---

### POST `/api/extraction/compare`

Called by the main system after the user reviews and submits the form. Compares what the model originally extracted against what the user actually saved, and logs the result for accuracy tracking.

If `ComparisonFeatureEnabled` is `false`, this endpoint returns `200 OK` silently with no logging or side effects.

**Request:** `application/json`

```json
{
  "extractionId": "690caf00-534f-4ab1-8552-353c46354187",
  "extractedNames": ["الدكتور سعيد أحمد فارس النعيمي"],
  "extractedEntities": ["وزارة المواصلات والاتصالات"],
  "extractedCountries": ["البحرين"],
  "userFinalUploadedNames": ["الدكتور سعيد أحمد فارس النعيمي"],
  "userFinalUploadedEntities": ["وزارة المواصلات والاتصالات", "هيئة المعلومات والحكومة الإلكترونية"],
  "userFinalUploadedCountries": ["البحرين"]
}
```

`extractedX` — the values returned by `/upload`. The main system must hold onto these from the upload response.
`userFinalUploadedX` — the values the user confirmed or corrected before submitting.

**Response:** `200 OK`
```json
{
  "extractionId": "690caf00-534f-4ab1-8552-353c46354187",
  "namesMatch": true,
  "entitiesMatch": false,
  "countriesMatch": true,
  "score": "2/3"
}
```

Matching is case-insensitive and order-independent. Duplicates are ignored — both lists are treated as sets.

---

### GET `/api/Dashboard/report-json`

Returns the current performance summary as JSON. Called by the dashboard.

**Response:** `200 OK`
```json
{
  "totalExtractions": 47,
  "totalComparisons": 23,
  "fullMatchCount": 18,
  "totalFieldsChanged": 9,
  "averageLatencyMs": 5823,
  "extractionsToday": 5,
  "lastUpdated": "2026-07-02T09:01:28"
}
```

---

### GET `/api/Dashboard/recent-logs`

Returns the last 10 extraction log entries. Called by the dashboard.

---

### GET `/api/Dashboard/recent-comparisons`

Returns the last 10 comparison log entries. Called by the dashboard.

---

### GET `/api/Dashboard/latency-trend`

Returns the last 200 extraction entries with only `timestamp` and `latencyMs`, oldest first. Powers the latency trend chart on the dashboard.

---

## Developer Dashboard

Access at: `http://server:11467/dashboard.html`

The dashboard is a static HTML page served directly from `wwwroot/` — no controller, no authentication. It is intended for internal developer use only and should not be exposed publicly.

**What the dashboard shows:**

- **Total Extractions** — cumulative count since the service started
- **Extractions Today** — resets at midnight UTC
- **Avg Latency** — running average end-to-end time per extraction
- **Total Comparisons** — how many `/compare` calls have been received
- **Full Match Rate** — percentage of comparisons where all 3 fields matched exactly. Green above 80%, yellow above 50%, red below
- **Fields Changed** — total individual field mismatches across all comparisons. A high number relative to Total Comparisons is the clearest signal that the entity or country lists need updating
- **Latency Trend chart** — last 200 extractions as a line chart, oldest left to newest right. Watch for gradual increases over time which may indicate server load issues
- **Recent Extractions panel** — last 10 extractions with time, latency, and stop reason

The dashboard auto-refreshes every 8 seconds. The "Refresh Now" button forces an immediate update.

**What to watch:** if Full Match Rate drops below 80% consistently, open `GlinerService/main.py` and check whether the entity or country lists are missing values that users are regularly adding manually. That is almost always the cause.

---

## Log Files

All log files live in `./logs/` on the host server, mapped from `/app/logs/` inside the container via the volume mount in `docker-compose.yml`.

| File | Description |
|---|---|
| `extraction_log.csv` | One row per upload. Columns: `extraction_id, timestamp, file_name, file_type, file_size_bytes, latency_ms, model, prompt_tokens, completion_tokens, total_duration_ms, eval_duration_ms, done_reason` |
| `feedback_log.csv` | One row per comparison. Columns: `extraction_id, timestamp, names_match, entities_match, countries_match, overall_score, extracted_names, actual_names, extracted_entities, actual_entities, extracted_countries, actual_countries` |
| `performance_summary.json` | Running aggregate metrics updated after every extraction and every comparison |

**Note on the LLM columns in `extraction_log.csv`:** `prompt_tokens`, `completion_tokens`, `total_duration_ms`, and `eval_duration_ms` are always empty when using GLiNER. These columns are kept intentionally, if the team decides to trial an LLM in the future, the log format is already ready and historical GLiNER rows will simply have empty values in those columns. No migration needed.

To read logs directly on the server:
```bash
tail -f logs/extraction_log.csv
tail -f logs/feedback_log.csv
cat logs/performance_summary.json
```

---

## Keeping Entity & Country Lists in Sync with the Main System

This is the most important ongoing maintenance task for keeping accuracy high.

The entities and countries GLiNER can detect are defined in `GlinerService/main.py`. If the main system uses organisation names or country variants that are not in these lists, GLiNER will miss them — they will appear as changed fields in the comparison logs and bring down the Full Match Rate on the dashboard.

### How to update the lists

1. Open `GlinerService/main.py`
2. Find the `ENTITIES` or `COUNTRIES` list
3. Add the exact Arabic name as it appears in real documents, including all informal variants

**Adding a new entity:**
```python
ENTITIES = [
    ...
    "ديوان المحاسبة",        # exact name as it appears in documents
    "محكمة الاستئناف",
]
```

**Adding a new country with variants:**
```python
COUNTRIES = [
    ...
    "جمهورية كينيا",   # formal name
    "كينيا",           # informal variant — always add both
]
```

4. Rebuild only the GLiNER container — the .NET API does not need to be rebuilt:
```bash
docker compose up -d --build gliner-service
```

**When to update:** whenever the main system adds new organisations or countries to its controlled vocabulary, mirror those additions here. The closer these lists match what users actually see and type in the main system, the higher the Full Match Rate will be.

---

## Extending GLiNER — Adding New Extraction Fields

All extraction logic lives in `GlinerService/main.py`. Two matching strategies are available:

### Semantic Matching (used for Names)
GLiNER understands the concept of what you are looking for — no predefined list needed. It detects any text span that semantically fits the label, including values it has never seen before.

```python
semantic_labels = ["اسم الشخص"]  # "person name" in Arabic
```

To add a new semantic field such as job titles:
```python
# 1. Add the label
semantic_labels = ["اسم الشخص", "المسمى الوظيفي"]

# 2. Add to result dict
result = {
    "names": None,
    "countries": None,
    "entities": None,
    "job_titles": None,
}

# 3. Handle in the loop
for semantic_entity in semantic_entities_extracted:
    if semantic_entity["label"] == "المسمى الوظيفي":
        if result["job_titles"] is None:
            result["job_titles"] = []
        result["job_titles"].append(semantic_entity["text"])
```

### Span Matching (used for Entities and Countries)
GLiNER matches text spans against a predefined list. Only values from the list can be returned — the model cannot invent values that are not in the source document.

```python
entity_matches_extracted = model.predict_entities(request.text, ENTITIES, threshold=0.4)
```

`threshold` controls minimum confidence (0.0–1.0). Lower means more matches but more noise; higher means fewer but more precise. The current values — `0.4` for entities, `0.35` for countries — were tuned during benchmarking on Bahraini government documents. Adjust carefully and test after any change.

---

## Model Information

**Model:** NAMAA-Space/gliner_arabic-v2.1
**Architecture:** Encoder-only bidirectional transformer (GLiNER framework)
**Size:** ~300MB
**Hardware:** CPU only — no GPU required
**Measured latency:** 4.2–7.1 seconds per document (i7 7th Gen CPU)

**Benchmark accuracy (10 synthetic Bahraini documents):**

| Field | Accuracy |
|---|---|
| Names | 89% |
| Entities | 100% |
| Countries | 70% |
| **Overall** | **86%** |

A key property of GLiNER's encoder-only architecture: it can only return text spans that already exist in the source document. It cannot generate or hallucinate content — what you get back is always a direct quote from the input.

---

## Handover Notes

### Why these decisions were made

**GLiNER over an LLM:** the alternative evaluated was qwen3.5:9b, which required a dedicated GPU (minimum 6GB VRAM) to run at usable speed. On CPU it took 30 seconds per document — slower than typing the fields manually. GLiNER runs entirely on CPU in 4–8 seconds, handles all concurrent users simultaneously due to its parallel encoder architecture, and achieved comparable overall accuracy (86% vs 87%) with 100% entity accuracy compared to qwen's 60–90%.

**CSV logs over a database:** the service is stateless by design — it extracts and logs, nothing more. No database means no schema migrations, no connection strings, no ORM setup. Any developer can open the log files in a text editor or Excel and understand what happened without any tooling.

**LLM support is preserved, not removed:** `LlmService.cs` and the `OllamaSharp` dependency remain in the codebase. The `/upload` endpoint accepts a `model` parameter that defaults to `gliner` but works with any Ollama model name. Token columns in the extraction log are already in place. If the team wants to trial an LLM in the future, no code needs to be re-written — point `OLLAMA_HOST` at a running Ollama instance and pass the model name in the request.

### What is not yet built

- **PDF support:** `DetectFileType` already identifies PDFs by magic bytes, and `PdfService` is already wired up for text extraction. To enable, remove the PDF block in `ExtractionController.Upload`.
- **Description field:** GLiNER cannot generate descriptions — that requires a generative model. If this becomes a requirement, a lightweight CPU-optimised LLM could be added as a second sidecar alongside GLiNER.
- **Log rotation:** CSV files grow indefinitely. At high document volumes, consider a cron job to archive rows older than a set period.
- **Dashboard access control:** the dashboard has no login and relies entirely on network-level restrictions. If the server is ever reachable from outside the internal network, add basic auth middleware in front of `DashboardController`.

### Repository structure

```
arabic-doc-extractor/
├── Controllers/
│   ├── ExtractionController.cs   # upload and compare endpoints
│   └── DashboardController.cs    # dashboard JSON endpoints
├── Models/
│   ├── ExtractionVerification.cs # compare request DTO
│   ├── ExtractionLogEntry.cs     # maps extraction_log.csv rows
│   └── ComparisonLogEntry.cs     # maps feedback_log.csv rows
├── Services/
│   ├── GlinerService.cs          # calls GLiNER sidecar
│   └── LlmService.cs             # retained for future LLM use — not active by default
├── Utilities/
│   ├── PerformanceMonitor.cs     # reads/writes performance_summary.json
│   └── CsvTailReader.cs          # reads last N lines of CSV backwards without loading the full file
├── Middleware/
│   └── RequestLoggingMiddleware.cs
├── wwwroot/
│   ├── dashboard.html            # developer dashboard
│   ├── dashboard.css
│   └── dashboard.js
├── GlinerService/
│   ├── main.py                   # all extraction logic — edit here to add fields or update lists
│   ├── requirements.txt
│   └── Dockerfile
├── logs/                         # runtime only — gitignored
├── gliner-cache/                 # model weights cache — gitignored
├── docker-compose.yml
├── appsettings.json              # local development only — not included in Docker
└── Program.cs
```