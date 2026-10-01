# Product Exercise

Product management API (ASP.NET Core) with an Angular frontend.

## Versions

.NET: 10 (SDK 10.0.401)
Angular: 22.2
Node: 24
Angular Material: 22.2
Bootstrap: 5.3.8
EF Core: 10.0.12
SQL Server: 2025

## Run everything

Needs Docker.

```
docker compose up --build
```

Frontend:
http://localhost:4200

Backend:
http://localhost:8080

Health:
http://localhost:8080/api/health

Swagger:
http://localhost:8080/docs

## Users

There is no login page and no password (see [Decisions](#users-and-roles)). Pick a user in the header dropdown.

| Id | Name | Email | Role |
|---|---|---|---|
| 1 | Alex Admin | admin@example.com | Admin |
| 2 | Erin Editor | editor@example.com | Editor |
| 3 | Sam User | sam@example.com | User |
| 4 | Taylor User | taylor@example.com | User |

**In Swagger:** click **Authorize** and enter a user Id (`1` = Admin can do everything). Reads work without a user; changes need one.

## Run backend only

Needs the database container:

```
docker compose up -d db
cd backend
dotnet run
```

## Run frontend only

```
cd frontend
npm install
npm start
```

## Tests

Backend (unit + integration tests against a real SQL Server started by Testcontainers, so Docker must be running):

```
dotnet test
```

Frontend (Vitest):

```
cd frontend
npm test
```

## API

| Method | Endpoint | Who |
|---|---|---|
| GET | `/api/products` | everyone |
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
| GET / POST / PUT / DELETE | `/api/categories`, `/api/categories/{id}` | read: everyone, write: Editor, Admin, delete: Admin |
| POST | `/api/categories/{id}/enable` · `/disable` | Editor, Admin |
| GET | `/api/users` | everyone |
| POST | `/api/auth/login` | everyone |

The first 9 rows are the endpoints from the assessment. Every product response includes `stock`.

List endpoints accept `includeHidden=true`; it only has an effect for Editors and Admins.

Errors are always [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) with a `code`, e.g.:

```json
{ "status": 409, "title": "Not enough stock.", "detail": "Cannot remove 4 from product 100002: not enough stock.", "code": "InsufficientStock" }
```

## Database

The API applies migrations on startup, so `docker compose up --build` always brings the database up to date.

Data is kept in a Docker volume. Reset everything (database is recreated from migrations and seed data):

```
docker compose down -v
```

Create a new migration (needs `dotnet tool install -g dotnet-ef`):

```
cd backend
dotnet ef migrations add <Name>
```

Tables: `Products` (+ `ProductsHistory`), `Categories`, `Users`, `UserMetrics`.

Seed data: 13 products (IDs `100000`-`100012`), 7 categories, 4 users. IDs `100000`-`100099` are reserved for seed data; new products start at `100100`.

In production, migrations would run from the deployment pipeline (e.g. `dotnet ef migrations bundle`) instead of on startup.

## Secrets

Nothing to set up: the values below are dev-only defaults committed on purpose, so the project runs out of the box.

In a real deployment they would move to a secret store (e.g. Azure Key Vault, or pipeline secrets as environment variables), and the API would use its own SQL login instead of `sa`.

| Secret | Where it is now |
|---|---|
| SQL Server `sa` password | `docker-compose.yml` (`MSSQL_SA_PASSWORD`, healthcheck) |
| Database connection string | `backend/appsettings.json` (`ConnectionStrings:Default`), `docker-compose.yml` (`ConnectionStrings__Default`) |

## Project structure

```
backend/        ASP.NET Core API
  Controllers/  HTTP only, one line per action
  Services/     business logic
  Dtos/         request/response classes (validation rules here)
  Models/       EF Core entities and limits
  Data/         DbContext, seed data
  Migrations/   EF Core migrations (code first)
  Errors/       error codes, exception handler
  Auth/         current user, role permissions
tests/          backend tests (xUnit)
frontend/src/app/
  core/         enums, models, services, interceptors, guard
  layout/       header
  features/     products, categories, metrics pages
  shared/       confirm dialog
```

## Decisions

### Database: SQL Server

Candidates were PostgreSQL and SQL Server (SQLite was ruled out because it is a single file and cannot safely serve multiple API instances).

Both solve the Product ID requirement the simple way: the database generates the ID from a sequence (`100000` to `999999`), so any number of API instances can create products without duplicates.

SQL Server was chosen: heavier Docker image, but production-proven and the usual Microsoft/.NET stack.

### Product IDs

A SQL Server sequence (`ProductIds`, `100000`-`999999`, no cycle) is the default value of `Products.Id`. The database hands out each number exactly once, whichever API instance asks. A test creates 25 products in parallel and checks that all IDs are unique.

Alternative considered: random 6-digit number + unique index + retry. Works with any database, but collisions grow as the table fills.

### Stock changes

Add/decrement is one SQL `UPDATE ... SET Stock = Stock + @delta WHERE Id = @id AND Stock + @delta >= 0`. No read-then-write, so concurrent requests can never lose a change or go below 0. A test sends 10 parallel decrements on a stock of 3: exactly 3 succeed. A database check constraint (`Stock >= 0`) is the last line of defence.

### Concurrent edits

Products and categories have a `RowVersion`. PUT must send the version the client loaded; if someone saved in between, the API returns `409 ConcurrencyConflict` instead of silently overwriting. The UI then reloads the list.

### Errors and logging

- Expected errors (not found, not enough stock, ...) are thrown as `AppException(ErrorCode)` and turned into ProblemDetails by one global handler. They are not logged.
- Unexpected exceptions return a generic 500 and are logged with `LogError` (stack trace + traceId). Log level is `Warning`, so logs contain only real problems; ready for Application Insights.
- Error codes are enums on both sides (`ErrorCode.cs`, `error-code.ts`). The UI picks its message from the code, never from the text.

### Users and roles

No real authentication (out of scope). Picking a user in the dropdown calls `POST /api/auth/login` with the email and is recorded as a login. Requests then send `X-User-Id`; the API loads the user and checks the role on every call. The header can be faked on purpose: this only identifies who does what. Real authentication would replace `CurrentUser` with JWT/cookie claims.

| | User | Editor | Admin |
|---|---|---|---|
| View active products/categories | ✅ | ✅ | ✅ |
| Add / decrement stock | ✅ | ✅ | ✅ |
| Create / edit | ❌ | ✅ | ✅ |
| Enable / disable, see hidden | ❌ | ✅ | ✅ |
| Delete | ❌ | ❌ | ✅ |
| Metrics tab | ❌ | TBD | ✅ |

The role → permission map exists only in the backend (`Permissions.cs`); the login response includes the user's permissions and the UI uses those. Hiding a button is never the only check.

### Delete vs disable

- **Disable** hides a product or category from normal users without losing anything. A product is visible when it is active **and** its category is active, so re-enabling a category brings its products back as they were.
- **Delete** (Admin only) is permanent. Deleting a category moves its products to the protected **Uncategorized** category (Id 1, cannot be renamed, disabled or deleted), which only Editors and Admins see.
- `UserMetrics.EntityId` has no foreign key on purpose, so the history survives deletes.

### History and metrics

- `Products` is a SQL Server **temporal table**: every change keeps the previous version in `ProductsHistory` automatically (price and stock over time, shown in the History dialog).
- `UserMetrics` records who did what: `Entity` (User, Product, Category) + `Action` (Login, Create, Update, Delete, Enable, Disable, AddStock, DecrementStock) + `EntityId` + readable `Details` (e.g. `Stock +5 (12 → 17)`). Enums are stored as text, so new values need no migration.

### Small but deliberate

- Dates are stored and returned in UTC (`...Z`).
- Validation limits and formatting use the invariant culture, so a server with a decimal comma behaves the same (a test caught this).
- Normal users asking for hidden items silently get the normal view (not an error), so the same UI code works for every role.
- Search and stock-level are separate filters in the UI, each calling its own endpoint from the assessment.
- No pagination on the API (the assessment's endpoints return lists); the UI pages client side. A larger catalogue would need server-side paging.
- The frontend Docker image runs the Angular dev server for simplicity; production would serve the built files with nginx.

## Extras (own initiative)

Not asked for in the assessment. Added because they make the project closer to a real one.

- **Swagger UI at `/docs`**: explore and test the API from the browser, with an Authorize box for the user.
- **Health endpoint**: quick check that the API and database are up.
- **Docker Compose for everything**: one command runs database, backend and frontend.
- **SQL Server sequence for IDs**: the database guarantees unique IDs across any number of API instances.
- **Atomic stock updates**: a single SQL update, so two instances can never lose a change or go below 0.
- **Error codes as enums**: backend and frontend share the same codes; the UI reacts to the code, not the message text.
- **Limits, messages and feedback as enums/constants**: one place to change them, same values in API and form validation.
- **Error-only logging**: only unexpected failures are logged, ready for Application Insights.
- **Responsive UI**: works on phone and desktop.
- **RowVersion**: two people editing the same item get a 409 instead of silently overwriting each other.
- **Temporal history on Products**: SQL Server keeps every past version, so price and stock changes can be tracked.
- **Users and roles** (User, Editor, Admin): user dropdown instead of a login page; permissions enforced in API and UI.
- **Categories**: a real relationship in the schema; deleting one moves its products to Uncategorized instead of losing them.
- **Enable / disable**: hide products or categories without deleting them.
- **UserMetrics**: records who did what and when, for the Metrics tab.
- **Integration tests against real SQL Server**, including concurrency tests for IDs and stock.

## Checklist

### Phase 1 - The brief

- [x] 1. Add EF Core + SQL Server (docker-compose)
- [x] 2. Product model (Id from sequence 100000-999999, Name, Description, Price, Stock, CreatedAt, UpdatedAt)
- [x] 3. First migration, applied on startup
- [x] 4. Seed products
- [x] 5. Error codes (enums) + ProblemDetails + global exception handler (log errors only)
- [x] 6. Product DTOs, ProductService, ProductsController
- [x] 7. GET products / GET product by ID
- [x] 8. POST product + validation
- [x] 9. PUT product + validation
- [x] 10. DELETE product
- [x] 11. Search by name (partial match)
- [x] 12. Stock level (min / max)
- [x] 13. Add to stock
- [x] 14. Decrement stock (atomic, never below 0)
- [x] 15. Backend unit + integration tests
- [x] 16. Angular: products service, API URL in environment
- [x] 17. Angular: header, responsive products table, search, stock filter
- [x] 18. Angular: create / edit form, delete confirmation, stock buttons
- [x] 19. Angular: error codes (enums), loading states, snackbar feedback
- [x] 20. Frontend tests
- [x] 21. README: run, test, decisions

### Phase 2 - Beyond the brief

- [x] 22. RowVersion on Products (409 on conflicting edits)
- [x] 23. Temporal history on Products (price / stock over time)
- [x] 24. Users (4 seeded: 1 admin, 1 editor, 2 users) + login by user dropdown
- [x] 25. Roles enforced in API (X-User-Id header) and UI
- [x] 26. Categories (migration moves products to Uncategorized, Id 1)
- [x] 27. Enable / disable products and categories
- [x] 28. Category delete moves products to Uncategorized (with warning)
- [x] 29. UserMetrics (Entity, Action, EntityId, Details)
- [ ] 30. Metrics tab (to be defined; page exists as a placeholder, data is already recorded)
- [ ] 31. Optional: BDD tests
- [ ] 32. Optional: two backend instances to prove unique IDs (covered by the parallel-create test)
