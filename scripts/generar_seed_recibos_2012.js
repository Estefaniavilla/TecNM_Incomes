const fs = require('fs');
const path = require('path');

// 1. Read the extracted rows from media_1795582185609_37312a03.xlsx
let text = fs.readFileSync('scratch_recibos_2012.json', 'utf8').replace(/^\uFEFF/, '');
const rows = JSON.parse(text);

console.log(`Cargando ${rows.length} renglones de recibo...`);

// Careers map (8 official degrees)
const carrerasMap = {
    "64": "Lic. Informática",
    "65": "Ing. Mecánica",
    "67": "Ing. Industrial",
    "68": "Ing. Electrónica",
    "69": "Ing. Informática",
    "70": "Ing. Gestión Emp.",
    "71": "Ing. Energías Renovables",
    "74": "Ing. Mecatrónica"
};

// Cashiers map
const cajerosMap = {
    "1": { username: "cajero1", name: "Cajero Turno Matutino" },
    "2": { username: "cajero2", name: "Cajero Turno Nocturno" },
    "3": { username: "admin",   name: "Administrador General" },
    "4": { username: "usuario", name: "Usuario Operativo Demo" }
};

// Date serial conversion (Excel 1900 date system)
function excelSerialToDateStr(serialStr) {
    const s = parseInt(serialStr);
    if (s === 40931) return '2012-01-23 00:00:00-06';
    if (s === 40932) return '2012-01-24 00:00:00-06';
    if (s === 40933) return '2012-01-25 00:00:00-06';
    // General formula if other serial
    const date = new Date(Math.round((s - 25569) * 86400 * 1000));
    return date.toISOString().replace('T', ' ').substring(0, 19) + '-06';
}

// Concept definitions
const conceptosInfo = {
    '5400-001-002-0037': { desc: 'INSCRIPCION SEM. FEB-JUL 2012', fee: 1700.00, cat: 'Inscripción' },
    '5400-001-002-0038': { desc: 'DESCUENTO DE INSCRIPCION', fee: 100.00, cat: 'Descuentos' },
    '5400-001-002-0039': { desc: 'BECA DE INSCRIPCION', fee: 480.00, cat: 'Becas' },
    '5400-001-002-0040': { desc: 'BECA DE INSCRIPCION', fee: 400.00, cat: 'Becas' },
    '5400-001-002-0041': { desc: 'BECA DE INSCRIPCION', fee: 800.00, cat: 'Becas' },
    '5400-001-002-0043': { desc: 'BECA DE INSCRIPCION', fee: 800.00, cat: 'Becas' },
    '5400-001-002-0044': { desc: 'DESCUENTO DE INSCRIPCION', fee: 320.00, cat: 'Descuentos' },
    '5400-001-002-0028': { desc: 'INSCRIPCION AGOSTO 2011-ENERO 2012', fee: 1600.00, cat: 'Inscripción' },
    '5400-001-002-0036': { desc: 'INSCRIPCION SEM. ENERO-JUNIO 2012', fee: 300.00, cat: 'Inscripción' },
    '5400-001-002-0031': { desc: 'INSCRIPCION SEMESTRAL', fee: 200.00, cat: 'Inscripción' },
    '5200-001-002-001':  { desc: 'CUOTA DE TRAMITE INSTITUCIONAL', fee: 100.00, cat: 'Trámites' },
    '5200-001-002-002':  { desc: 'SEGURO ESTUDIANTIL / TRAMITE', fee: 50.00, cat: 'Trámites' },
    '5200-001-002-005':  { desc: 'INSCRIPCION ESPECIAL', fee: 1500.00, cat: 'Inscripción' },
    '5200-002-004-019':  { desc: 'INSCRIPCION EXTEMPORANEA', fee: 1450.00, cat: 'Inscripción' },
    '5200-004-009-024':  { desc: 'RECARGOS ADMINISTRATIVOS', fee: 20.00, cat: 'Recargos' },
    '5200-004-009-027':  { desc: 'CUOTA DE REINSCRIPCION', fee: 300.00, cat: 'Inscripción' },
    '5200-004-009-029':  { desc: 'RECARGOS POR INSCRIPCION', fee: 200.00, cat: 'Recargos' }
};

// 2. Identify unique students and concepts to ensure catalogs are complete
const uniqueStudents = {};
for (const r of rows) {
    const mat = (r.A || '').trim();
    if (mat && !uniqueStudents[mat]) {
        uniqueStudents[mat] = {
            matricula: mat,
            nombre: (r.B || '').trim().replace(/'/g, "''"),
            semestre: parseInt(r.C || 1),
            carreraId: carrerasMap[(r.D || '').trim()] ? (r.D || '').trim() : "NULL",
            carreraNombre: carrerasMap[(r.D || '').trim()] || "Sin Asignar",
            turno: (r.E || 'Matutino').trim()
        };
    }
}

// 3. Group rows into coherent receipts
const receipts = [];
let current = null;

for (let i = 0; i < rows.length; i++) {
    const r = rows[i];
    const mat = (r.A || '').trim();
    const sub = parseFloat(r.J || 0);
    const st = parseInt(r.K || 1);
    const usr = (r.L || '').trim();
    const fch = (r.F || '').trim();

    const isSameStudent = current && current.matricula === mat && current.fecha === fch && current.idUsuario === usr && current.status === st;

    // Belong to current receipt if it is a discount or accessory concept
    if (isSameStudent && (sub < 0 || r.G.includes('029') || r.G.includes('024') || r.G.includes('002') || r.G.includes('001'))) {
        current.items.push(r);
    } else {
        if (current) receipts.push(current);
        current = {
            matricula: mat,
            nombre: (r.B || '').trim().replace(/'/g, "''"),
            semestre: parseInt(r.C || 1),
            carreraId: carrerasMap[(r.D || '').trim()] ? (r.D || '').trim() : "NULL",
            carreraNombre: carrerasMap[(r.D || '').trim()] || "Sin Asignar",
            turno: (r.E || 'Matutino').trim(),
            fecha: fch,
            status: st,
            idUsuario: usr,
            items: [r]
        };
    }
}
if (current) receipts.push(current);

console.log(`Se consolidaron ${receipts.length} recibos oficiales.`);

// 4. Build SQL script
const sqlLines = [];
sqlLines.push('-- ============================================================================');
sqlLines.push('-- SEED: CARGA DE RECIBOS HISTORICOS CICLO 2012-1');
sqlLines.push('-- Sistema de Control de Caja Universitaria - TecNM Campus Monclova');
sqlLines.push(`-- Total de Recibos: ${receipts.length} | Total de Partidas: ${rows.length}`);
sqlLines.push('-- ============================================================================');
sqlLines.push('BEGIN;\n');

// 4.1 Conceptos de cobro complementarios
sqlLines.push('-- 1. CONCEPTOS DE COBRO COMPLEMENTARIOS');
sqlLines.push('INSERT INTO payment_concepts (code, description, base_fee, display_order, ledger_account_code, allows_scholarship, category, is_active) VALUES');
const ctaEntries = Object.keys(conceptosInfo).map(code => {
    const info = conceptosInfo[code];
    return `('${code}', '${info.desc}', ${info.fee.toFixed(2)}, NULL, '${code.substring(0, 4)}', true, '${info.cat}', true)`;
});
sqlLines.push(ctaEntries.join(',\n') + '\nON CONFLICT (code) DO NOTHING;\n');

// 4.2 Asegurar que los alumnos del archivo existan en el padron
sqlLines.push('-- 2. ASEGURAR REGISTRO DE ALUMNOS EN PADRON');
const studentKeys = Object.keys(uniqueStudents);
for (let i = 0; i < studentKeys.length; i += 200) {
    const chunk = studentKeys.slice(i, i + 200);
    const valRows = chunk.map(m => {
        const s = uniqueStudents[m];
        return `('${s.matricula}', '${s.nombre}', '${s.nombre}', NULL, NULL, ${s.carreraId}, '${s.carreraNombre}', ${s.semestre}, '${s.turno}', NULL, 'Inscrito', false, 0.00)`;
    });
    sqlLines.push('INSERT INTO students (student_id, full_name, first_name, last_name, gender, career_id, career_name, semester, shift, internal_account_number, academic_status, has_scholarship, scholarship_percentage) VALUES');
    sqlLines.push(valRows.join(',\n') + '\nON CONFLICT (student_id) DO NOTHING;\n');
}

// 4.3 Folios (Serie A, rango 6001 a 6000 + receipts.length)
sqlLines.push('-- 3. CONTROL DE FOLIOS');
const startFolio = 6001;
const folioRows = [];
for (let i = 0; i < receipts.length; i++) {
    folioRows.push(`(${startFolio + i}, 'A')`);
}
for (let i = 0; i < folioRows.length; i += 500) {
    const chunk = folioRows.slice(i, i + 500);
    sqlLines.push('INSERT INTO receipt_folios (folio_number, series) VALUES');
    sqlLines.push(chunk.join(',\n') + '\nON CONFLICT (folio_number) DO NOTHING;\n');
}

// 4.4 Recibos, Detalles y Movimientos de Pago
sqlLines.push('-- 4. RECIBOS DE CAJA, PARTIDAS FACTURADAS Y MOVIMIENTOS DE PAGO');

let itemGlobalIdx = 1;

for (let i = 0; i < receipts.length; i += 100) {
    const chunk = receipts.slice(i, i + 100);

    const recibosBatch = [];
    const detallesBatch = [];
    const pagosBatch = [];

    for (let j = 0; j < chunk.length; j++) {
        const globalIdx = i + j + 1;
        const rc = chunk[j];
        const folio = startFolio + i + j;
        const receiptUuid = `c0000000-0000-0000-0000-${String(globalIdx).padStart(12, '0')}`;
        const fechaSql = excelSerialToDateStr(rc.fecha);
        const cajero = cajerosMap[rc.idUsuario] || cajerosMap["1"];

        let subtotal = 0;
        let discount = 0;

        // Process line items for this receipt
        for (const it of rc.items) {
            const itemUuid = `d0000000-0000-0000-0000-${String(itemGlobalIdx++).padStart(12, '0')}`;
            const ctaCode = (it.G || '').trim();
            const val = parseFloat(it.J || 0);
            const desc = (conceptosInfo[ctaCode] ? conceptosInfo[ctaCode].desc : 'CONCEPTO INSTITUCIONAL ' + ctaCode).replace(/'/g, "''");

            let unitPrice = val;
            let itemDiscount = 0;

            if (val >= 0) {
                subtotal += val;
            } else {
                discount += Math.abs(val);
                unitPrice = Math.abs(val);
                itemDiscount = Math.abs(val);
            }

            const itemTotal = Math.max(0, unitPrice - itemDiscount);

            detallesBatch.push(`('${itemUuid}', '${receiptUuid}', (SELECT id FROM payment_concepts WHERE code = '${ctaCode}'), '${desc}', 1, ${unitPrice.toFixed(2)}, ${itemDiscount.toFixed(2)}, ${itemTotal.toFixed(2)}, '2012-1')`);
        }

        const netTotal = Math.max(0, subtotal - discount);
        const statusCode = rc.status === 0 ? 0 : 1;
        const statusStr = rc.status === 0 ? 'CANCELADO' : 'REGISTRADO';
        const cancelReasonSql = rc.status === 0 ? "'Cancelación en ventanilla'" : "NULL";
        const cancelDateSql = rc.status === 0 ? `'${fechaSql}'` : "NULL";

        // Payment method detection
        let paymentMethod = 'EFECTIVO';
        let bankRef = '0';
        if (rc.items[0].M && rc.items[0].M !== 'NULL') {
            paymentMethod = rc.items[0].M.trim();
        }
        if (rc.items[0].N && rc.items[0].N !== 'NULL') {
            bankRef = rc.items[0].N.trim();
            paymentMethod = 'DEPOSITO_BANCARIO';
        }

        recibosBatch.push(`('${receiptUuid}', '${folio}', '${fechaSql}', '${fechaSql}', '${rc.matricula}', '${rc.nombre}', '${rc.carreraNombre}', ${rc.semestre}, '${rc.turno}', '${paymentMethod}', '${bankRef}', ${subtotal.toFixed(2)}, ${discount.toFixed(2)}, ${netTotal.toFixed(2)}, ${statusCode}, '${statusStr}', 'DEFINITIVO', ${cancelReasonSql}, ${cancelDateSql}, '${cajero.username}', '${cajero.name}')`);

        if (statusCode === 1 && netTotal > 0) {
            pagosBatch.push(`('${receiptUuid}', '${paymentMethod}', '${bankRef}', ${netTotal.toFixed(2)}, '${fechaSql}', '${cajero.username}')`);
        }
    }

    if (recibosBatch.length > 0) {
        sqlLines.push('INSERT INTO receipts (id, folio_number, issued_at, effective_payment_date, student_id, student_name, student_career, student_semester, student_shift, payment_method, bank_reference, subtotal, discount_total, total, status_code, status, receipt_type, cancellation_reason, cancelled_at, cashier_username, cashier_name) VALUES');
        sqlLines.push(recibosBatch.join(',\n') + '\nON CONFLICT (id) DO NOTHING;\n');
    }

    if (detallesBatch.length > 0) {
        sqlLines.push('INSERT INTO receipt_items (id, receipt_id, concept_id, description, quantity, unit_price, scholarship_discount, total_amount, academic_term) VALUES');
        sqlLines.push(detallesBatch.join(',\n') + '\nON CONFLICT (id) DO NOTHING;\n');
    }

    if (pagosBatch.length > 0) {
        sqlLines.push('INSERT INTO payment_transactions (receipt_id, payment_method, reference_number, amount, transaction_date, cashier_username) VALUES');
        sqlLines.push(pagosBatch.join(',\n') + '\nON CONFLICT DO NOTHING;\n');
    }
}

sqlLines.push('COMMIT;\n');

const outputSql = path.join('scripts', 'seed_recibos_2012.sql');
fs.writeFileSync(outputSql, sqlLines.join('\n'), 'utf8');

console.log(`Exito: Archivo ${outputSql} generado correctamente.`);
console.log(`- Recibos: ${receipts.length}`);
console.log(`- Renglones facturados: ${rows.length}`);
console.log(`- Folios asignados: ${startFolio} a ${startFolio + receipts.length - 1}`);
