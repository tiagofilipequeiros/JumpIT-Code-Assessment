# Product Manager

Product management API (ASP.NET Core 10, EF Core, SQL Server) with an Angular 22 dashboard.

- [Quick start](#quick-start)
- [Review in 5 minutes](#review-in-5-minutes)
- [Assessment coverage](#assessment-coverage)
- [Architecture](#architecture)
- [Decisions and trade-offs](#decisions-and-trade-offs)
- [Assumptions](#assumptions)
- [Where this differs from common practice](#where-this-differs-from-common-practice)
- [Metrics](#metrics)
- [API](#api)
- [Running parts separately](#running-parts-separately)
- [Tests](#tests)
- [Database](#database)
- [Configuration and secrets](#configuration-and-secrets)
- [Project structure](#project-structure)
- [Extras (own initiative)](#extras-own-initiative)
- [Known limitations and next steps](#known-limitations-and-next-steps)

## Quick start

Needs Docker (with about 2 GB of memory for SQL Server). Ports 4200, 8080 and 1433 must be free: a locally installed SQL Server also uses 1433 (stop it, or change the left side of `"1433:1433"` in `docker-compose.yml`). On Apple Silicon, SQL Server runs through Docker Desktop's x86 emulation (Rosetta), which must be enabled.

```
docker compose up --build
```

| | |
|---|---|
| App | http://localhost:4200 |
| API + Swagger | http://localhost:8080/docs |
| Health | http://localhost:8080/api/health |

This starts SQL Server, the API and the frontend. On the first start the API creates the database (migrations), inserts the seed data and generates about 90 days of demo activity for the Metrics tab. Reset everything with `docker compose down -v`.

**Versions:** .NET 10 · EF Core 10.0.12 · SQL Server 2025 · Angular 22.2 · Angular Material 22.2 · Bootstrap 5.3.8 · ApexCharts 7.8 · Node 24.

### Users

There is no login page and no password (see [Users and roles](#users-and-roles)). Pick a user in the header dropdown.

| Id | Name | Email | Role | Can |
|---|---|---|---|---|
| 1 | Alex Admin | admin@example.com | Admin | everything |
| 2 | Erin Editor | editor@example.com | Editor | create, edit, enable/disable, see hidden items, product metrics |
| 3 | Sam User | sam@example.com | User | view and change stock |
| 4 | Taylor User | taylor@example.com | User | view and change stock |

**In Swagger:** click **Authorize** and enter a user Id. Reads work without a user; changes need one.

**From the command line**, send the user in the `X-User-Id` header:

```
curl -X POST http://localhost:8080/api/products/100000/decrement-stock/2 -H "X-User-Id: 3"
curl -X POST http://localhost:8080/api/products -H "X-User-Id: 1" -H "Content-Type: application/json" \
     -d '{"name":"Polarizing Filter","price":45.5,"stock":10,"categoryId":6}'
```

## Review in 5 minutes

1. Open http://localhost:4200 as **Alex Admin**. Type `obj` in the search, then add **Stock status: Low stock**: the filters combine (AND) and stay in the URL. Filter **Status: Disabled** to see a hidden product.
2. Open a product's menu → **History**: every price and stock change, from the SQL Server temporal table.
3. Use the stock button on a product with little stock and remove more than is left: the API refuses (`409 InsufficientStock`) and nothing changes.
4. Switch to **Sam User**: no create, edit, delete or Metrics; the hidden products are gone.
5. Open **Metrics** as Alex: switch **Products / Users**, change the range, drag on a timeline to zoom, use the table icon to see exact values.
6. In Swagger, authorize as `1` and try `POST /api/products` with an invalid body: one error per field, with `"code": "ValidationFailed"`.

## Assessment coverage

| Requirement | Where / how |
|---|---|
| 9 endpoints (CRUD, add/decrement stock, search, stock level) | `ProductsController`; see [API](#api) |
| 6-digit unique ID, safe with many instances | SQL Server sequence `ProductIds` (100000-999999) as the column default; [Product IDs](#product-ids) |
| Validation on create and update | Data annotations on the request DTOs + business rules in the services; one error per field |
| Stock in every list response | `ProductResponse.Stock`, returned by every list endpoint |
| EF Core code-first migrations | `backend/src/Products.Infrastructure/Persistence/Migrations`, applied on startup |
| Seeding | Products, categories and users in migrations; demo activity at startup ([Seeding](#seeding)) |
| Angular frontend that consumes the API | Products, Categories and Metrics pages; every action calls the API and the list reloads |
| Reflects changes made through the API | Lists reload after every action, with a Refresh button, and when the browser tab becomes visible again |
| Latest Angular, clean structure, async handled | Angular 22, standalone components, signals, typed services, interceptors for errors and user header |
| Unit tests | 3 backend test projects + Vitest; see [Tests](#tests) |
| Documentation | This file |
| Optional: BDD | Reqnroll scenarios in `backend/tests/Products.AcceptanceTests/Features` |
| Optional: UX, validation, filtering, UI framework | Angular Material + Bootstrap, responsive; search while typing plus category, status, stock status, stock and price filters (all combined); inline validation, confirmations |

## Architecture

```
backend/src
  Products.Domain          entities, limits, roles → permissions, error codes        (no dependencies)
  Products.Application     use cases (services), DTOs, interfaces it needs          → Domain
  Products.Infrastructure  EF Core: DbContext, migrations, repositories, seeding     → Application
  Products.Api             controllers, error handling, Swagger, wiring             → Infrastructure
```

Dependencies point inwards. The Application layer does not know EF Core: it talks to interfaces (`IProductRepository`, `IUnitOfWork`, …) that Infrastructure implements.

**A request, e.g. `POST /api/products/{id}/decrement-stock/3`:**

1. `ProductsController`: HTTP only, one line that calls the service.
2. `ProductService`: checks the role (`CurrentUser`), validates the quantity and opens a transaction (`IUnitOfWork`).
3. `ProductRepository.TryChangeStockAsync`: one SQL `UPDATE … WHERE Stock - 3 >= 0`.
4. `UserMetricService` records who did it; the unit of work saves it in the same transaction.
5. Any `AppException(ErrorCode)` becomes a ProblemDetails response in `GlobalExceptionHandler`.

**Repositories: specific, not generic.** One repository per aggregate (`ProductRepository`, `CategoryRepository`, …) with methods named after what the use case needs (`TryChangeStockAsync`, `MoveToCategoryAsync`). A generic `Repository<T>` would only re-expose what `DbContext` already does, and would push query details into the services. The trade-off is a few more methods to write; in return the services are free of EF Core, can be unit-tested with fakes, and the SQL-sensitive parts (atomic updates, temporal queries) live in one place. `DbContext` is the unit of work behind `IUnitOfWork`.

**Frontend:** standalone components and signals; no state-management library. `core/` holds enums, models, HTTP services, the interceptors (adds `X-User-Id`, turns errors into a typed `ApiError`) and the route guard. Pages live in `features/`, reusable pieces (chart, stat tile, confirm dialog) in `shared/`.

## Decisions and trade-offs

| Decision | Alternative considered | Why |
|---|---|---|
| SQL Server | PostgreSQL, SQLite | The database generates the IDs (sequence), which is safe across instances. SQLite is a single file and can't serve multiple API instances. SQL Server over Postgres: the usual Microsoft/.NET stack, and temporal tables built in. Heavier Docker image. |
| ID from a sequence | Random 6-digit number + unique index + retry | One atomic number per insert, no retries; random IDs collide more as the table fills. Capacity: 900,000 IDs (100,000-999,999; 100,000-100,099 reserved for seed data). |
| Atomic stock update (`UPDATE … WHERE`) | Read, check, then save | Read-then-write loses updates under concurrency. A test sends 10 parallel decrements on a stock of 3: exactly 3 succeed. A check constraint (`Stock >= 0`) is the last line of defence. |
| RowVersion on edits | Last write wins | Two people editing the same product get a `409 ConcurrencyConflict` instead of silently overwriting each other. |
| Temporal table for product history | An audit table written by the code | SQL Server keeps every version automatically, also for changes made outside the API. Ties the project to SQL Server. |
| Retry policy (EF execution strategy) | No retries | Retries transient SQL errors, and SQL Server error 13535 (a temporal-table conflict under concurrency, found by the concurrency tests). Transactions run inside the strategy and a retry starts from a clean change tracker, so it repeats the whole unit without duplicating anything. |
| Expected errors as exceptions + one global handler | Result pattern | Keeps services short and every error response identical. The Result pattern is more explicit but needs checks at every call. |
| Data annotations for validation | FluentValidation | Built in and enough for these rules; business rules (category usable, stock range) are in the services. |
| Migrations applied on startup | Applied by the deployment pipeline (`dotnet ef migrations bundle`) | Simplest way to run locally. EF Core takes a lock, so several instances starting together are safe. Production would use a bundle. |
| `X-User-Id` header instead of authentication | JWT / cookies | Authentication is out of scope; the header only identifies who does what. `CurrentUser` is the single place to replace. |
| Categories: delete moves products to "Uncategorized" | Cascade delete, or block the delete | Nothing is lost. Uncategorized (Id 1) is protected and only visible to editors and admins, who can re-assign the products. |
| Disable instead of only delete | Hard delete only | Hiding is reversible. A product is visible when it **and** its category are active, so re-enabling a category restores its products. |
| Frontend served by nginx, forwarding `/api` | Browser calls the API on port 8080 | Same origin: no CORS, no API address in the frontend build. The same image runs everywhere; only `API_URL` changes. |
| Metrics aggregated in SQL | Send raw events to the browser | Small responses; the browser only draws. |
| Demo activity generated at startup | Activity in a migration | Metric dates must be relative to "now" (a 30-day chart must show the last 30 days); migrations can only hold fixed dates. |
| Error-only logging (level `Warning`) | Default logging | Only unexpected failures are logged (with trace id), ready for Application Insights. Expected errors go to the client, not the log; a client that disconnects is not an error. |
| Health check (`/api/health`) | A plain endpoint | ASP.NET Core health checks: `503` when the database doesn't answer, so a load balancer or orchestrator can react. |

## Assumptions

- Prices are in EUR with 2 decimals; there is no currency field.
- One warehouse; stock is a whole number between 0 and 1,000,000. Add/decrement accept 1-100,000 per call.
- Product names don't have to be unique; category names do (case-insensitive).
- IDs are never reused, also after a delete.
- "Return an OK response" for the stock endpoints: `200` with the updated product.
- `PUT` replaces the whole product (including stock), and must send the `rowVersion` it read.
- Search is a case-insensitive "contains" on the name. Stock level is inclusive; `min` and `max` are both optional.
- Lists are small enough not to need paging in the API; the UI pages client side.
- "Low stock" means 1 to 5 units.
- Dates are stored and returned in UTC; the UI shows local time.

## Where this differs from common practice

- **No authentication**, and the `X-User-Id` header can be faked on purpose (see above). Reads work anonymously, as a normal user.
- **Hidden statuses are silently ignored** for users who may not see them (normal users always get active products, instead of a 403), so one UI works for every role.
- **IDs 100000-100099 are reserved** for seed data; new products start at 100100. Seed data needs fixed IDs, and a fixed start keeps the sequence stable if seeds are added later.
- **Demo activity writes product history directly** (system versioning is switched off briefly, inside one transaction). Only the seeder does this, only once, and it is off by default.
- **Buttons use sentence case** ("New product"), following Angular Material rather than Title Case.
- **The xUnit analyzer rule xUnit1051 is off in tests** (pass the test cancellation token everywhere): it adds noise to short tests. Production code always passes cancellation tokens.

## Metrics

The Metrics tab has a **Products / Users** switch and a 7/30/90-day range. Both are kept in the URL (`/metrics?view=users&days=90`).

| View | Who | Shows | Data |
|---|---|---|---|
| Products | Editor, Admin | Inventory value, out of stock, low stock, units removed; units added vs removed per day (zoomable); stock level over time for up to 5 products (zoomable step line); top 10 products by units removed | `UserMetrics` (with `Quantity`), current products, `ProductsHistory` |
| Users | Admin only (per-user activity is personal data) | Active users, logins, edits, stock changes; activity per day (zoomable, stacked); actions per user; busiest hours (weekday × hour heatmap, local time) | `UserMetrics` |

Every chart has a table view with the exact values. Colours are validated for colour blindness; a series keeps the same colour in every chart.

**Adding a chart:** build a config and pass it to the reusable component. Theme, zoom, legend, tooltip, empty state, loading and table view come with it.

```ts
config: ChartConfig = { type: 'bar', horizontal: true, categories: ['Lens', 'Slides'], series: [{ name: 'Units', data: [12, 30] }] };
```

```html
<app-chart title="Units per product" subtitle="Last 30 days" [config]="config" [loading]="loading" />
```

Types: `timeline` (styles `line`, `area`, `bar`, `step`; zoomable), `bar` (vertical/horizontal, stacked) and `heatmap`. See `frontend/src/app/shared/chart/chart-config.ts`.

### Seeding

| What | How | When |
|---|---|---|
| 13 products, 7 categories, 4 users | `HasData` in migrations | Always, once |
| ~90 days of activity (logins, stock movements, price changes) and the matching product history | `DemoActivitySeeder`, setting `Seeding:DemoActivity` | On startup when there is no activity yet. On in `docker-compose.yml` and `launchSettings.json`, off in `appsettings.json` (tests, production) |

The demo activity is deterministic (fixed random seed). It is simulated backwards from today's real stock, so the history always ends exactly at the current values.

## API

All responses are JSON; enums are strings. Errors are always [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) with a `code`; errors about an input also name the field in `errors`, like model validation does:

```json
{ "status": 409, "title": "Not enough stock.", "detail": "Cannot remove 4 from product 100002: not enough stock.", "code": "InsufficientStock" }
{ "status": 400, "title": "Quantity is out of the allowed range.", "code": "InvalidQuantity", "errors": { "quantity": ["Quantity must be between 1 and 100000."] } }
```

Success codes: `201` with a `Location` header for creates, `204` for deletes, `200` with the resource otherwise.

| Method | Endpoint | Who |
|---|---|---|
| GET | `/api/products` (optional filters, see below) | everyone |
| GET | `/api/products/{id}` | everyone |
| POST | `/api/products` | Editor, Admin |
| PUT | `/api/products/{id}` | Editor, Admin |
| DELETE | `/api/products/{id}` | Admin |
| POST | `/api/products/{id}/decrement-stock/{quantity}` | any user |
| POST | `/api/products/{id}/add-to-stock/{quantity}` | any user |
| GET | `/api/products/search?name={name}` | everyone |
| GET | `/api/products/stock-level?min={min}&max={max}` | everyone |
| GET | `/api/products/{id}/history` | everyone |
| POST | `/api/products/{id}/enable` · `/disable` | Editor, Admin |
| GET / POST / PUT / DELETE | `/api/categories`, `/api/categories/{id}` | read: everyone · write: Editor, Admin · delete: Admin |
| POST | `/api/categories/{id}/enable` · `/disable` | Editor, Admin |
| GET | `/api/metrics/products?days=30`, `/api/metrics/products/stock-history?productIds=…&days=30` | Editor, Admin |
| GET | `/api/metrics/users?days=30` | Admin |
| GET | `/api/users`, POST `/api/auth/login` | everyone |

The first 9 rows are the endpoints from the assessment.

**Filtering `GET /api/products`:** every parameter is optional; different parameters combine with AND, repeated values of one parameter with OR. The UI uses this endpoint; `/search` and `/stock-level` stay as the assessment defines them and use the same filter internally.

| Parameter | Example | Notes |
|---|---|---|
| `search` | `search=lens` | part of the name, case-insensitive |
| `categoryIds` | `categoryIds=2&categoryIds=5` | |
| `statuses` | `statuses=Disabled` | `Active`, `Disabled`, `Uncategorized`, `CategoryDisabled`; non-active ones only for Editors and Admins |
| `stockStatuses` | `stockStatuses=LowStock` | `InStock`, `LowStock` (1-5), `OutOfStock` |
| `minStock`, `maxStock` | `minStock=10` | inclusive |
| `minPrice`, `maxPrice` | `maxPrice=300` | inclusive |

Every product response includes `status` and `stockStatus`, computed by the API, so badges and filters always agree.

## Running parts separately

Needs the .NET 10 SDK and Node 24. Backend (needs the database container):

```
docker compose up -d db
dotnet run --project backend/src/Products.Api
```

Frontend (`npm start` forwards `/api` to `http://localhost:8080`, like nginx does in Docker):

```
cd frontend
npm install
npm start
```

## Tests

Backend, from the repository root. The integration and acceptance tests start a real SQL Server with Testcontainers, so Docker must be running:

```
dotnet test
```

| Project | What it tests |
|---|---|
| `Products.UnitTests` | Business rules in the services with fake repositories (no database), validation, permissions, error mappings |
| `Products.IntegrationTests` | The API over HTTP against SQL Server: every endpoint, roles, edge cases, concurrency (parallel creates, parallel stock decrements, duplicate category names), metrics, demo seeding |
| `Products.AcceptanceTests` | BDD scenarios in plain English (Reqnroll / Gherkin) describing user outcomes |

Without Docker, only the unit tests run: `dotnet test --project backend/tests/Products.UnitTests`.

Frontend (Vitest, ESLint, Prettier):

```
cd frontend
npm test -- --watch=false
npm run lint
npm run format:check
```

GitHub Actions (`.github/workflows/ci.yml`) runs on pushes to `main` and on pull requests: all backend tests, frontend lint, format check, tests and build, then `docker compose up` of the whole stack with a check that the app, the API and the seeded data answer.

## Database

Tables: `Products` (+ `ProductsHistory`), `Categories`, `Users`, `UserMetrics`. The API applies migrations on startup. Data is kept in a Docker volume; `docker compose down -v` resets it.

Create a migration (needs `dotnet tool install -g dotnet-ef`):

```
dotnet ef migrations add <Name> --project backend/src/Products.Infrastructure --startup-project backend/src/Products.Api --output-dir Persistence/Migrations
```

## Configuration and secrets

The same Docker images run in every environment; only these values change:

| Setting | Where now | Production |
|---|---|---|
| `ConnectionStrings__Default` (API) | `appsettings.json`, `docker-compose.yml` | Secret store (e.g. Azure Key Vault), its own SQL login instead of `sa` |
| `MSSQL_SA_PASSWORD` (database container) | `docker-compose.yml` | Managed database (e.g. Azure SQL) instead of a container |
| `API_URL` (frontend nginx → API) | `frontend/Dockerfile` default, `docker-compose.yml` | Address of the API service |
| `Seeding__DemoActivity` | `docker-compose.yml`, `launchSettings.json` | Off |

The passwords in the repository are dev-only defaults, committed on purpose so the project runs out of the box. Nothing needs to be set up.

## Project structure

```
backend/
  src/                        Domain, Application, Infrastructure, Api (see Architecture)
  tests/
    Products.UnitTests/
    Products.IntegrationTests/
    Products.AcceptanceTests/
    Products.TestSupport/     shared by integration + acceptance tests (SQL Server container, API factory)
  Directory.Packages.props    every NuGet version in one place
frontend/
  src/app/
    core/                     enums, models, services, interceptors, guard
    layout/                   header
    features/                 products, categories, metrics pages
    shared/                   chart, stat tile, confirm dialog
  nginx/                      production web server config (template)
.github/workflows/ci.yml      tests, lint, builds and a full-stack check on pushes to main and PRs
docker-compose.yml
```

## Extras (own initiative)

Not asked for in the assessment. Added because they make the project closer to a real one.

- **Roles** (User, Editor, Admin) enforced in the API and reflected in the UI; user dropdown instead of a login page.
- **Categories**, with protected "Uncategorized"; **enable/disable** for products and categories.
- **Product history** (temporal table) with a History dialog; **RowVersion** conflict detection.
- **UserMetrics** and the **Metrics tab** with a reusable chart component and demo activity.
- **Error codes as enums** shared by API and UI; limits and messages in one place.
- **Swagger** with an Authorize box, **health endpoint**, **error-only logging**.
- **Docker Compose** for everything, nginx production frontend (non-root API container, basic security headers), **GitHub Actions CI** with lint and a full-stack smoke test.
- **Responsive UI**, URL keeps filters and views, skip link, table views for charts, reduced-motion support.
- **Integration tests against real SQL Server** including concurrency, and **BDD** scenarios.

## Known limitations and next steps

- Real authentication (JWT) to replace the `X-User-Id` header.
- Server-side paging and sorting for large catalogues.
- Failed attempts (e.g. not enough stock) are not recorded in the metrics.
- Application Insights (connection string + package); logs already contain only real failures.
- Migrations from the pipeline (`dotnet ef migrations bundle`) instead of on startup.
- Dark mode.
- A Content-Security-Policy header (the app loads Google Fonts and the charts inject styles, so it needs care).

## Development notes

Commits group finished, tested features rather than every intermediate step.
