Add-Type -AssemblyName System.IO.Compression.FileSystem

$zipPath = "C:/Users/Usuario/.gemini/antigravity/brain/0dbfb61d-6959-4e2c-b819-94fc2eb95fa2/.user_uploaded/media_1795297236267_6b89c64c.xlsx"
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)

$ssEntry = $zip.GetEntry("xl/sharedStrings.xml")
$sharedStrings = @()
if ($ssEntry) {
    $stream = $ssEntry.Open()
    $reader = New-Object System.IO.StreamReader($stream)
    [xml]$ssXml = $reader.ReadToEnd()
    $reader.Close()
    foreach ($si in $ssXml.sst.si) {
        $t = ""
        if ($si.t) { $t = $si.t }
        elseif ($si.r) { foreach ($r in $si.r) { $t += $r.t } }
        $sharedStrings += $t
    }
}

$sEntry = $zip.GetEntry("xl/worksheets/sheet1.xml")
$sStream = $sEntry.Open()
$sReader = New-Object System.IO.StreamReader($sStream)
[xml]$sXml = $sReader.ReadToEnd()
$sReader.Close()
$zip.Dispose()

$rows = $sXml.worksheet.sheetData.row

# Solo las 8 carreras oficiales iniciales
$carrerasMap = @{
    "64" = "Lic. Informática"
    "65" = "Ing. Mecánica"
    "67" = "Ing. Industrial"
    "68" = "Ing. Electrónica"
    "69" = "Ing. Informática"
    "70" = "Ing. Gestión Emp."
    "71" = "Ing. Energías Renovables"
    "74" = "Ing. Mecatrónica"
}

$uniqueAlumnos = @{}
$uniqueFolios = @{}
$uniqueCuentas = @{}
$foliosList = [System.Collections.Generic.List[String]]::new()
$recibosData = @{}

for ($i = 1; $i -lt $rows.Count; $i++) {
    $r = $rows[$i]
    $cells = @{}
    foreach ($c in $r.c) {
        $col = $c.r -replace '\d+',''
        $val = $c.v
        if ($c.t -eq "s") { $val = $sharedStrings[[int]$val] }
        $cells[$col] = $val
    }
    
    $folStr = if ($cells['A']) { $cells['A'].ToString().Trim() } else { "0" }
    $folNum = 0
    [int64]::TryParse($folStr, [ref]$folNum) | Out-Null
    
    $mat = if ($cells['B']) { $cells['B'].ToString().Trim() } else { "" }
    $nom = if ($cells['C']) { $cells['C'].ToString().Trim().Replace("'", "''") } else { "" }
    $sem = if ($cells['D']) { [int]$cells['D'] } else { 1 }
    $car = if ($cells['E']) { $cells['E'].ToString().Trim() } else { "" }
    $tur = if ($cells['F']) { $cells['F'].ToString().Trim() } else { "Matutino" }
    $fec = if ($cells['G']) { $cells['G'] } else { "42528" }
    $cta = if ($cells['H']) { $cells['H'].ToString().Trim() } else { "5400-001-002-0001" }
    $can = if ($cells['I']) { [int]$cells['I'] } else { 1 }
    $pre = if ($cells['J']) { [decimal]$cells['J'] } else { 0.0 }
    $sub = if ($cells['K']) { [decimal]$cells['K'] } else { 0.0 }
    $sta = if ($cells['L']) { [int]$cells['L'] } else { 1 }
    $tip = if ($cells['N']) { $cells['N'].ToString().Trim() } else { "P" }
    $dep = if ($cells['O']) { $cells['O'].ToString().Trim() } else { "0" }

    if ($tur -ne "Matutino" -and $tur -ne "Nocturno" -and $tur -ne "Vespertino" -and $tur -ne "Sabatino") {
        $tur = "Matutino"
    }

    # Si no tiene carrera válida de las 8 iniciales, es NULL y 'Sin Asignar'
    $cId = if ($carrerasMap.ContainsKey($car)) { $car } else { "NULL" }
    $cNom = if ($carrerasMap.ContainsKey($car)) { $carrerasMap[$car] } else { "Sin Asignar" }

    # Si no tiene matrícula o es '0', se establece como 'SIN_ASIGNAR'
    if ($mat -eq "" -or $mat -eq "0") {
        $mat = "SIN_ASIGNAR"
        $cId = "NULL"
        $cNom = "Sin Asignar"
        if ($nom -eq "") { $nom = "SIN ASIGNAR" }
    } else {
        if ($nom -eq "") { $nom = "ALUMNO $mat" }
    }

    if ($sem -lt 1) { $sem = 1 }
    if ($sem -gt 15) { $sem = 15 }

    # Date parsing from OADate
    $fechaSql = "2016-06-07 09:00:00-06"
    $fecNum = 0
    if ([double]::TryParse($fec, [ref]$fecNum)) {
        try {
            $dt = [DateTime]::FromOADate($fecNum)
            $fechaSql = $dt.ToString("yyyy-MM-dd HH:mm:ss") + "-06"
        } catch {}
    }

    if (-not $uniqueAlumnos.ContainsKey($mat)) {
        $uniqueAlumnos[$mat] = @{
            Matricula = $mat
            Nombre = $nom
            CarreraId = $cId
            CarreraNombre = $cNom
            Semestre = $sem
            Turno = $tur
        }
    }

    if ($folNum -gt 0) {
        $uniqueFolios[$folNum] = $true
    }

    if ($cta -and $cta -ne "0") {
        $uniqueCuentas[$cta] = $pre
    }

    if (-not $recibosData.ContainsKey($folStr)) {
        $foliosList.Add($folStr)
        $recibosData[$folStr] = [System.Collections.Generic.List[Object]]::new()
    }
    
    $rowItem = @{
        Folio = $folStr
        FolioNum = $folNum
        Matricula = $mat
        Nombre = $nom
        Semestre = $sem
        CarreraId = $cId
        CarreraNombre = $cNom
        Turno = $tur
        FechaSql = $fechaSql
        Cuenta = $cta
        Cantidad = $can
        Precio = $pre
        Subtotal = $sub
        Status = $sta
        Tipo = $tip
        Deposito = $dep
    }
    $recibosData[$folStr].Add($rowItem)
}

Write-Host "Unique Alumnos con matrícula: $($uniqueAlumnos.Count)"
Write-Host "Unique Folios: $($uniqueFolios.Count)"
Write-Host "Unique Cuentas: $($uniqueCuentas.Count)"
Write-Host "Total Receipts: $($foliosList.Count)"

$sqlFile = "scripts/seed_previos_completo.sql"
$writer = New-Object System.IO.StreamWriter($sqlFile, $false, [System.Text.Encoding]::UTF8)

$writer.WriteLine("-- ============================================================================")
$writer.WriteLine("-- SEED COMPLETO DE PREVIOS (HOJA PREVIO EXCEL TECNM)")
$writer.WriteLine("-- Carreras exclusivas: 8 carreras oficiales (sin carrera 0)")
$writer.WriteLine("-- Alumnos sin matrícula normalizados como 'SIN_ASIGNAR'")
$writer.WriteLine("-- ============================================================================")
$writer.WriteLine("BEGIN;")
$writer.WriteLine("")

# 1. Conceptos de cobro complementarios
$writer.WriteLine("-- 1. CONCEPTOS DE COBRO COMPLEMENTARIOS DE LA HOJA PREVIO")
$cuentaLines = @()
foreach ($cta in $uniqueCuentas.Keys) {
    $costo = [decimal]$uniqueCuentas[$cta]
    $ref = if ($cta.StartsWith("5200")) { "5200" } else { "5400" }
    $cuentaLines += "('$cta', 'Trámite o Cuota Institucional $cta', $costo.00, NULL, '$ref', true, 'Inscripción', true)"
}
$writer.WriteLine("INSERT INTO conceptos_cobro (clave, descripcion, cuota_base, orden, cuenta_referencia, aplica_beca, categoria, is_active) VALUES")
$writer.WriteLine(($cuentaLines -join ",`n"))
$writer.WriteLine("ON CONFLICT (clave) DO NOTHING;")
$writer.WriteLine("")

# 2. Alumno SIN_ASIGNAR
$writer.WriteLine("-- 2. REGISTRO MAESTRO PARA ALUMNOS SIN MATRÍCULA ASIGNADA")
$writer.WriteLine("INSERT INTO alumnos (matricula, nombre_completo, id_carrera, carrera, semestre, turno, estatus, tiene_beca, porcentaje_beca)")
$writer.WriteLine("VALUES ('SIN_ASIGNAR', 'SIN ASIGNAR', NULL, 'Sin Asignar', 1, 'Matutino', 'Inscrito', false, 0.00)")
$writer.WriteLine("ON CONFLICT (matricula) DO NOTHING;")
$writer.WriteLine("")

# 3. Padrón de Alumnos con matrícula
$writer.WriteLine("-- 3. PADRÓN DE ALUMNOS CON MATRÍCULA")
$alumnosKeys = @($uniqueAlumnos.Keys)
for ($i = 0; $i -lt $alumnosKeys.Count; $i += 500) {
    $batch = @()
    $limit = [Math]::Min($i + 500, $alumnosKeys.Count)
    for ($j = $i; $j -lt $limit; $j++) {
        $a = $uniqueAlumnos[$alumnosKeys[$j]]
        $m = $a.Matricula
        if ($m -eq "SIN_ASIGNAR") { continue }
        $n = $a.Nombre
        $cid = $a.CarreraId
        $cnom = $a.CarreraNombre
        $s = $a.Semestre
        $t = $a.Turno
        $batch += "('$m', '$n', $cid, '$cnom', $s, '$t', 'Inscrito', false, 0.00)"
    }
    if ($batch.Count -gt 0) {
        $writer.WriteLine("INSERT INTO alumnos (matricula, nombre_completo, id_carrera, carrera, semestre, turno, estatus, tiene_beca, porcentaje_beca) VALUES")
        $writer.WriteLine(($batch -join ",`n"))
        $writer.WriteLine("ON CONFLICT (matricula) DO NOTHING;")
        $writer.WriteLine("")
    }
}

# 4. Folios en lotes de 1000
$writer.WriteLine("-- 4. CONTROL DE FOLIOS")
$foliosKeys = @($uniqueFolios.Keys)
for ($i = 0; $i -lt $foliosKeys.Count; $i += 1000) {
    $batch = @()
    $limit = [Math]::Min($i + 1000, $foliosKeys.Count)
    for ($j = $i; $j -lt $limit; $j++) {
        $f = $foliosKeys[$j]
        $batch += "($f, 'A')"
    }
    $writer.WriteLine("INSERT INTO folios (numero_folio, serie) VALUES")
    $writer.WriteLine(($batch -join ",`n"))
    $writer.WriteLine("ON CONFLICT (numero_folio) DO NOTHING;")
    $writer.WriteLine("")
}

# 5. Recibos, Detalles y Movimientos de Pago en lotes de 400
$writer.WriteLine("-- 5. RECIBOS, DETALLES Y MOVIMIENTOS DE PAGO")
$guidIdx = 0

for ($i = 0; $i -lt $foliosList.Count; $i += 400) {
    $batchRecibos = @()
    $batchDetalles = @()
    $batchMovimientos = @()
    $limit = [Math]::Min($i + 400, $foliosList.Count)
    
    for ($j = $i; $j -lt $limit; $j++) {
        $guidIdx++
        $reciboUuid = "b0000000-0000-0000-0000-" + $guidIdx.ToString("D12")
        $folKey = $foliosList[$j]
        $items = $recibosData[$folKey]
        $first = $items[0]
        
        $subtotal = 0.0
        $descuento = 0.0
        $total = 0.0
        foreach ($it in $items) {
            $total += $it.Subtotal
            if ($it.Subtotal -lt 0) {
                $descuento += [Math]::Abs($it.Subtotal)
            } else {
                $subtotal += $it.Subtotal
            }
        }
        
        $metodoPago = if ($first.Cuenta.StartsWith("5200")) { "EFECTIVO" } else { "DEPOSITO_BANCARIO" }
        $refBancaria = if ($first.Deposito -and $first.Deposito -ne "0") { $first.Deposito } else { "0" }
        $statusVal = $first.Status
        $estadoVal = if ($statusVal -eq 0) { "CANCELADO" } else { "PREVIO" }
        
        $mat = $first.Matricula
        $nom = $first.Nombre
        $carNom = $first.CarreraNombre
        $sem = $first.Semestre
        $tur = $first.Turno
        $fec = $first.FechaSql
        
        $batchRecibos += "('$reciboUuid', '$folKey', '$fec', '$mat', '$nom', '$carNom', $sem, '$tur', '$metodoPago', '$refBancaria', $subtotal.00, $descuento.00, $total.00, $statusVal, '$estadoVal', 'PREVIO', 'cajero1', 'Cajero Turno Matutino')"
        
        foreach ($it in $items) {
            $cKey = $it.Cuenta
            $cant = $it.Cantidad
            $precio = $it.Precio
            $sub = $it.Subtotal
            $batchDetalles += "('$reciboUuid', (SELECT id FROM conceptos_cobro WHERE clave = '$cKey' LIMIT 1), 'Partida Folio $folKey ($cKey)', $cant, $precio.00, 0.00, $sub.00, '2016-1')"
        }
        
        if ($total -gt 0) {
            $batchMovimientos += "('$reciboUuid', '$metodoPago', '$refBancaria', $total.00, '$fec', 'cajero1')"
        }
    }
    
    $writer.WriteLine("INSERT INTO recibos (id, folio_administrativo, fecha_operacion, alumno_matricula, alumno_nombre, alumno_carrera, alumno_semestre, alumno_turno, metodo_pago, referencia_bancaria, subtotal, descuento_total, total, status, estado, tipo_recibo, cajero_usuario, cajero_nombre) VALUES")
    $writer.WriteLine(($batchRecibos -join ",`n"))
    $writer.WriteLine("ON CONFLICT (id) DO NOTHING;")
    $writer.WriteLine("")
    
    if ($batchDetalles.Count -gt 0) {
        $writer.WriteLine("INSERT INTO detalles_recibo (recibo_id, concepto_id, descripcion, cantidad, cuota_unitario, descuento_beca, importe_total, periodo_ano) VALUES")
        $writer.WriteLine(($batchDetalles -join ",`n"))
        $writer.WriteLine(";")
        $writer.WriteLine("")
    }
    
    if ($batchMovimientos.Count -gt 0) {
        $writer.WriteLine("INSERT INTO movimientos_pago (recibo_id, forma_pago, referencia_bancaria, monto, fecha_movimiento, cajero_usuario) VALUES")
        $writer.WriteLine(($batchMovimientos -join ",`n"))
        $writer.WriteLine(";")
        $writer.WriteLine("")
    }
}

$writer.WriteLine("-- 6. SINCRONIZACIÓN DE LA SECUENCIA DE RECIBOS")
$writer.WriteLine("SELECT setval('recibos_numero_recibo_seq', COALESCE((SELECT MAX(numero_recibo) FROM recibos), 128450));")
$writer.WriteLine("")
$writer.WriteLine("COMMIT;")

$writer.Close()
Write-Host "Generated $sqlFile successfully! Total receipts written: $guidIdx"
