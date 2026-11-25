# Module Specification: Alumnos (Student Roster & Academic Services)

## Domain Overview
The `Alumnos` module models the official student roster of the Instituto Tecnológico Superior de Monclova (TecNM Monclova). It is completely decoupled from the cash register collection logic (`Caja`) and receipt operations (`Recibos`).

## Business Capabilities
1. **Consolidated Roster Access:** Querying across the institutional database containing 14,013 registered students.
2. **Predictive Search Autocomplete:** Real-time multi-field matching by enrollment number (`Matricula`), full name, and registration card (`NumeroFicha`).
3. **Institutional UI Pagination:** Paginated data queries configured by default with 5 items per page (`PageSize = 5`) matching the system UI pattern established in PR #11.
4. **Historical Matricula Resolution:** Transparent resolution of old or migrated enrollment numbers to the active student profile.
5. **Scholarship & Fee Waiver Inquiries:** Real-time status of student scholarships to allow automated discounts during receipt calculation.
6. **Outstanding Debt Auditing:** Querying student account balances without intertwining with transaction persistence.

## API Contracts & Dual Routing
To ensure 100% backward compatibility with existing frontend components (`StudentSearchDropdown.vue`, `CajaPrincipalView.vue`, etc.) without requiring modifications to the client-side TypeScript code:

- Primary Route: `/api/alumnos`
- Compatibility Route: `/api/caja/alumnos`

### Endpoints
| HTTP Method | Route | Description |
| :--- | :--- | :--- |
| `GET` | `/api/alumnos` / `/api/caja/alumnos` | Paginated student list (PageSize = 5 by default). |
| `GET` | `/api/alumnos/buscar` / `/api/caja/alumnos/buscar` | Search suggestions for autocomplete (top 25 results). |
| `GET` | `/api/alumnos/{matricula}` / `/api/caja/alumnos/{matricula}` | Single student profile by enrollment ID. |
| `GET` | `/api/alumnos/{matricula}/historial-matriculas` | Audit history of student enrollment ID reassignments. |
| `POST`| `/api/alumnos/{matricula}/cambiar-matricula` | Reassign enrollment ID and store audit trail. |
| `GET` | `/api/alumnos/{matricula}/adeudos` | Pending debt items for the student. |
| `GET` | `/api/alumnos/{matricula}/becas` | Active scholarships and waiver percentages. |
| `POST`| `/api/alumnos/inscribir` | Student registration or sync from Academic Services. |

## Data Persistence & Performance
- **Read-Only Optimizations:** Uses `AsNoTracking()` for all search, pagination, and inquiry operations.
- **Index Friendly:** Case-insensitive comparisons (`ToLower()`) matching PostgreSQL indexes.
