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

## Deployment (Render + Vercel)

The app is deployed as two services:

| Component | Platform | Type |
|-----------|----------|------|
| **Backend** (ASP.NET Core 8 API) | [Render](https://render.com) | Docker Web Service |
| **Frontend** (React + Vite) | [Vercel](https://vercel.com) | Static Site |

### Deploy backend to Render

1. Push the repo to GitHub.
2. Go to [render.com](https://render.com) → **New** → **Blueprint**.
3. Connect the GitHub repo — Render auto-detects the `render.yaml` at the project root.
4. The blueprint creates the **inventory-api** web service using the `backend/Dockerfile`.
5. Set the `Cors__Origins__0` environment variable to your Vercel frontend URL (e.g. `https://your-app.vercel.app`).
6. Verify: `GET https://<your-service>.onrender.com/health` should return `200`.

**Environment variables set by `render.yaml`:**

| Variable | Purpose | Value |
|----------|---------|-------|
| `ConnectionStrings__DefaultConnection` | SQLite DB path | `Data Source=/data/inventory.db` |
| `Database__UseSqlite` | Use SQLite provider | `true` |
| `Jwt__SigningKey` | JWT signing secret | Auto-generated |
| `Jwt__Issuer` | JWT issuer | `InventoryManagement` |
| `Jwt__Audience` | JWT audience | `InventoryManagementClients` |
| `Cors__Origins__0` | Allowed frontend origin | Set after Vercel deploy |

> **Note:** A persistent `disk` is configured in `render.yaml` to prevent the SQLite database from resetting on each redeploy. Persistent disks require a paid Render plan. If you are on the free tier, you may need to remove the `disk` section from `render.yaml` or upgrade your plan.

### Deploy frontend to Vercel

1. Go to [vercel.com](https://vercel.com) → **Add New Project**.
2. Import the same GitHub repo.
3. Set **Root Directory** to `frontend`.
4. Add environment variable: `VITE_API_BASE_URL` = `https://<your-service>.onrender.com` (your Render backend URL).
5. Vercel auto-detects Vite and deploys. The `vercel.json` handles SPA rewrites.
6. Copy the Vercel URL and set it as `Cors__Origins__0` in the Render dashboard.

### Post-deploy checklist

- [ ] Backend `/health` returns `200`
- [ ] Frontend loads the login page
- [ ] Login with `admin@inventory.local` / `Admin123!` works
- [ ] CORS allows requests from the Vercel domain to the Render API
- [ ] Full CRUD operations work end-to-end

> **`ReverseProxy:UseForwardedHeaders`** is enabled in `appsettings.Production.json` so `X-Forwarded-*` headers from Render's load balancer are honored.

## Role capabilities

| Role | Typical access |
|------|------------------|
| **Viewer** | Read products, warehouses, inventory, orders (GET); dashboard and Low Stock alerts |
| **Manager** | Viewer + create/update/delete catalog, orders, manual stock adjustments |
| **Admin** | Manager + register users with elevated roles (standard registration defaults to Viewer) |

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

### Register user (Open)

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "newuser@company.com",
  "password": "SecurePass123!",
  "role": "Viewer"
}
```

## Hangfire

- Dashboard: `/hangfire` — in **Development**, open access; in **Production**, restricted to **Admin** role (see `HangfireDashboardAuthFilter`). For production, put the dashboard behind auth at the reverse proxy as well.

## Project layout

```
backend/
  Dockerfile              # Multi-stage Docker build for Render
  .dockerignore
  InventoryManagement.sln
  InventoryManagement.Api/
  InventoryManagement.Application/
  InventoryManagement.Domain/
  InventoryManagement.Infrastructure/
frontend/
  src/                    # React app (pages, api client, auth)
  vercel.json             # Vercel SPA config
  .env.development        # Local dev settings
  .env.production         # Production API URL
render.yaml               # Render Blueprint (auto-creates services)
README.md
```

## Further production hardening

- Replace `EnsureCreated` with **EF migrations** and automated `dotnet ef database update` in CI/CD.
- Store secrets in **Azure Key Vault**, **User Secrets** (dev), or environment variables.
- Add **rate limiting**, **HTTPS** termination, and structured logging (Serilog + Application Insights, etc.).
- Add integration tests for ledger concurrency and order workflows.
