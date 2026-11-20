# Specification: Conceptos de Cobro (Payment Concepts Management)

## 1. Overview
The **Conceptos de Cobro** module manages the catalog of fees, tuition, certificates, and academic services charged to students and users at TecNM Campus Monclova. It provides complete CRUD operations, filtering by category, search capabilities, pagination, and status toggling, supporting both cash register operations and accounting policies.

## 2. Business Rules & Validation
- **Clave (Concept Code):** Must be non-empty, unique across all concepts, alphanumeric or formatted (e.g., `5200-001-002-004`, `INSC-VIG`, `KARD`), maximum length 30 characters.
- **Descripcion (Description):** Must be non-empty, maximum length 200 characters.
- **CuotaBase (Base Fee):** Must be a non-negative decimal with up to 4 decimal places (`>= 0.00`).
- **AplicaBeca (Scholarship Discount Applicable):** Boolean flag. When true, scholarship percentage is applied during checkout in the Cashier module.
- **Categoria (Category):** Must be specified (e.g., `Inscripción`, `Constancias`, `Cursos`, `Exámenes`, `Servicios`).
- **CuentaReferencia (Accounting Account Reference):** Optional string referencing institutional accounting codes (up to 30 characters).
- **Orden (Display Order):** Optional integer for sorting priority in cash register selectors.
- **IsActive (Active Flag):** Soft-delete flag; concepts marked as inactive are preserved for historical receipts but excluded from new receipts.
- **Integrity Constraint on Deletion:** A concept cannot be hard-deleted if receipts or debts reference it; in such cases, soft-delete (`IsActive = false`) is enforced.

## 3. Endpoints (HTTP Specification)

| Method | Route | Description | Expected Status Codes |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/conceptos-cobro` | Retrieves paginated and filtered list of concepts | `200 OK` |
| `GET` | `/api/conceptos-cobro/all` | Retrieves all active concepts (optimized for dropdowns) | `200 OK` |
| `GET` | `/api/conceptos-cobro/categories` | Retrieves distinct existing categories | `200 OK` |
| `GET` | `/api/conceptos-cobro/{id}` | Retrieves a single concept by UUID | `200 OK`, `404 Not Found` |
| `POST` | `/api/conceptos-cobro` | Creates a new payment concept | `201 Created`, `400 Bad Request`, `409 Conflict` |
| `PUT` | `/api/conceptos-cobro/{id}` | Updates details and fee of an existing concept | `200 OK`, `400 Bad Request`, `404 Not Found` |
| `PATCH` | `/api/conceptos-cobro/{id}/toggle-status` | Toggles the active/inactive state | `200 OK`, `404 Not Found` |
| `DELETE` | `/api/conceptos-cobro/{id}` | Deletes or soft-deletes a concept | `204 No Content`, `404 Not Found`, `409 Conflict` |
