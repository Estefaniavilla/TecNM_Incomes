# Module Specification: Dashboard (Role-Scoped Indicators & PDF Export)

## Domain Overview
The `Dashboard` module centralizes all financial KPIs, trend charts, payment method breakdowns, recent receipt tables, and executive PDF exports in C# (.NET 10), strictly scoped to the authenticated user's role and assigned cashier shift.

## Role & Shift Scoping Rules
1. **Admin / Auditor:** Global visibility across all cashiers, shifts, and bank deposits.
2. **Cajero Matutino (`cajero1`):** Filtered strictly to receipts emitted by `cajero1` / `Cajero Turno Matutino`.
3. **Cajero Vespertino / Nocturno (`cajero2`):** Filtered strictly to receipts emitted by `cajero2` / `Cajero Turno Nocturno`.
4. **User:** Scoped to the user's own operations.

## API Contracts & Dual Routing
- Primary Route: `/api/dashboard`
- Compatibility Route: `/api/caja/dashboard`

### Endpoints
| HTTP Method | Route | Description |
| :--- | :--- | :--- |
| `GET` | `/api/dashboard/indicadores?periodo=hoy\|semana\|mes` | Role-scoped KPIs, charts, concepts, and recent receipts with real student matriculas & names. |
| `GET` | `/api/dashboard/exportar-pdf?periodo=hoy\|semana\|mes` | Generates and downloads a real `application/pdf` executive cut report. |

## PDF Export Specification
- Built directly in pure C# (`DashboardPdfBuilder.cs`) producing valid PDF 1.4 binary streams (`Content-Type: application/pdf`).
- Exports strictly as `.pdf` without relying on browser print dialogs.

