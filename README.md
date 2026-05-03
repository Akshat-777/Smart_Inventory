# Inventory Management System

Full-stack inventory system with an **ASP.NET Core 8** Web API (clean architecture) and a **React + TypeScript (Vite)** client.

## Quick start (Windows)

1. Install **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** (or: `winget install --id Microsoft.DotNet.SDK.8 -e`).
2. Install **Node.js 20+** (`winget install OpenJS.NodeJS.LTS` or from nodejs.org).
3. **Database:** In **Development**, the API uses **SQLite** (`inventory-dev.db` next to the API project) — **no SQL Server install required** for local runs. Set `"Database:UseSqlite": false` and use a SQL Server connection string when you want full SQL Server + Hangfire (see `appsettings.json`).
4. From the repo root: **`.\run-dev.ps1`** (starts API + Vite), or run the API and `npm run dev` manually (below).

Passwords are stored with **BCrypt** (works the same on SQLite and SQL Server).

> **Verified:** `dotnet build`, API `dotnet run` with SQLite, login `admin@inventory.local` / `Admin123!`, and `npm run build` for the frontend.

## Architecture (backend)

| Layer | Projects |
|--------|----------|
| **API** | `InventoryManagement.Api` — controllers, middleware, DI composition, Hangfire, Swagger |
| **Application** | `InventoryManagement.Application` — DTOs, service interfaces, pagination, domain exceptions |
| **Domain** | `InventoryManagement.Domain` — entities, enums |
| **Infrastructure** | `InventoryManagement.Infrastructure` — EF Core, repositories, services, JWT, background jobs |

**Design choices**

- **Stock changes** always go through `IInventoryLedgerService` (creates a `StockMovement` and updates `InventoryItem` in the same `SaveChanges`).
- **Optimistic concurrency** on `Product`, `Warehouse`, and `InventoryItem` via `RowVersion` (retries in the ledger on `DbUpdateConcurrencyException`).
- **Orders** complete/fulfill inside a **database transaction** so either all lines post or none do.
- **Auth** uses **JWT** with a simple `UserAccount` table and `PasswordHasher` (no ASP.NET Identity UI), roles: `Admin`, `Manager`, `Viewer`.
- **Audit** rows are queued on the same `DbContext` as business changes; `SaveChanges` persists them together.
- **Hangfire** (SQL Server storage) runs a **daily low-stock** check (logs; extend to email/Slack).

## Prerequisites

- **.NET 8 SDK** — [Download](https://dotnet.microsoft.com/download)
- **Node.js 20+** and npm (for the frontend)
- **SQL Server** — LocalDB (Windows), full SQL Server instance, or another hosted SQL Server when `Database:UseSqlite` is `false`

## Run the API (local)

```bash
cd backend
dotnet restore
dotnet run --project InventoryManagement.Api
```

- Default dev URL: `https://localhost:7188` and `http://localhost:5188` (see `Properties/launchSettings.json`).
- Swagger: `/swagger` (Development only).
- On first run, the app calls `EnsureCreated` to build the schema and seeds an **admin** user (see below).

**Connection string** — set `ConnectionStrings:DefaultConnection` in `InventoryManagement.Api/appsettings.Development.json` (SQLite / LocalDB) or `appsettings.json` / environment variables for SQL Server and production.

**Default admin (seeded)**

- Email: `admin@inventory.local`
- Password: `Admin123!`
- Role: `Admin`

> For production, switch to **EF Core migrations** instead of `EnsureCreated`, rotate **JWT** `SigningKey`, and harden CORS, Hangfire dashboard access, and SQL credentials.

## Run the frontend (local)

```bash
cd frontend
npm install
npm run dev
```

- UI: `http://localhost:5173`
- The Vite dev server **proxies** `/api` to `http://localhost:5188` (see `frontend/vite.config.ts`).  
  If the API runs on another host/port, set `VITE_API_BASE_URL` in `frontend/.env.development`.

Example `frontend/.env.development`:

```env
# Leave empty to use the Vite proxy (same origin /api)
VITE_API_BASE_URL=

# Or point directly at the API:
# VITE_API_BASE_URL=http://localhost:5188
```

## Production hosting (no Docker)

- **API:** `dotnet publish` the `InventoryManagement.Api` project and run it under IIS, raw Kestrel, or a platform App Service. Set `ASPNETCORE_ENVIRONMENT=Production`, the SQL connection string, JWT signing key, and CORS origins via environment variables or `appsettings.Production.json`.
- **Frontend:** run `npm run build` in `frontend/` and deploy the `frontend/dist` folder as static files (IIS static site, nginx, CDN, etc.). If the UI is served from a **different origin** than the API, set `VITE_API_BASE_URL` when building to your public API URL; otherwise configure your web server to proxy `/api` to the API (similar to Vite’s dev proxy).
- **`ReverseProxy:UseForwardedHeaders`** is enabled in `appsettings.Production.json` so `X-Forwarded-*` headers from IIS, nginx, or a load balancer are honored when TLS terminates in front of Kestrel.

Production builds leave the login fields empty; local `npm run dev` still pre-fills the seeded admin credentials.

## Role capabilities

| Role | Typical access |
|------|------------------|
| **Viewer** | Read products, warehouses, inventory, orders (GET); dashboard |
| **Manager** | Viewer + create/update/delete catalog, orders, manual stock adjustments |
| **Admin** | Manager + register users (`POST /api/auth/register`) |

## Sample API requests

Base URL examples use `http://localhost:5188`. Send `Authorization: Bearer <token>` for protected endpoints.

### Login

**Request**

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@inventory.local",
  "password": "Admin123!"
}
```

**Response** (`200`)

```json
{
  "token": "<jwt>",
  "expiresAt": "2026-05-03T12:00:00Z",
  "email": "admin@inventory.local",
  "roles": ["Admin"]
}
```

### Create product (Manager/Admin)

```http
POST /api/products
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "Widget A",
  "sku": "WID-A-01",
  "category": "Parts",
  "price": 19.99,
  "lowStockThreshold": 15
}
```

### List inventory (paginated + filters)

```http
GET /api/inventory?page=1&pageSize=20&search=widget&productId=<guid>&warehouseId=<guid>
Authorization: Bearer <token>
```

### Manual adjustment (creates `StockMovement`, updates stock)

```http
POST /api/stockmovements/adjust
Authorization: Bearer <token>
Content-Type: application/json

{
  "productId": "<guid>",
  "warehouseId": "<guid>",
  "quantityChange": 10,
  "reason": "Cycle count correction"
}
```

### Purchase order → receive stock

```http
POST /api/orders/purchase
Authorization: Bearer <token>
Content-Type: application/json

{
  "lines": [
    { "productId": "<guid>", "warehouseId": "<guid>", "quantity": 50 }
  ]
}
```

Then complete (posts inventory + movements in one transaction):

```http
POST /api/orders/purchase/<orderId>/complete
Authorization: Bearer <token>
```

### Sales order → ship stock

```http
POST /api/orders/sales
Authorization: Bearer <token>
Content-Type: application/json

{
  "lines": [
    { "productId": "<guid>", "warehouseId": "<guid>", "quantity": 5 }
  ]
}
```

```http
POST /api/orders/sales/<orderId>/fulfill
Authorization: Bearer <token>
```

### Low stock

```http
GET /api/products/low-stock
Authorization: Bearer <token>
```

### Dashboard summary

```http
GET /api/dashboard/summary
Authorization: Bearer <token>
```

### Register user (Admin only)

```http
POST /api/auth/register
Authorization: Bearer <token>
Content-Type: application/json

{
  "email": "manager@company.com",
  "password": "SecurePass123!",
  "role": "Manager"
}
```

## Hangfire

- Dashboard: `/hangfire` — in **Development**, open access; in **Production**, restricted to **Admin** role (see `HangfireDashboardAuthFilter`). For production, put the dashboard behind auth at the reverse proxy as well.

## Project layout

```
backend/
  InventoryManagement.sln
  InventoryManagement.Api/
  InventoryManagement.Application/
  InventoryManagement.Domain/
  InventoryManagement.Infrastructure/
frontend/
  src/   # React app (pages, api client, auth)
README.md
```

## Further production hardening

- Replace `EnsureCreated` with **EF migrations** and automated `dotnet ef database update` in CI/CD.
- Store secrets in **Azure Key Vault**, **User Secrets** (dev), or environment variables.
- Add **rate limiting**, **HTTPS** termination, and structured logging (Serilog + Application Insights, etc.).
- Add integration tests for ledger concurrency and order workflows.
