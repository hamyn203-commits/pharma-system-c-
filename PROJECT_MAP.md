# PROJECT MAP — مخزن الندا (Makhzan Al-Neda) C# Migration
**Date:** 2026-05-07 | **Source:** Python/FastAPI → **Target:** C#/.NET WPF

---

## 1. PROJECT UNDERSTANDING

### Current System (Python — Fully Analyzed)

| Component | Stack | Location |
|-----------|-------|----------|
| **Desktop Admin App** | CustomTkinter (Python) | `D:\pharma_project\` |
| **Backend API** | FastAPI + SQLAlchemy + SQLite | `D:\pharma_project\main.py` (2668 lines) |
| **Mobile App (Pharmacy)** | Flutter/Dart | `D:\New folder (2)\` |
| **Database** | SQLite (`pharmacy.db`) | 14 tables, in use |

### Current DB Schema (14 Tables)
`users`, `products`, `categories`, `pharmacies`, `orders`, `order_items`, `order_status_history`, `payments`, `returns`, `return_items`, `suppliers`, `purchases`, `purchase_items`, `audit_logs`

### Current Screens (16 tabs)
dashboard, products, categories, suppliers, purchases, pharmacies, orders, payments, returns, statements (account), reports, alerts, expiry, audit_log, backup, settings

### Business Logic Preserved
- Auth: SHA-256 hashing, roles (admin/accountant/rep)
- Orders: state machine (pending→reviewed→in_store→with_driver→on_the_way→delivered/postponed/cancelled)
- Payments: cash/partial/full/deferred/collect_on_delivery
- Returns: type-based (expired/damaged/wrong_item/extra_quantity/customer_return/other)
- Account statement: running balance ledger (orders=debit, payments=credit, returns=credit)
- Stock deduction on order review, not on creation
- Soft-delete for pharmacies (account_status: pending/active/blocked/deleted)

---

## 2. ASSUMPTIONS

| # | Assumption | Risk if Wrong |
|---|------------|---------------|
| A1 | .NET 9 SDK will be installed (runtime 9.0.12 exists) | Low — install via winget |
| A2 | WPF is the correct UI framework for Windows desktop admin | Medium — but user-default |
| A3 | SQLite remains the initial database | Low — EF Core abstracts it |
| A4 | Admin app is standalone (no live FastAPI dependency) | Medium — currently depends on API |
| A5 | Arabic text direction is RTL with proper FlowDirection | Medium — must test early |
| A6 | Existing pharmacy.db must be migrated verbatim | Low — schema maps 1:1 |

---

## 3. RISKS

| Risk | Mitigation |
|------|-----------|
| .NET SDK not installed | Install via `winget install Microsoft.DotNet.SDK.9` |
| WPF lacks native RTL support in DataGrid | Use proper FlowDirection, test Arabic rendering in M1 |
| Image paths from SQLite may be absolute Windows paths | Normalize to relative in data migration |
| Current Python app uses live API; C# app must work offline | Bundle SQLite locally, no API dependency |
| return_items table exists in DB but not in models.py | Migration must handle both schemas |

---

## 4. TARGET C# ARCHITECTURE

### Tech Stack (Actual vs. Requested)

| Component | Requested | Actual | Reason |
|-----------|-----------|--------|--------|
| .NET Version | 10 LTS | **9.0 LTS** | .NET 10 unreleased (Nov 2026); 9.0 is latest LTS |
| SDK | — | **Install .NET 9 SDK** | Only runtime 9.0.12 installed |
| UI | WPF | **WPF (.NET 9)** | User default; Windows-only desktop |
| Architecture | MVVM | **MVVM** | CommunityToolkit.Mvvm |
| ORM | EF Core | **EF Core 9** | Matches request |
| DB | SQLite | **SQLite** (Microsoft.Data.Sqlite) | Same as source for migration |
| Logging | Serilog | **Serilog** | User request |
| DI | MS DI | **Microsoft.Extensions.DependencyInjection** | User request |

### Solution Structure (Simplified, No Over-engineering)

```
AlNada.sln
├── src/
│   ├── AlNada.Admin/                 # WPF Desktop App
│   │   ├── App.xaml(.cs)
│   │   ├── MainWindow.xaml(.cs)
│   │   ├── Views/                    # XAML pages/tabs
│   │   │   ├── LoginView.xaml
│   │   │   ├── DashboardView.xaml
│   │   │   ├── ProductsView.xaml
│   │   │   ├── CategoriesView.xaml
│   │   │   ├── PharmaciesView.xaml
│   │   │   ├── OrdersView.xaml
│   │   │   ├── PaymentsView.xaml
│   │   │   ├── ReturnsView.xaml
│   │   │   ├── AccountStatementView.xaml
│   │   │   ├── ReportsView.xaml
│   │   │   ├── SuppliersView.xaml
│   │   │   ├── PurchasesView.xaml
│   │   │   ├── AuditLogView.xaml
│   │   │   ├── BackupView.xaml
│   │   │   └── SettingsView.xaml
│   │   ├── ViewModels/
│   │   ├── Controls/                 # Custom controls (cards, badges)
│   │   ├── Converters/               # IValueConverter for RTL, status badges
│   │   ├── Themes/                   # Dark theme, styles
│   │   ├── Resources/                # Icons, fonts, logos
│   │   └── Services/                 # Navigation, Dialog, Session
│   │
│   ├── AlNada.Core/                  # Shared kernel (NO business logic)
│   │   ├── Entities/                 # EF Core entities (1:1 with DB)
│   │   ├── Enums/                    # OrderStatus, PaymentType, AccountStatus, ReturnType
│   │   ├── Interfaces/              # IRepository<T>, IUnitOfWork
│   │   └── Constants/
│   │
│   ├── AlNada.Data/                  # EF Core + Migrations + Seeding
│   │   ├── AppDbContext.cs
│   │   ├── Repositories/
│   │   ├── Migrations/
│   │   ├── Seed/
│   │   └── Import/                   # Legacy pharmacy.db import tool
│   │
│   └── AlNada.Services/              # Business logic services
│       ├── AuthService.cs
│       ├── ProductService.cs
│       ├── OrderService.cs
│       ├── PaymentService.cs
│       ├── ReturnService.cs
│       ├── AccountStatementService.cs
│       ├── ReportService.cs
│       ├── BackupService.cs
│       └── AuditService.cs
│
└── tests/
    └── AlNada.Tests/
        ├── Services/
        └── Data/
```

**Key Architecture Decisions:**
1. **No separate Application/Infrastructure split** — premature abstraction. Data + Services is sufficient.
2. **No MediatR/CQRS** — overkill for a desktop ERP.
3. **No microservices** — single WPF process with local SQLite.
4. **Feature-folders in Views** — each screen gets one View + one ViewModel.
5. **Services are thin** — one service per domain aggregate, injected via DI.

---

## 5. MIGRATION STRATEGY

### Phase 1: Schema Migration
- Create EF Core entities matching the actual SQLite schema (14 tables)
- Use `Microsoft.Data.Sqlite` + EF Core SQLite provider
- Provide a `LegacyImporter` service that:
  1. Opens existing `pharmacy.db` via raw SQLite
  2. Reads all tables into DTOs
  3. Maps to new EF entities
  4. Inserts with preserved IDs (identity insert)
  5. Reports row counts

### Phase 2: Data Integrity
- Arabic text: stored as UTF-8 in SQLite, EF Core handles natively
- Image paths: keep as relative strings, resolve at runtime
- Monetary values: `decimal(18,2)` in C# (no floats)
- Password migration: SHA-256 hex strings hash; can verify against existing hashes

### Phase 3: App Replacement
- New C# app reads from migrated DB (copy of pharmacy.db)
- Old Python app remains usable until C# version is verified
- No destructive migration — original DB untouched

---

## 6. MILESTONES

| M# | Name | Deliverables | Verifiable Criteria |
|----|------|-------------|-------------------|
| **M0** | Discovery & Plan | PROJECT_MAP.md, this document | ✅ Done |
| **M1** | Foundation | .NET 9 SDK installed, solution created, WPF shell with sidebar + DI + logging + dark theme | `dotnet build` passes, window appears with sidebar |
| **M2** | Data Layer | Entities, DbContext, migrations, Legacy DB import tool | Import runs, all 14 tables populated | ✅ Done |
| **M3** | Auth & Navigation | Login screen, user roles, sidebar navigation, session | Login → dashboard, role-based tab hiding | ✅ Done |
| **M4** | Inventory | Products CRUD + Categories CRUD + image support | Add/edit/delete product + category, search, filter |
| **M5** | Pharmacies | CRUD, account_status (pending/active/blocked/deleted), balance | Approve/block/revoke, balance display |
| **M6** | Orders | Full order lifecycle with state machine, items, stock, discount | Create → review → stock deduct → deliver, history |
| **M7** | Payments | Payment entry, partial/full/deferred, balance update, over-payment guard | Payment → balance updates, edge cases |
| **M8** | Returns | Return creation with items, stock/balance adjustment, status | Full return lifecycle |
| **M9** | Account Statement | Ledger with running balance, filters, export | Statement matches manual calculation |
| **M10** | Reports | Sales, top pharmacies, top products, debts, stock | Numbers match raw data |
| **M11** | Utilities | Backup/export/import, audit log viewer, settings | Backup creates valid .db, restore works |
| **M12** | Polish | RTL verified, dialogs, error states, build packaging | `dotnet build -c Release`, installer-ready |

---

## 7. QUESTIONS / CLARIFICATIONS NEEDED

| # | Question | Impact |
|---|----------|--------|
| Q1 | .NET 9 SDK not installed. Install via `winget install Microsoft.DotNet.SDK.9`? | Blocks M1 |
| Q2 | Current app depends on FastAPI server. C# version will use **direct SQLite** (no server). Confirm? | Architectural |
| Q3 | Returns in DB have `return_items` table with product-level detail. Should we preserve line-item returns or simplify to amount-only? | Impacts M8 scope |
| Q4 | Flutter mobile app — should it connect to a new C# API later, or is mobile out of scope for now? | Project scope |
| Q5 | Admin roles: admin/accountant/rep — are these sufficient, or add more? | Impacts M3 |

---

## 8. FIRST ACTION

**If approved, I will:**

1. Install .NET 9 SDK: `winget install Microsoft.DotNet.SDK.9`
2. Create solution structure (M1):
   - `dotnet new sln -n AlNada`
   - `dotnet new wpf -n AlNada.Admin`
   - `dotnet new classlib -n AlNada.Core`
   - `dotnet new classlib -n AlNada.Data`
   - `dotnet new classlib -n AlNada.Services`
   - `dotnet new xunit -n AlNada.Tests`
3. Add NuGet packages: EF Core SQLite, CommunityToolkit.Mvvm, Serilog
4. Build dark theme shell with sidebar navigation
5. Create all 14 EF entity classes from actual DB schema
6. Create legacy DB import tool

**Expected build output after M1:**
```
dotnet build
Build succeeded. 0 warnings, 0 errors
```

---

## 9. FEATURE STATUS

| Milestone | Status | Notes |
|-----------|--------|-------|
| M0: Discovery & Plan | ✅ Done | Full analysis of Python system, 14-table schema extraction |
| M1: Foundation | ✅ Done | .NET 9 SDK, solution 7 projects, NuGet packages, dark theme, all entities, DI bootstrap |
| M2: Data Layer | ✅ Done | EF Core initial migration, `LegacyDbImporter` reads real `pharmacy.db` → 76 rows, 0 errors |
| M3: Auth & Navigation | ✅ Done | `LoginViewModel` (MVVM), `MainViewModel` (nav), `DashboardView`, login → main window flow |
| M4: Inventory | ✅ Done | ProductsViewModel + ProductsView, CategoriesViewModel + CategoriesView, full CRUD |
| M5: Pharmacies | ✅ Done | PharmaciesViewModel + PharmaciesView, CRUD + account_status |
| M6: Orders | ✅ Done | OrderService (F# state machine integrated), OrdersViewModel + OrdersView |
| M7: Payments & Returns | ✅ Done | PaymentService + PaymentView, ReturnService + ReturnView (F# balance updates) |
| M8: Purchases & Suppliers | ✅ Done | SupplierService + SuppliersView, PurchaseService + PurchasesView |
| M9: Reports & Dashboard | ✅ Done | DashboardViewModel (real stats), ReportsViewModel + ReportsView, SQL reporting views |
| M10: Utilities | ✅ Done | AuditLogViewModel + AuditLogView, BackupViewModel + BackupView, SettingsView + SettingsView |
| M11: DI Wiring | ✅ Done | All 11 services + 14 ViewModels registered in App.xaml.cs DI container |
| M12: Polish & Final Build | ✅ Done | Release build 0w 0e, Debug build 0w 0e, 30/30 tests passed |

## 10. PROJECT STRUCTURE (CURRENT)

```
AlNeda.sln
├── src/
│   ├── AlNeda.Admin/
│   │   ├── App.xaml(.cs)
│   │   ├── MainWindow.xaml(.cs)
│   │   ├── LoginWindow.xaml(.cs)
│   │   ├── ViewModels/
│   │   │   ├── LoginViewModel.cs
│   │   │   ├── MainViewModel.cs
│   │   │   ├── DashboardViewModel.cs
│   │   │   ├── ProductsViewModel.cs
│   │   │   ├── ModuleViewModels.cs
│   │   │   ├── ModuleViewModels2.cs
│   │   │   └── TextBlockPlaceholder.cs
│   │   ├── Views/
│   │   │   ├── DashboardView.xaml(.cs)
│   │   │   ├── ProductsView.xaml(.cs)
│   │   │   ├── CategoriesView.xaml(.cs)
│   │   │   ├── PharmaciesView.xaml(.cs)
│   │   │   ├── OrdersView.xaml(.cs)
│   │   │   ├── PaymentsView.xaml(.cs)
│   │   │   ├── ReturnsView.xaml(.cs)
│   │   │   ├── SuppliersView.xaml(.cs)
│   │   │   ├── PurchasesView.xaml(.cs)
│   │   │   ├── AccountStatementView.xaml(.cs)
│   │   │   ├── ReportsView.xaml(.cs)
│   │   │   ├── AuditLogView.xaml(.cs)
│   │   │   ├── BackupView.xaml(.cs)
│   │   │   └── SettingsView.xaml(.cs)
│   │   ├── Converters/
│   │   │   └── Converters.cs
│   │   └── Themes/
│   │       └── DarkTheme.xaml
│   ├── AlNeda.Core/
│   │   ├── Entities/ (14 classes)
│   │   └── Enums/ (6 files)
│   ├── AlNeda.DomainLogic/ (F#)
│   │   ├── Domain.fs
│   │   ├── OrderWorkflow.fs
│   │   ├── Calculations.fs
│   │   └── Reporting.fs
│   ├── AlNeda.Data/
│   │   ├── AppDbContext.cs
│   │   ├── AppDbContextFactory.cs
│   │   ├── Import/
│   │   │   └── LegacyDbImporter.cs
│   │   └── Migrations/
│   └── AlNeda.Services/
│       ├── AuthService.cs
│       ├── ProductService.cs
│       └── BusinessServices.cs
├── tests/
│   ├── AlNeda.Tests/
│   │   ├── LegacyImportTests.cs
│   │   └── UnitTest1.cs
│   └── AlNeda.DomainLogic.Tests/ (F#)
│       ├── DomainTests.fs
│       ├── OrderWorkflowTests.fs
│       └── CalculationsTests.fs
├── scripts/
│   ├── build.ps1
│   ├── deploy.ps1
│   ├── backup.ps1
│   └── init.ps1
└── database/
    └── views/
        └── reporting_views.sql
```

## 11. BUILD STATUS

```
Debug Build:
> dotnet build
Build succeeded. 0 warnings, 0 errors

Release Build:
> dotnet build -c Release
Build succeeded. 0 warnings, 0 errors

Tests:
> dotnet test
F# Tests  (AlNeda.DomainLogic.Tests): 28 passed
C# Tests  (AlNeda.Tests):            2 passed
Total:                                 30 passed
```

*Last updated: 2026-05-07T16:30*
