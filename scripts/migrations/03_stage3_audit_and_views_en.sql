-- ============================================================================
-- STAGE 3: AUDIT, TRIGGERS & ANALYTICAL VIEWS - ENGLISH CANONICAL
-- University Cash Register Management System - TecNM Campus Monclova
-- ============================================================================
-- Features:
-- 1. Automatic debt reversal trigger on receipt cancellation (PostgreSQL engine).
-- 2. English analytical views for administrative reporting and audits.
-- 3. Transparent compatibility views for existing Backend (.NET 10) & Frontend (Vue 3).
-- ============================================================================

BEGIN;

-- ----------------------------------------------------------------------------
-- 1. TRIGGER: AUTOMATIC DEBT REVERSAL ON RECEIPT CANCELLATION
-- Reverts any settled or partial debt back to 'PENDING' when a receipt is cancelled.
-- Supports both 'CANCELLED' (English) and 'CANCELADO' (Spanish) status strings.
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_reverse_debt_on_cancellation()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.status IN ('CANCELLED', 'CANCELADO') AND (OLD.status IS NULL OR OLD.status NOT IN ('CANCELLED', 'CANCELADO')) THEN
        UPDATE student_debts sd
        SET status = 'PENDING',
            pending_balance = sd.original_amount
        WHERE sd.student_id = NEW.student_id
          AND sd.status IN ('SETTLED', 'PARTIAL', 'LIQUIDADO')
          AND EXISTS (
              SELECT 1 
              FROM receipt_items ri
              WHERE ri.receipt_id = NEW.id
                AND (ri.description ILIKE '%' || sd.concept_name || '%' OR sd.concept_name ILIKE '%' || ri.description || '%')
          );
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_reverse_debt_cancellation ON receipts;
CREATE TRIGGER trg_reverse_debt_cancellation
AFTER UPDATE OF status ON receipts
FOR EACH ROW
EXECUTE FUNCTION fn_reverse_debt_on_cancellation();

-- ----------------------------------------------------------------------------
-- 2. CANONICAL ENGLISH ANALYTICAL VIEWS
-- ----------------------------------------------------------------------------

-- View 1: Daily Cash Cut and Reconciliation
DROP VIEW IF EXISTS vw_daily_cash_cut CASCADE;
CREATE VIEW vw_daily_cash_cut AS
SELECT 
    r.issued_at::DATE AS cut_date,
    r.cashier_username,
    r.cashier_name,
    pt.payment_method,
    COUNT(DISTINCT r.id) AS total_receipts_issued,
    COUNT(DISTINCT CASE WHEN r.status IN ('CANCELLED', 'CANCELADO') THEN r.id END) AS cancelled_receipts,
    SUM(CASE WHEN r.status NOT IN ('CANCELLED', 'CANCELADO') THEN pt.amount ELSE 0 END) AS total_collected
FROM receipts r
LEFT JOIN payment_transactions pt ON r.id = pt.receipt_id
GROUP BY r.issued_at::DATE, r.cashier_username, r.cashier_name, pt.payment_method;

-- View 2: Detailed Receipt Breakdown Report
DROP VIEW IF EXISTS vw_receipt_details_report CASCADE;
CREATE VIEW vw_receipt_details_report AS
SELECT 
    r.id AS receipt_id,
    r.receipt_number,
    r.folio_number,
    r.issued_at,
    r.effective_payment_date,
    r.student_id,
    r.student_name,
    r.student_career,
    r.student_semester,
    r.student_shift,
    ri.description AS item_description,
    ri.quantity,
    ri.unit_price,
    ri.scholarship_discount,
    ri.total_amount AS item_total,
    r.total AS receipt_total,
    r.payment_method,
    r.bank_reference,
    r.receipt_type,
    r.status AS receipt_status,
    r.cancellation_reason,
    r.cancelled_at,
    r.cashier_username,
    r.cashier_name
FROM receipts r
JOIN receipt_items ri ON r.id = ri.receipt_id;

-- View 3: Student Debts & Receivables
DROP VIEW IF EXISTS vw_student_debts CASCADE;
CREATE VIEW vw_student_debts AS
SELECT 
    sd.debt_id,
    sd.student_id,
    s.full_name AS student_name,
    s.career_name,
    s.semester,
    s.shift,
    s.academic_status,
    sd.concept_name,
    at.code AS term_code,
    at.year AS term_year,
    at.cycle AS term_cycle,
    sd.original_amount,
    sd.pending_balance,
    sd.due_date,
    sd.status AS debt_status
FROM student_debts sd
JOIN students s ON sd.student_id = s.student_id
JOIN academic_terms at ON sd.term_id = at.term_id;

-- View 4: Bank Deposits Reconciliation (5+5 Rule)
DROP VIEW IF EXISTS vw_bank_deposits_reconciliation CASCADE;
CREATE VIEW vw_bank_deposits_reconciliation AS
SELECT
    r.id AS receipt_id,
    r.receipt_number,
    r.folio_number,
    r.issued_at,
    r.effective_payment_date,
    r.student_id,
    r.student_name,
    s.internal_account_number,
    r.bank_reference,
    SUBSTRING(r.bank_reference FROM 1 FOR 5) AS student_account_prefix,
    SUBSTRING(r.bank_reference FROM 6 FOR 5) AS concept_code_suffix,
    r.bank_account,
    r.total,
    r.status,
    r.cashier_username
FROM receipts r
LEFT JOIN students s ON r.student_id = s.student_id
WHERE r.payment_method IN ('DEPOSITO_BANCARIO', 'TRANSFERENCIA');

-- View 5: Revenue Policies Summary
DROP VIEW IF EXISTS vw_revenue_policies_summary CASCADE;
CREATE VIEW vw_revenue_policies_summary AS
SELECT
    rp.policy_id,
    rp.policy_number,
    rp.policy_date,
    rp.total_collected,
    rp.cashier_username,
    rp.status,
    COUNT(r.id) AS associated_receipts_count,
    rp.created_at
FROM revenue_policies rp
LEFT JOIN receipts r ON rp.policy_id = r.policy_id
GROUP BY rp.policy_id, rp.policy_number, rp.policy_date, rp.total_collected, rp.cashier_username, rp.status, rp.created_at;

-- View 6: Student Financial Kardex
DROP VIEW IF EXISTS vw_student_financial_kardex CASCADE;
CREATE VIEW vw_student_financial_kardex AS
SELECT
    r.student_id,
    r.student_name,
    r.student_career,
    r.receipt_number,
    r.folio_number,
    r.issued_at,
    ri.description AS concept_description,
    ri.quantity,
    ri.unit_price,
    ri.scholarship_discount,
    ri.total_amount,
    r.payment_method,
    r.status AS receipt_status,
    r.cashier_name
FROM receipts r
JOIN receipt_items ri ON r.id = ri.receipt_id;

-- ----------------------------------------------------------------------------
-- 3. TRANSPARENT COMPATIBILITY VIEWS (FOR .NET 10 BACKEND & VUE 3 FRONTEND)
-- Updatable 1:1 mapped views ensuring zero breakage for existing applications.
-- ----------------------------------------------------------------------------

DROP VIEW IF EXISTS carreras CASCADE;
CREATE OR REPLACE VIEW carreras AS
SELECT 
    career_id       AS id_carrera,
    career_name     AS carrera,
    full_name       AS nombre_carrera,
    degree_title    AS titulo,
    is_active       AS activo
FROM careers;

DROP VIEW IF EXISTS periodos_academicos CASCADE;
CREATE OR REPLACE VIEW periodos_academicos AS
SELECT
    term_id      AS id_periodo,
    code         AS codigo,
    description  AS descripcion,
    year         AS anio,
    cycle        AS ciclo,
    start_date   AS fecha_inicio,
    end_date     AS fecha_fin,
    is_active    AS activo
FROM academic_terms;

DROP VIEW IF EXISTS alumnos CASCADE;
CREATE OR REPLACE VIEW alumnos AS
SELECT
    student_id              AS matricula,
    full_name               AS nombre_completo,
    first_name              AS nombres,
    last_name               AS apellidos,
    gender                  AS sexo,
    career_id               AS id_carrera,
    career_name             AS carrera,
    semester                AS semestre,
    shift                   AS turno,
    admission_ticket_number AS numero_ficha,
    internal_account_number AS cuenta_a,
    academic_status         AS estatus,
    has_scholarship         AS tiene_beca,
    scholarship_percentage  AS porcentaje_beca,
    created_at
FROM students;

DROP VIEW IF EXISTS alumno_matriculas_historial CASCADE;
CREATE OR REPLACE VIEW alumno_matriculas_historial AS
SELECT
    history_id          AS id_historial,
    student_id          AS matricula_alumno,
    previous_student_id AS matricula_previa,
    term_id             AS id_periodo,
    registered_at       AS fecha_registro,
    change_reason       AS motivo_cambio
FROM student_enrollment_history;

DROP VIEW IF EXISTS becas_descuentos CASCADE;
CREATE OR REPLACE VIEW becas_descuentos AS
SELECT
    scholarship_id   AS id_beca,
    student_id       AS matricula,
    term_id          AS id_periodo,
    scholarship_type AS tipo_beca,
    percentage       AS porcentaje,
    fixed_amount     AS monto_fijo,
    reason           AS motivo,
    is_active        AS activa,
    created_at
FROM scholarships;

DROP VIEW IF EXISTS adeudos CASCADE;
CREATE OR REPLACE VIEW adeudos AS
SELECT
    debt_id          AS id_adeudo,
    student_id       AS matricula,
    concept_name     AS concepto_cobro,
    term_id          AS id_periodo,
    original_amount  AS monto_original,
    pending_balance  AS saldo_pendiente,
    due_date         AS fecha_limite,
    status           AS estado,
    created_at
FROM student_debts;

DROP VIEW IF EXISTS conceptos_cobro CASCADE;
CREATE OR REPLACE VIEW conceptos_cobro AS
SELECT
    id,
    code                AS clave,
    description         AS descripcion,
    base_fee            AS cuota_base,
    display_order       AS orden,
    ledger_account_code AS cuenta_referencia,
    allows_scholarship  AS aplica_beca,
    category            AS categoria,
    is_active
FROM payment_concepts;

DROP VIEW IF EXISTS sesiones_caja CASCADE;
CREATE OR REPLACE VIEW sesiones_caja AS
SELECT
    session_id       AS id_sesion,
    cashier_username AS usuario_cajero,
    cashier_name     AS nombre_cajero,
    opened_at        AS fecha_apertura,
    initial_cash     AS monto_inicial,
    closed_at        AS fecha_cierre,
    system_total     AS total_sistema,
    counted_total    AS total_arqueo,
    difference       AS diferencia,
    status           AS estado,
    notes            AS observaciones
FROM cashier_sessions;

DROP VIEW IF EXISTS folios CASCADE;
CREATE OR REPLACE VIEW folios AS
SELECT
    folio_number AS numero_folio,
    series       AS serie,
    created_at   AS fecha_creacion,
    notes        AS observaciones
FROM receipt_folios;

DROP VIEW IF EXISTS polizas_ingreso CASCADE;
CREATE OR REPLACE VIEW polizas_ingreso AS
SELECT
    policy_id        AS id_poliza,
    policy_number    AS numero_poliza,
    policy_date      AS fecha_poliza,
    total_collected  AS total_recaudado,
    cashier_username AS cajero_usuario,
    status           AS estado,
    created_at
FROM revenue_policies;

DROP VIEW IF EXISTS recibos CASCADE;
CREATE OR REPLACE VIEW recibos AS
SELECT
    id,
    receipt_number          AS numero_recibo,
    folio_number            AS folio_administrativo,
    issued_at               AS fecha_operacion,
    effective_payment_date  AS fecha_efectiva_pago,
    student_id              AS alumno_matricula,
    student_name            AS alumno_nombre,
    student_career          AS alumno_carrera,
    student_semester        AS alumno_semestre,
    student_shift           AS alumno_turno,
    payment_method          AS metodo_pago,
    bank_reference          AS referencia_bancaria,
    bank_account            AS cuenta_bancaria,
    bank_transaction_date   AS fecha_bancaria,
    subtotal,
    discount_total          AS descuento_total,
    total,
    status_code             AS status,
    status                  AS estado,
    receipt_type            AS tipo_recibo,
    is_locked,
    session_id              AS sesion_id,
    policy_id               AS poliza_id,
    cancellation_reason     AS motivo_cancelacion,
    cancelled_at            AS fecha_cancelacion,
    cashier_username        AS cajero_usuario,
    cashier_name            AS cajero_nombre
FROM receipts;

DROP VIEW IF EXISTS detalles_recibo CASCADE;
CREATE OR REPLACE VIEW detalles_recibo AS
SELECT
    id,
    receipt_id           AS recibo_id,
    concept_id           AS concepto_id,
    description          AS descripcion,
    quantity             AS cantidad,
    unit_price           AS cuota_unitario,
    scholarship_discount AS descuento_beca,
    total_amount         AS importe_total,
    academic_term        AS periodo_ano
FROM receipt_items;

DROP VIEW IF EXISTS movimientos_pago CASCADE;
CREATE OR REPLACE VIEW movimientos_pago AS
SELECT
    transaction_id   AS id_movimiento,
    receipt_id       AS recibo_id,
    payment_method   AS forma_pago,
    reference_number AS referencia_bancaria,
    amount           AS monto,
    transaction_date AS fecha_movimiento,
    cashier_username AS cajero_usuario
FROM payment_transactions;

DROP VIEW IF EXISTS migracion_incidencias CASCADE;
CREATE OR REPLACE VIEW migracion_incidencias AS
SELECT
    id,
    source_table      AS tabla_origen,
    record_identifier AS identificador_registro,
    detected_issue    AS incidencia_detectada,
    logged_at         AS fecha_registro,
    is_resolved       AS resuelta,
    resolution_notes  AS notas_resolucion
FROM migration_issues;

COMMIT;

