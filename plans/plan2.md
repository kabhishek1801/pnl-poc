# Plan: Real-Time PnL Capture, Processing & Reporting POC

Greenfield POC in empty workspace `c:\Users\z004ff8j\OneDrive - Siemens AG\study\bnp\poc`.
Clean Architecture .NET 9 backend + Angular 19 SPA + RabbitMQ + SignalR + MS SQL 2022, all via docker-compose.

## Confirmed decisions (latest discussion)
- Layout: `ui/` (Angular) + `server/` (one .sln, multiple projects) + `docker-compose.yml` at root — both are direct children of the workspace root, no intermediate `src/` folder.
- One API host containing two hosted background workers: FileWorker (CSV) and QueueWorker (realtime messages). All are projects in the same solution; the workers run as `BackgroundService` implementations inside `PnL.Api` for this simplified POC.
- SignalR Option 1: workers publish notification messages to a fanout exchange; API consumes and broadcasts. Angular holds ONE hub connection.
- EF Core 9 + migrations only (NO SqlBulkCopy). Use standard repository pattern with EF Core, not raw ADO.NET for this POC.
- File flow: Angular uploads multipart -> API stores the raw CSV in MinIO (recommended prod-grade staging), publishes `FileUploaded { BatchId, Bucket, ObjectKey }`, returns 202 Accepted. FileWorker parses the file from MinIO.
- Realtime flow: Angular form submits one record -> API validates payload -> API publishes to `pnl.realtime` queue -> QueueWorker processes it.
- Business rule: `PnLAmount == 0` is not a transport failure; it is a valid excluded record. Persist it with `Status = Excluded`, `ExclusionReason = "ZeroPnLAmount"`, and send it to UI as a processed record.
- Idempotency & upsert model: `AccountNumber` is the business key (assumed unique for this POC). Every processed message — from a file row or a realtime submission — is an **upsert by `AccountNumber`**: if a row for that account exists, update its `SourceSystem`, `PnLAmount`, `Status`, `ExclusionReason`, `FeedSource`, `BatchId`, `CapturedAtUtc`; otherwise insert a new row. Re-uploading the same file (or resubmitting a realtime record) is therefore explicitly **allowed** and simply refreshes the existing account's values rather than being rejected as a duplicate. Kept simple for the POC: no separate re-delivery tracking — an upsert is naturally safe to repeat, so a redelivered message just re-applies the same values (at most a harmless duplicate UI update).
- `BatchId` is a new GUID generated per upload request (every file upload is a distinct batch, regardless of content).
- Feed carries ONLY 3 fields: `SourceSystem` (string), `AccountNumber` (int), `PnLAmount` (int). No BusinessDate.
- UI: single page, dropdown selects feed type (File | Realtime) which toggles upload or form; two live tables on the same page: Validated PnL and Excluded PnL.
- No test projects in this POC.
- No auth, no CI.

## Architecture summary
- `PnL.Api`: ingress, SignalR hub, read APIs, small validation, file upload endpoint, notification relay, and hosted background consumers.
- `FileWorkerHostedService`: consumes file-upload messages, downloads CSV from MinIO, parses rows, validates business rule, persists valid/excluded rows, and emits `PnLRecordProcessed` notifications.
- `QueueWorkerHostedService`: consumes realtime messages, validates them, persists valid/excluded records, and emits notifications.
- `PnL.Infrastructure`: EF Core DB context, repositories, RabbitMQ publishers/consumers, MinIO client, CSV parser.
- `PnL.Contracts`: shared DTOs for queue and notification payloads.
- `ui/`: Angular SPA with one SignalR connection and two reporting tables.

## Solution structure
server/PnL.sln
- PnL.Domain          -> `PnLRecord` (with a plain `string SourceSystem` property — kept simple for the POC, no value object), `PnLStatus`/`FeedSource` enums, zero-amount rule.
- PnL.Application     -> plain injectable service classes (no MediatR — only 2 internal callers, no HTTP dispatch needed), ports: `IPnLRepository`, `IFeedPublisher`, `INotificationPublisher`, `IFileStorage`, `ICsvFeedParser`.
- PnL.Infrastructure  -> EF Core DB context, repositories, RabbitMQ connection + publishers, MinIO adapter, CSV parser.
- PnL.Contracts       -> `FileUploadedMessage`, `RealtimePnLMessage`, `PnLProcessedNotification`.
- PnL.Api             -> controllers, `PnLHub`, `NotificationRelayHostedService`, `FileWorkerHostedService`, `QueueWorkerHostedService`, Swagger, health checks, CORS.
ui/ -> Angular 19 standalone + signals + Angular Material + `@microsoft/signalr` + nginx Dockerfile.

## Project folder structure
```
poc/
├── docker-compose.yml
├── .env
├── server/
│   ├── PnL.sln
│   ├── Directory.Build.props
│   ├── PnL.Domain/
│   │   ├── PnLRecord.cs
│   │   ├── Enums/PnLStatus.cs
│   │   ├── Enums/FeedSource.cs
│   │   └── Rules/ZeroAmountExclusionRule.cs
│   ├── PnL.Application/
│   │   ├── Interfaces/IPnLRepository.cs
│   │   ├── Interfaces/IFeedPublisher.cs
│   │   ├── Interfaces/INotificationPublisher.cs
│   │   ├── Interfaces/IFileStorage.cs
│   │   ├── Interfaces/ICsvFeedParser.cs
│   │   ├── Features/Ingestion/IPnLRecordProcessor.cs
│   │   ├── Features/Ingestion/PnLRecordProcessor.cs
│   │   ├── Features/Reporting/IPnLReportService.cs
│   │   └── Features/Reporting/PnLReportService.cs
│   ├── PnL.Infrastructure/
│   │   ├── Persistence/PnLDbContext.cs
│   │   ├── Persistence/Configurations/PnLRecordConfiguration.cs
│   │   ├── Persistence/Migrations/
│   │   ├── Persistence/PnLRepository.cs
│   │   ├── Adapters/Messaging/RabbitMqConnectionProvider.cs
│   │   ├── Adapters/Messaging/RabbitMqFeedPublisher.cs
│   │   ├── Adapters/Messaging/RabbitMqNotificationPublisher.cs
│   │   ├── Adapters/Storage/ObjectStorage.cs
│   │   └── Helpers/CsvFeedParser.cs
│   ├── PnL.Contracts/
│   │   ├── FileUploadedMessage.cs
│   │   ├── RealtimePnLMessage.cs
│   │   └── PnLProcessedNotification.cs
│   └── PnL.Api/
│       ├── Program.cs
│       ├── Controllers/FeedsController.cs
│       ├── Controllers/ReportsController.cs
│       ├── Hubs/PnLHub.cs
│       ├── Services/NotificationRelayHostedService.cs
│       ├── Services/FileWorkerHostedService.cs
│       ├── Services/QueueWorkerHostedService.cs
│       ├── Services/RabbitMqConsumerBase.cs
│       ├── appsettings.json
│       └── Dockerfile
└── ui/
    ├── src/app/
    │   ├── core/signalr.service.ts
    │   ├── core/api.service.ts
    │   ├── features/feed-capture/feed-capture.component.ts
    │   └── features/pnl-report/pnl-report.component.ts
    ├── src/environments/environment.ts
    ├── angular.json
    ├── package.json
    └── Dockerfile
```

## Messaging topology
- Exchange `pnl.ingestion` (direct):
  - `pnl.file.uploaded` -> FileWorker
  - `pnl.realtime` -> QueueWorker
- Exchange `pnl.notifications` (fanout) -> queue bound for API notification relay -> SignalR broadcast.
- Manual ack, prefetch. No dead-letter queue for now (kept out of scope for this POC — may be added later).
- EXCLUDED records still go through the normal processing flow; they are NOT treated as failures.

## Database
Table `PnLRecords`: `Id` uniqueidentifier PK (surrogate, generated once on first insert, stable across updates), `AccountNumber` int **unique** (business key, upsert target), `SourceSystem` nvarchar(100),
`PnLAmount` int, `Status` tinyint (0=Valid,1=Excluded), `FeedSource` tinyint (0=File,1=Realtime),
`ExclusionReason` nvarchar(200) null, `BatchId` uniqueidentifier null (most recent batch that touched this row), `CapturedAtUtc` datetime2 (overwritten on every upsert — doubles as "last touched" time, kept single-column for POC simplicity).
Indexes: `(Status, CapturedAtUtc desc)`, `(FeedSource)`, `(SourceSystem)`, unique index on `AccountNumber` (the upsert key).
Migrations applied at API startup via migration runner with retry.

## API surface
- POST `/api/feeds/file` -> 202 Accepted with `{ batchId }`
- POST `/api/feeds/realtime` -> 202 Accepted with `{ messageId }`
- GET `/api/pnl/validated?page&pageSize&sourceSystem`
- GET `/api/pnl/excluded?page&pageSize&sourceSystem`
- GET `/api/pnl/summary`
- GET `/health`
- Hub `/hubs/pnl` -> event `PnLRecordProcessed`

## Processing rules
- `PnLAmount == 0` => upsert row as excluded, `Status = Excluded`, `ExclusionReason = "ZeroPnLAmount"`.
- `PnLAmount != 0` => upsert row as valid.
- Upsert is keyed by `AccountNumber`: existing account rows are updated in place; new accounts are inserted. Both valid and excluded rows are persisted and reported.
- UI receives a SignalR event for each processed record and adds/updates it in the corresponding table.

## Phases
1. Scaffolding & infra (solution, projects, refs, docker-compose, MinIO, RabbitMQ, SQL, Serilog, health checks).
2. Domain + Application (entity, rule, ports, handlers, result pattern).
3. Infrastructure (EF Core, repository, RabbitMQ topology, MinIO adapter, CSV parser).
4. API (controllers, hub, notification relay, CORS, Swagger, migration runner).
5. Hosted consumers (FileWorkerHostedService, QueueWorkerHostedService, shared consumer base class with manual ack).
6. Angular SPA (feed-type selector, upload form, realtime form, two live tables, SignalR client).
7. Dockerize + compose everything, run end-to-end verification.

## Verification checklist
1. Start stack via `docker compose up -d --build`.
2. Upload a CSV with a mix of valid and zero rows — expect 202 and live update in both tables.
3. Submit a realtime message with zero amount — expect it to appear in the excluded table.
4. Submit another realtime message with non-zero — expect it in the valid table.
5. Re-upload the same CSV (new `BatchId`) — expect the same accounts to be **updated in place** (no duplicate rows), UI reflects the refreshed values live.
6. Confirm SQL contains exactly one row per `AccountNumber`, each reflecting valid or excluded status.
