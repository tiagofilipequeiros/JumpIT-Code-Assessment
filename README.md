# Product Exercise

## Versions

.NET: 10 (SDK 10.0.401)
Angular: 22.2.0
Node: 24
Angular Material: 22.2.1
Bootstrap: 5.3.8
EF Core: 10.0.12
SQL Server: 2025

## Run everything

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

## Run backend only

Needs the database container:

```
docker compose up -d db
cd backend
dotnet run
```

## Database

The API applies migrations on startup, so `docker compose up --build` always brings the database up to date.

Data is kept in a Docker volume. Reset everything:

```
docker compose down -v
```

Create a new migration (needs `dotnet tool install -g dotnet-ef`):

```
cd backend
dotnet ef migrations add <Name>
```

Seed data (12 products, IDs `100000`-`100011`) is inserted by a migration. IDs `100000`-`100099` are reserved for seed data; new products start at `100100`.

In production, migrations would run from the deployment pipeline (e.g. `dotnet ef migrations bundle`) instead of on startup.

## Run frontend only

```
cd frontend
npm install
npm start
```

## Secrets

Nothing to set up: the values below are dev-only defaults committed on purpose, so the project runs out of the box.

In a real deployment they would move to a secret store (e.g. Azure Key Vault, or pipeline secrets as environment variables), and the API would use its own SQL login instead of `sa`.

| Secret | Where it is now |
|---|---|
| SQL Server `sa` password | `docker-compose.yml` (`MSSQL_SA_PASSWORD`, healthcheck) |
| Database connection string | `backend/appsettings.json` (`ConnectionStrings:Default`), `docker-compose.yml` (`ConnectionStrings__Default`) |

## Decisions

### Database: SQL Server

Candidates were PostgreSQL and SQL Server (SQLite was ruled out because it is a single file and cannot safely serve multiple API instances).

Both solve the Product ID requirement the simple way: the database generates the ID from a sequence (`100000` to `999999`), so any number of API instances can create products without duplicates.

SQL Server was chosen: heavier Docker image, but production-proven and the usual Microsoft/.NET stack.

### Roles

| | User | Editor | Admin |
|---|---|---|---|
| View active products/categories | ✅ | ✅ | ✅ |
| Add / decrement stock | ✅ | ✅ | ✅ |
| Create / edit | ❌ | ✅ | ✅ |
| Enable / disable, see disabled | ❌ | ✅ | ✅ |
| See Uncategorized | ❌ | ✅ | ✅ |
| Delete | ❌ | ❌ | ✅ |
| Metrics tab | ❌ | TBD | ✅ |

## Extras (own initiative)

Not asked for in the assessment. Added because they make the project closer to a real one.

- **Swagger UI at `/docs`**: explore and test the API from the browser.
- **Health endpoint**: quick check that the API (and database) are up.
- **Docker Compose for everything**: one command runs the whole project.
- **SQL Server sequence for IDs**: the database guarantees unique IDs across any number of API instances.
- **Atomic stock updates**: a single SQL update, so two instances can never lose a change or go below 0.
- **Error codes as enums**: backend and frontend share the same codes; the UI reacts to the code, not the message text.
- **Limits as constants/enums** (e.g. name length): one place to change them, same values in API and form validation.
- **Error-only logging**: only unexpected failures are logged, ready for Application Insights.
- **Responsive UI**: works on phone and desktop.
- **RowVersion**: two people editing the same product get a 409 instead of silently overwriting each other.
- **Temporal history on Products**: SQL Server keeps every past version, so price and stock changes can be tracked.
- **Users and roles** (User, Editor, Admin): user dropdown instead of a login page; permissions enforced in API and UI.
- **Categories**: a real relationship in the schema; deleting one moves its products to Uncategorized instead of losing them.
- **Enable / disable**: hide products or categories without deleting them.
- **UserMetrics**: records who did what and when (Entity, Action, EntityId, Details), for the Metrics tab.

## Next steps

### Phase 1 - The brief

- [x] 1. Add EF Core + SQL Server (docker-compose)
- [x] 2. Product model (Id from sequence 100000-999999, Name, Description, Price, Stock, CreatedAt, UpdatedAt)
- [x] 3. First migration, applied on startup
- [x] 4. Seed products
- [x] 5. Error codes (enums) + ProblemDetails + global exception handler (log errors only)
- [x] 6. Product DTOs, ProductService, ProductsController
- [x] 7. GET products / GET product by ID
- [ ] 8. POST product + validation
- [ ] 9. PUT product + validation
- [ ] 10. DELETE product
- [ ] 11. Search by name (partial match)
- [ ] 12. Stock level (min / max)
- [ ] 13. Add to stock
- [ ] 14. Decrement stock (atomic, never below 0)
- [ ] 15. Backend unit + integration tests
- [ ] 16. Angular: products service, API URL in environment
- [ ] 17. Angular: header, responsive products table, search, stock filter
- [ ] 18. Angular: create / edit form, delete confirmation, stock buttons
- [ ] 19. Angular: error codes (enums), loading states, snackbar feedback
- [ ] 20. Frontend tests
- [ ] 21. README: run, test, decisions

### Phase 2 - Beyond the brief

- [ ] 22. RowVersion on Products (409 on conflicting edits)
- [ ] 23. Temporal history on Products (price / stock over time)
- [ ] 24. Users (4 seeded: 1 admin, 1 editor, 2 users) + login by user dropdown
- [ ] 25. Roles enforced in API (X-User-Id header) and UI
- [ ] 26. Categories (migration moves products to Uncategorized, Id 1)
- [ ] 27. Enable / disable products and categories
- [ ] 28. Category delete moves products to Uncategorized (with warning)
- [ ] 29. UserMetrics (Entity, Action, EntityId, Details)
- [ ] 30. Metrics tab (to be defined)
- [ ] 31. Optional: BDD tests
- [ ] 32. Optional: two backend instances to prove unique IDs
