# Software Design Description: Financial Reports & Document Export Module

## 1. Context and Objective
The **Reportes** vertical slice in the `TecNM_Incomes` backend provides standardized institutional document exports across all operational reports and cash register receipts. It enables users to export data into three industry-standard formats:
- **PDF (.pdf)**: Formal binary PDF 1.4 document with header, tabular alignment, summaries, and digital signature boxes.
- **CSV (.csv)**: Clean RFC-4180 compliant comma-separated file encoded with UTF-8 BOM for immediate opening in Microsoft Excel without character encoding distortion.
- **Word (.doc)**: Microsoft Office-compatible document with embedded styles, borders, and landscape/portrait paper dimensions.

## 2. Supported Reports
1. **Póliza de Ingresos (Revenue Policy)**:
   - Account summary (`modo=cuentas`) grouping by accounting ledger code and concepts.
   - Folio details (`modo=detalle`) listing each transaction, matricula, student, and payment method.
2. **Reporte de Deudores (Debtors Roster)**:
   - Filterable by academic term, career code, cutoff date, and enrolled status.
3. **Reporte de Ingresos (Income Activity)**:
   - Filterable by date range, concept category, and cashier operator.
4. **Recibo Oficial (Cash Receipt)**:
   - Individual official receipt by folio number.

## 3. Endpoints and Contracts
- `GET /api/reportes/poliza/exportar`
- `GET /api/reportes/deudores/exportar`
- `GET /api/reportes/ingresos/exportar`
- `GET /api/reportes/recibos/{folio}/exportar`
- Dual-routed under `/api/caja/reportes/...` for frontend backward-compatibility.
