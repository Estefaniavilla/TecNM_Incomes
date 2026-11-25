# Module Specification: Polizas (Revenue Policies & Accounting Journals)

## Domain Overview
The `Polizas` module is responsible for the financial aggregation of income recorded by institutional cashiers. It translates individual receipt details (`DetallesRecibo`) into official accounting journals (`Póliza de Ingresos`) grouped by institutional revenue account codes and budgetary items.

## Business Capabilities
1. **Daily Policy Calculation (`Poliza Diaria`):** Aggregates receipts registered during a single operational day into accounting rows (`Cuenta`, `Concepto`, `Cantidad`, `Total`), strictly excluding cancelled receipts.
2. **Multi-Day Period Policy (`Poliza por Periodo`):** Enables auditing revenue across custom date ranges (e.g. weekly, monthly, semester cutoffs), returning both account summary groupings and itemized transaction chronologies.
3. **Printable Ledger Detail (`Reporte Detallado`):** Provides line-by-line receipt item projections including student credentials, payment methods, fees, and cashier usernames for official administrative printouts.
4. **Daily Operational Closing (`Cierre de Póliza`):** Formalizes the end of shift/day financial closing, assigning a sequential policy folio (`POL-YYYYMMDD-01`).

## API Contracts & Dual Routing
To ensure complete backward compatibility with existing frontend components (`ModalPolizaIngresos.vue`, `ModalPolizaIngresoPeriodo.vue`, `cajaApi.ts`):

- Primary Route: `/api/polizas`
- Compatibility Route: `/api/caja/polizas`
- Compatibility Route for Ledger: `/api/caja/reportes/detallado`

### Endpoints
| HTTP Method | Route | Description |
| :--- | :--- | :--- |
| `GET` | `/api/polizas/resumen` / `/api/caja/polizas/resumen` | Daily revenue policy grouped by accounts. |
| `GET` | `/api/polizas/periodo` / `/api/caja/polizas/periodo` | Multi-day date range policy with accounts and movements. |
| `GET` | `/api/polizas/detallado` / `/api/caja/reportes/detallado` | Line-by-line ledger for official printout. |
| `POST`| `/api/polizas/cerrar` | Formally closes daily policy with audit record. |

## Financial & Accounting Integrity
- Only non-cancelled receipts (`Estado != 'CANCELADO'`) are accumulated into revenue totals.
- Accounts are mapped from concept codes (`Concepto.Clave`) or fall back to the institutional general revenue account (`5200-001-002`).
- Amounts preserve 2 decimal precision without floating point drift.
