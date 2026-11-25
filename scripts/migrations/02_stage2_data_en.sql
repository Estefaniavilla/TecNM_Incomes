-- ============================================================================
-- STAGE 2: DATA POPULATION & SEEDING - ENGLISH TABLES
-- University Cash Register Management System - TecNM Campus Monclova
-- ============================================================================
-- Design rules:
-- 1. 'careers' populated with official 8 degrees (abbreviation & full name).
-- 2. 'payment_concepts' preserves official descriptions in Spanish.
-- 3. 'academic_terms' and 'users' configured.
-- ============================================================================

BEGIN;

-- ----------------------------------------------------------------------------
-- 1. CAREERS (Official Degree Programs - TecNM Monclova)
-- ----------------------------------------------------------------------------
INSERT INTO careers (career_id, career_name, full_name, degree_title, is_active) VALUES
(64, 'Lic. Informática',         'LICENCIATURA EN INFORMATICA',          'LICENCIADO EN INFORMATICA', true),
(65, 'Ing. Mecánica',            'INGENIERIA MECANICA',                  'INGENIERO MECANICO', true),
(67, 'Ing. Industrial',          'INGENIERIA INDUSTRIAL',                'INGENIERO INDUSTRIAL', true),
(68, 'Ing. Electrónica',         'INGENIERIA ELECTRONICA',               'INGENIERO ELECTRONICO', true),
(69, 'Ing. Informática',         'INGENIERIA INFORMATICA',               'INGENIERO INFORMATICO', true),
(70, 'Ing. Gestión Emp.',        'INGENIERIA EN GESTION EMPRESARIAL',    'INGENIERO EN GESTION EMPRESARIAL', true),
(71, 'Ing. Energías Renovables', 'INGENIERIA EN ENERGIAS RENOVABLES',    'INGENIERO EN ENERGIAS RENOVABLES', true),
(74, 'Ing. Mecatrónica',         'INGENIERIA MECATRONICA',               'INGENIERO MECATRONICO', true)
ON CONFLICT (career_id) DO UPDATE SET
    career_name = EXCLUDED.career_name,
    full_name = EXCLUDED.full_name,
    degree_title = EXCLUDED.degree_title,
    is_active = EXCLUDED.is_active;

-- ----------------------------------------------------------------------------
-- 2. ACADEMIC TERMS
-- ----------------------------------------------------------------------------
INSERT INTO academic_terms (code, description, year, cycle, start_date, end_date, is_active) VALUES
('2012-1', 'SEMESTRE ENERO-JUNIO 2012',        2012, 'PRIMAVERA', '2012-01-09', '2012-06-30', false),
('2016-1', 'SEMESTRE ENERO-JUNIO 2016',        2016, 'PRIMAVERA', '2016-01-11', '2016-06-30', false),
('2016-2', 'SEMESTRE AGOSTO-ENERO 2016-2017',  2016, 'OTONO',     '2016-08-08', '2017-01-31', false),
('2024-2', 'SEMESTRE AGOSTO-DICIEMBRE 2024',   2024, 'OTONO',     '2024-08-12', '2024-12-20', false),
('2025-1', 'SEMESTRE ENERO-JUNIO 2025',        2025, 'PRIMAVERA', '2025-01-13', '2025-06-27', false),
('2025-2', 'SEMESTRE AGOSTO-DICIEMBRE 2025',   2025, 'OTONO',     '2025-08-11', '2025-12-19', false),
('2026-1', 'SEMESTRE ENERO-JUNIO 2026',        2026, 'PRIMAVERA', '2026-01-12', '2026-06-30', true)
ON CONFLICT (code) DO UPDATE SET
    description = EXCLUDED.description,
    is_active = EXCLUDED.is_active;

-- ----------------------------------------------------------------------------
-- 3. USERS (Authentication & Role-Based Access Control)
-- ----------------------------------------------------------------------------
INSERT INTO users (id, username, email, password, name, role, is_active) VALUES
('10000000-0000-4000-8000-000000000001', 'admin',   'admin@tecnm.local',   'AQAAAAIAAYagAAAAEPjbneKeORd2reFIG62af0ufOMPtwf64j0jpBznNBo3Y8K/BUoUW+IhJOm0b5iqWOg==', 'Administrador General', 'Admin',  true),
('10000000-0000-4000-8000-000000000002', 'cajero1', 'cajero1@tecnm.local', 'AQAAAAIAAYagAAAAECBhXWISKf+YayCz0xB8E4vyjO6chAfRLTq+f2eA4ztdtdDqA99jr+VsfZXUoKbfdQ==', 'Cajero Turno Matutino', 'Cajero', true),
('10000000-0000-4000-8000-000000000003', 'cajero2', 'cajero2@tecnm.local', 'AQAAAAIAAYagAAAAECBhXWISKf+YayCz0xB8E4vyjO6chAfRLTq+f2eA4ztdtdDqA99jr+VsfZXUoKbfdQ==', 'Cajero Turno Nocturno', 'Cajero', true),
('10000000-0000-4000-8000-000000000004', 'usuario', 'usuario@tecnm.local', 'AQAAAAIAAYagAAAAECBhXWISKf+YayCz0xB8E4vyjO6chAfRLTq+f2eA4ztdtdDqA99jr+VsfZXUoKbfdQ==', 'Usuario Operativo Demo', 'User',  true)
ON CONFLICT (username) DO UPDATE SET
    password = EXCLUDED.password,
    name = EXCLUDED.name,
    role = EXCLUDED.role;

-- ----------------------------------------------------------------------------
-- 4. PAYMENT CONCEPTS (Tariff Catalog - Preserving Spanish Descriptions)
-- ----------------------------------------------------------------------------
INSERT INTO payment_concepts (code, description, base_fee, display_order, ledger_account_code, allows_scholarship, category, is_active) VALUES
  ('5200-001-002-004', 'INSCRIPCION SEM. AGO 2012-ENERO 2013', 1700.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-001-002-021', 'REINSCRIPCION SEM. AGOSTO 2022-ENERO 2023', 500.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-001-002-051', 'REINSCRIPCION SEM. AGOSTO 2025-ENERO 2026', 500.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-001-002-082', 'REINSCRIPCION SEM FEB-JUL 2023', 500.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-001-002-115', 'INSCRIPCION FEB 2006', 1450.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-002-001-001', 'INSCRIPCIONES', 1350.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-002-001-009', 'INSCRIPCION AGO-05', 1400.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-002-004-011', 'REINSCRIPCION SEM. FEBRERO-JULIO 2024', 500.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-004-009-023', 'INSCRIPCION AGO-2006', 1450.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-004-009-028', 'Inscripción Feb-Jul-2007', 1500.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-004-009-029', 'RECARGOS POR INSCRIPCION', 200.00, NULL, '5200', false, 'Recargos', true),
  ('5200-004-009-058', 'INSCRIPCION AGO-ENERO 2007', 1525.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-005-001-001', 'INSCRIPCION AGO-DIC-04 NVO. ING.', 1650.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-005-001-003', 'INSCRIPCION NVO. INGRESO', 1700.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-005-001-004', 'INSCRIPCION FEBRERO-JULIO 2008', 1550.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-005-001-013', 'INSCRIPCION SEM-FEB-JULIO-2009', 1600.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-005-001-017', 'INSCRIPCION SEM-AGOSTO 08-ENERO 09', 1550.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-006-001-0040', 'INSCRIPCION SEM. FEB-JUL 2010', 1600.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-006-001-035', 'INSCRIPCION SEM. AGO 09-ENE 10', 1600.00, NULL, '5200', true, 'Inscripción', true),
  ('5200-006-001-036', 'REINSCRIPCION SEM. FEBRERO-JULIO 2026', 800.00, NULL, '5200', true, 'Inscripción', true),
  ('5400-001-002-0016', 'INSCRIPCION FEBRERO-JULIO 2011', 1600.00, NULL, '5400', true, 'Inscripción', true),
  ('5400-001-002-0028', 'INSCRIPCION AGOSTO 2011-ENERO 2012', 1600.00, NULL, '5400', true, 'Inscripción', true),
  ('5400-001-002-0037', 'INSCRIPCION SEM. FEB-JUL 2012', 1700.00, 20, '5400', true, 'Inscripción', true),
  ('5400-001-002-0063', 'REINSCRIPCION SEM. AGOSTO 2023-ENERO 2024', 500.00, NULL, '5400', true, 'Inscripción', true),
  ('5400-001-002-0071', 'INSCRIPCION SEM. FEB/JUL 2013', 1700.00, NULL, '5400', true, 'Inscripción', true),
  ('5400-001-002-0079', 'INSCRIPCION SEM. AGO. 2013-ENE. 2014', 1700.00, NULL, '5400', true, 'Inscripción', true),
  ('5400-001-002-0097', 'REINSCRIPCION SEM. FEBRERO-JULIO 2025', 500.00, NULL, '5400', true, 'Inscripción', true),
  ('5400-001-002-0098', 'INSCRIPCION SEM FEB-JUL 2014', 1700.00, NULL, '5400', true, 'Inscripción', true),
  ('5400-001-002-0123', 'INSCRIPCION SEM. AGOSTO 2014-ENERO 2015', 1700.00, NULL, '5400', true, 'Inscripción', true),
  ('5400-001-002-0125', 'INSCRIPCION SEM. FEBRERO-JULIO 2015', 1700.00, NULL, '5400', true, 'Inscripción', true),
  ('5400-001-002-0143', 'INSCRIPCION SEM. AGOSTO 2015-ENERO 2016', 1700.00, 1, '5400', true, 'Inscripción', true),
  ('5400-001-002-0147', 'INSCRIPCION SEM. FEBRERO-JULIO 2016', 1700.00, 2, '5400', true, 'Inscripción', true),
  ('5400-001-002-0156', 'INSCRIPCION POSGRADO', 1700.00, NULL, '5400', true, 'Inscripción', true),
  ('5400-001-002-0160', 'INSCRIPCION SEM. AGOSTO 2017-ENERO 2018', 1700.00, 4, '5400', true, 'Inscripción', true),
  ('5400-001-002-0163', 'INSCRIPCIÓN SEM. FEBRERO-JULIO 2018', 1700.00, 5, '5400', true, 'Inscripción', true),
  ('5400-001-002-0224', 'INSCRIPCION NVO. INGRESO SEM. AGO 2019-ENERO 2020', 500.00, NULL, '5400', true, 'Inscripción', true),
  ('5400-001-002-0225', 'REINSCRIPCION SEM. FEB-JUL 2020', 500.00, 10, '5400', true, 'Inscripción', true),
  ('5400-001-002-0231', 'REINSCRIPCION AGOSTO 2020 - ENERO 2021', 500.00, 11, '5400', true, 'Inscripción', true),
  ('5400-001-002-0232', 'INSCRIPCION NVO. INGRESO AGOSTO 2020 - ENERO 2021', 500.00, NULL, '5400', true, 'Inscripción', true),
  ('5400-001-002-0236', 'REINSCRIPCION SEM. FEB-JUL 2021', 500.00, 13, '5400', true, 'Inscripción', true),
  ('5400-001-002-0258', 'REINSCRIPCION SEM. AGOSTO 2021-ENERO 2022', 500.00, 15, '5400', true, 'Inscripción', true),
  ('5400-001-002-0259', 'INSCRIPCION NVO. INGRESO SEM. AGO-2021-ENERO 2022', 500.00, NULL, '5400', true, 'Inscripción', true),
  ('5400-001-002-0262', 'REINSCRIPCION SEM. FEB-JUL 2022', 500.00, NULL, '5400', true, 'Inscripción', true),
  ('5400-001-003-0017', 'INSCRIPCION AGOSTO 2016 ENERO 2017', 1700.00, NULL, '5400', true, 'Inscripción', true),
  ('5400-001-003-0022', 'REINSCRIPCION SEM. AGOSTO 2024-ENERO 2025', 500.00, NULL, '5400', true, 'Inscripción', true),
  ('5400-001-003-0024', 'INSCRIPCION SEM. FEBRERO-JULIO 2017', 1700.00, 3, '5400', true, 'Inscripción', true),
  ('5400-001-003-0084', 'INSCRIPCION SEM. AGOSTO 2018-ENERO 2019', 1700.00, 6, '5400', true, 'Inscripción', true),
  ('5400-001-003-0090', 'INSCRIPCION SEM. FEBRERO-JULIO 2019', 1700.00, 7, '5400', true, 'Inscripción', true),
  ('5400-001-003-0094', 'REINSCRIPCION SEM. AGOSTO 2026-ENERO 2027', 800.00, 8, '5400', true, 'Inscripción', true),
  ('54001-002-009', 'INSCRIPCION AGOSTO 2010-ENERO 2011', 1600.00, NULL, '5400', true, 'Inscripción', true),
  -- Operational & student services concepts
  ('INSC-VIG',         'Pago de Inscripción (Periodo Vigente 2026)',   2600.00, 50, '5200', true,  'Inscripción', true),
  ('INSC-ANT',         'Adeudo Inscripción Periodos Anteriores',       2450.00, 51, '5200', false, 'Inscripción', true),
  ('5400-001-003-0001', 'REINSCRIPCION SEMESTRAL TARIFA 1',             520.00, 60, '5400', false, 'Inscripción', true),
  ('5400-001-003-0002', 'REINSCRIPCION SEMESTRAL TARIFA 2',             620.00, 61, '5400', false, 'Inscripción', true),
  ('5400-001-003-0003', 'REINSCRIPCION SEMESTRAL TARIFA 3',             620.00, 62, '5400', false, 'Inscripción', true),
  ('5400-001-003-0005', 'REINSCRIPCION SEMESTRAL TARIFA 5',             700.00, 63, '5400', false, 'Inscripción', true),
  ('5400-001-003-0006', 'REINSCRIPCION SEMESTRAL TARIFA 6',             700.00, 64, '5400', false, 'Inscripción', true),
  ('5400-001-002-0038', 'DESCUENTO DE INSCRIPCION',                    -100.00, 65, '5400', false, 'Descuentos',  true),
  ('KARD',              'KARDEX OFICIAL',                               150.00, 70, '5200', false, 'Constancias',  true),
  ('CONST',             'Constancia de Estudios con Calificaciones',    100.00, 71, '5200', false, 'Constancias',  true),
  ('VERA',              'Curso de Verano (Por Asignatura)',            1200.00, 72, '5200', false, 'Cursos',       true),
  ('TITU',              'Trámite de Titulación Integral y Cédula',     3800.00, 73, '5200', false, 'Otros',        true)
ON CONFLICT (code) DO UPDATE SET
    description = EXCLUDED.description,
    base_fee = EXCLUDED.base_fee,
    category = EXCLUDED.category;

-- ----------------------------------------------------------------------------
-- 5. RECEIPT FOLIOS BASE
-- ----------------------------------------------------------------------------
INSERT INTO receipt_folios (folio_number, series)
SELECT s, 'A' FROM generate_series(128450, 128500) s
ON CONFLICT (folio_number) DO NOTHING;

COMMIT;
