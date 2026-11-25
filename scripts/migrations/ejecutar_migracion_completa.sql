-- ============================================================================
-- EJECUTOR MAESTRO DE MIGRACIÓN: SISTEMA DE CAJA UNIVERSITARIA
-- TecNM Campus Monclova (Canonical English Schema + Spanish Compatibility)
-- ============================================================================
-- Orden de ejecución secuencial:
--   1. Stage 1: Structure (Tablas en Inglés, Constraints, Índices)
--   2. Stage 2: Data (Catálogos Oficiales 8 Carreras, Periodos, Conceptos, Usuarios)
--   3. Stage 3: Audit, Triggers & Views (Triggers, Vistas Analíticas en Inglés y Vistas de Compatibilidad)
--   4. Seed Padrón Alumnos: 14,013 alumnos consolidados
--   5. Seed Previos Completo: Recibos, partidas facturadas y movimientos de pago
-- ============================================================================

\ir 01_stage1_structure_en.sql
\ir 02_stage2_data_en.sql
\ir 03_stage3_audit_and_views_en.sql
\ir ../seed_alumnos_padron.sql
\ir ../seed_previos_completo.sql
\ir ../seed_recibos_2012.sql

-- REPORTE FINAL DE VALIDACIÓN POST-MIGRACIÓN (TABLAS CANÓNICAS EN INGLÉS)
SELECT 'careers' AS table_name, COUNT(*) AS total_records FROM careers
UNION ALL SELECT 'academic_terms', COUNT(*) FROM academic_terms
UNION ALL SELECT 'users', COUNT(*) FROM users
UNION ALL SELECT 'students', COUNT(*) FROM students
UNION ALL SELECT 'student_enrollment_history', COUNT(*) FROM student_enrollment_history
UNION ALL SELECT 'scholarships', COUNT(*) FROM scholarships
UNION ALL SELECT 'student_debts', COUNT(*) FROM student_debts
UNION ALL SELECT 'payment_concepts', COUNT(*) FROM payment_concepts
UNION ALL SELECT 'receipt_folios', COUNT(*) FROM receipt_folios
UNION ALL SELECT 'revenue_policies', COUNT(*) FROM revenue_policies
UNION ALL SELECT 'receipts', COUNT(*) FROM receipts
UNION ALL SELECT 'receipt_items', COUNT(*) FROM receipt_items
UNION ALL SELECT 'payment_transactions', COUNT(*) FROM payment_transactions
UNION ALL SELECT 'cashier_sessions', COUNT(*) FROM cashier_sessions
UNION ALL SELECT 'migration_issues', COUNT(*) FROM migration_issues;
