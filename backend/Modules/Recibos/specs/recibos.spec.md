# Specification: Recibos (Receipt Issuance, Financial Transactions & Cancellation Audit)

## 1. Overview
The **Recibos** module is the central transactional engine of the university cash register. It processes student payments, generates official consecutive fiscal folios, amortizes student debt, applies scholarship deductions, and handles authorized receipt cancellations with comprehensive audit logging as mandated by institutional accounting standards.

## 2. Business Rules & Validation
- **Consecutive Numbering (NumeroRecibo & FolioAdministrativo):**
  - Every definitive receipt receives a strictly increasing, contiguous integer ID (`NumeroRecibo`).
  - An administrative folio string is generated automatically formatted as `FOL-YYYY-MM-###` (e.g., `FOL-2026-11-1002`).
- **Student Data Binding:**
  - Requires valid `Matricula`, `NombreCompleto`, `Carrera`, `Semestre`, and `Turno`.
- **Payment Method (MetodoPago):**
  - Valid methods: `EFECTIVO`, `DEPOSITO_BANCARIO`, `TRANSFERENCIA`, `TARJETA_DEBITO`, `TARJETA_CREDITO`.
  - Banking payment methods optionally record bank account and reference number.
- **Amounts & Line Items (Detalles):**
  - At least one concept line item is mandatory.
  - Line items record `ConceptoId`, `Descripcion`, `Cantidad >= 1`, `CuotaUnitario >= 0.00`, `DescuentoBeca >= 0.00`, and `ImporteTotal = (Cantidad * CuotaUnitario) - DescuentoBeca`.
  - `Total` must equal the sum of line item `ImporteTotal`.
- **Debt Amortization:**
  - When a definitive receipt is processed, matching pending debts (`adeudos`) for the student are automatically reduced or marked as `LIQUIDADO`.
- **Receipt Cancellation (Auditoría de Cancelación):**
  - A receipt can only be cancelled once.
  - A formal justification (`motivo`) of at least 5 characters is mandatory.
  - Records the cancelling cashier username and UTC timestamp; changes status to `CANCELADO` (`Status = 0`).

## 3. Endpoints Specification

| Method | Route | Description | Expected Status Codes |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/recibos` | Retrieves paginated and filtered list of receipts | `200 OK` |
| `GET` | `/api/recibos/recientes` | Retrieves the latest N receipts (limit: 15) | `200 OK` |
| `GET` | `/api/recibos/{numero}` | Retrieves single receipt by its integer receipt number | `200 OK`, `404 Not Found` |
| `GET` | `/api/recibos/id/{id}` | Retrieves single receipt by UUID | `200 OK`, `404 Not Found` |
| `POST` | `/api/recibos` | Generates official definitive receipt and amortizes debt | `201 Created`, `400 Bad Request` |
| `POST` | `/api/recibos/previo` | Generates preview receipt without locking folios | `200 OK`, `400 Bad Request` |
| `POST` | `/api/recibos/{id}/cancelar` | Cancels receipt with mandatory audit justification | `200 OK`, `400 Bad Request`, `404 Not Found`, `409 Conflict` |
| `GET` | `/api/recibos/corte-turno` | Computes shift closing metrics, balances cash vs bank, and deducts cancellations | `200 OK` |
| `GET` | `/api/recibos/auditoria/cancelaciones` | Retrieves cancellation audit trail for fiscal oversight | `200 OK` |
| `GET` | `/api/recibos/estatus/{numero}` | Quick status summary lookup for consecutive receipt number | `200 OK`, `404 Not Found` |
| `GET` | `/api/recibos/{numero}/exportar` | Generates official institutional printable receipt voucher file for download | `200 OK`, `404 Not Found` |
