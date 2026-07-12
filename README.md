# RestaurantPOS — Production-Ready Restaurant & Grocery POS System

A full-featured **Foodics-inspired** Point-of-Sale system built with **ASP.NET Core 8**, **Razor Pages**, **Clean Architecture**, and **Modular Monolith** principles.

---

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 8, Razor Pages, REST API |
| Architecture | Clean Architecture, Modular Monolith, CQRS (MediatR) |
| ORM | Entity Framework Core 8 (Code-First) |
| Database | SQL Server |
| Auth | ASP.NET Identity, JWT, Role-Based Authorization |
| Caching | Redis / In-Memory |
| Background Jobs | Hangfire |
| Real-time | SignalR |
| Logging | Serilog |
| Validation | FluentValidation |
| Frontend | Bootstrap 5, HTMX, jQuery |
| e-Invoicing | Saudi ZATCA Phase 2 |
| Containerization | Docker + Docker Compose |
| Exports | Excel (ClosedXML), PDF |

---

## Project Structure

```
RestaurantPOS/
├── RestaurantPOS.Domain/          # Entities, Interfaces, Enums (no dependencies)
├── RestaurantPOS.Application/     # CQRS Commands/Queries, Validators, Behaviours
├── RestaurantPOS.Infrastructure/  # EF Core, Repositories, External Services
├── RestaurantPOS.Shared/          # DTOs, Result Pattern, Constants
├── RestaurantPOS.Web/             # Razor Pages UI (Bootstrap 5 + HTMX)
└── RestaurantPOS.API/             # REST API with Swagger/JWT
```

---

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server) or Docker
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (optional)

### Option 1 — Docker Compose (Recommended)

```bash
git clone <repo-url>
cd RestaurantPOS
docker-compose up -d
# Web UI:  http://localhost:5000
# API:     http://localhost:5001
# Swagger: http://localhost:5001/swagger
```

### Option 2 — Local Development

1. Update `RestaurantPOS.Web/appsettings.json` connection string with your SQL Server password
2. Apply migrations: `dotnet ef database update --project RestaurantPOS.Infrastructure --startup-project RestaurantPOS.Web`
   - EF Core is configured to generate SQL Server 2017-compatible SQL (compatibility level 140) for migrations and runtime queries
3. Run web: `dotnet run --project RestaurantPOS.Web`
4. Run API: `dotnet run --project RestaurantPOS.API`

---

## Default Credentials

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@restaurantpos.com | Admin@123456! |

---

## Features

### Restaurant
- 🍽️ Dine-in, Take-away, Delivery, Drive-through
- 🪑 Table management with section map
- 📺 Kitchen Display System (KDS) with real-time SignalR updates
- 💰 Split bill, modifiers, combos, discounts, coupons, tips
- 📱 Touch-screen POS with barcode scanner support

### Inventory
- 🏭 Multi-warehouse inventory with FIFO costing
- 📊 Low stock alerts, waste tracking, purchase orders
- 🔄 Stock transfers, stock counts, adjustments

### Finance & Reports
- 💵 Shift management, cash drawer, expense tracking
- 📈 Sales, VAT, top products, daily breakdown reports
- 📤 Excel export

### Saudi ZATCA e-Invoicing
- 🇸🇦 Phase 2 compliant UBL 2.1 XML generation
- 📱 TLV QR code, digital signing, B2B/B2C APIs

---

## API Endpoints (Swagger at `/swagger`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Authenticate, get JWT |
| GET | `/api/products` | List products (paged) |
| POST | `/api/orders` | Create order |
| POST | `/api/orders/{id}/payment` | Process payment |
| GET | `/api/customers` | List customers |
| GET | `/api/inventory/stock` | Stock levels |
| POST | `/api/inventory/adjust` | Stock adjustment |
| GET | `/api/reports/sales-summary` | Sales summary |

---

## Build & Migrations

```bash
# Build
dotnet build RestaurantPOS.sln

# Add migration
dotnet ef migrations add <Name> --project RestaurantPOS.Infrastructure --startup-project RestaurantPOS.Web --output-dir Data/Migrations

# Apply to database
dotnet ef database update --project RestaurantPOS.Infrastructure --startup-project RestaurantPOS.Web
```

---

## Production Deployment Checklist

- [ ] Replace all `******` password placeholders with real secrets
- [ ] Set strong `Jwt__Key` (minimum 32 chars)
- [ ] Configure HTTPS certificates
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Configure ZATCA certificates for e-invoicing compliance
- [ ] Set up Redis for distributed caching
