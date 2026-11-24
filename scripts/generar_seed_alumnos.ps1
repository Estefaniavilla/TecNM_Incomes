Add-Type -AssemblyName System.IO.Compression.FileSystem

$excelFiles = @(
    "C:/Users/Usuario/.gemini/antigravity/brain/0dbfb61d-6959-4e2c-b819-94fc2eb95fa2/.user_uploaded/media_1795490414614_a0bb5ec9.xlsx",
    "C:/Users/Usuario/.gemini/antigravity/brain/0dbfb61d-6959-4e2c-b819-94fc2eb95fa2/.user_uploaded/media_1795493090581_6d7d7622.xlsx",
    "C:/Users/Usuario/.gemini/antigravity/brain/0dbfb61d-6959-4e2c-b819-94fc2eb95fa2/.user_uploaded/media_1795493808177_8a984a3d.xlsx",
    "C:/Users/Usuario/.gemini/antigravity/brain/0dbfb61d-6959-4e2c-b819-94fc2eb95fa2/.user_uploaded/media_1795495526395_a3c63928.xlsx"
)
$outputSql = "scripts/seed_alumnos_padron.sql"

$enc1252 = [System.Text.Encoding]::GetEncoding(1252)
$encUtf8 = [System.Text.Encoding]::UTF8

function Fix-Encoding([string]$text) {
    if ([string]::IsNullOrEmpty($text)) { return "" }
    try {
        if ($text.Contains([char]0x00C3)) {
            $bytes = $enc1252.GetBytes($text)
            $fixed = $encUtf8.GetString($bytes)
            return $fixed
        }
    } catch {}
    return $text
}

# Las 8 carreras oficiales del TecNM
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

$seenMatriculas = @{}
$sqlRows = [System.Collections.Generic.List[String]]::new()

foreach ($zipPath in $excelFiles) {
    if (-not (Test-Path $zipPath)) { continue }
    
    $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)

    $ssEntry = $zip.GetEntry("xl/sharedStrings.xml")
    $sharedStrings = @()
    if ($ssEntry) {
        $stream = $ssEntry.Open()
        $reader = New-Object System.IO.StreamReader($stream)
        [xml]$ssXml = $reader.ReadToEnd()
        $reader.Close()
        foreach ($si in $ssXml.sst.si) {
            $rawT = $si.InnerText
            $sharedStrings += (Fix-Encoding $rawT)
        }
    }

    $sEntry = $zip.GetEntry("xl/worksheets/sheet1.xml")
    $sStream = $sEntry.Open()
    $sReader = New-Object System.IO.StreamReader($sStream)
    [xml]$sXml = $sReader.ReadToEnd()
    $sReader.Close()
    $zip.Dispose()

    $rows = $sXml.worksheet.sheetData.row

    for ($i = 1; $i -lt $rows.Count; $i++) {
        $r = $rows[$i]
        $cells = @{}
        foreach ($c in $r.c) {
            $col = $c.r -replace '\d+',''
            $val = $c.v
            if ($c.t -eq "s") { $val = $sharedStrings[[int]$val] }
            $cells[$col] = $val
        }

        $mat = if ($cells['A']) { $cells['A'].ToString().Trim() } else { "" }
        if ([string]::IsNullOrWhiteSpace($mat) -or $mat.ToLower() -eq "matricula") { continue }
        if ($seenMatriculas.ContainsKey($mat)) { continue }
        $seenMatriculas[$mat] = $true

        $nom = if ($cells['B']) { $cells['B'].ToString().Trim().Replace("'", "''") } else { "ALUMNO TECNM" }
        $nombres = if ($cells['C']) { $cells['C'].ToString().Trim().Replace("'", "''") } else { $nom }
        
        $sexo = if ($cells['D']) { $cells['D'].ToString().Trim().ToUpper() } else { "" }
        $sexoSql = if ($sexo -eq "M" -or $sexo -eq "F") { "'$sexo'" } else { "NULL" }

        $idCarStr = if ($cells['E']) { $cells['E'].ToString().Trim() } else { "0" }
        $idCarSql = "NULL"
        $carreraNom = "Sin Asignar"
        if ($carrerasMap.ContainsKey($idCarStr)) {
            $idCarSql = "$idCarStr"
            $carreraNom = $carrerasMap[$idCarStr]
        }

        $semStr = if ($cells['F']) { $cells['F'].ToString().Trim() } else { "1" }
        $sem = 1
        [int]::TryParse($semStr, [ref]$sem) | Out-Null
        if ($sem -le 0 -or $sem -gt 30) { $sem = 1 }

        $ctaStr = if ($cells['G']) { $cells['G'].ToString().Trim() } else { "" }
        $cta = 0
        $ctaSql = if ([int]::TryParse($ctaStr, [ref]$cta)) { "$cta" } else { "NULL" }

        $sqlRows.Add("('$mat', '$nom', '$nombres', NULL, $sexoSql, $idCarSql, '$carreraNom', $sem, 'Matutino', $ctaSql, 'Inscrito', false, 0.00)")
    }
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("-- ============================================================================")
[void]$sb.AppendLine("-- Carga y Actualizacion de Alumnos Oficiales TecNM (Padron Consolidado)")
[void]$sb.AppendLine("-- Total de alumnos unicos procesados: " + $sqlRows.Count)
[void]$sb.AppendLine("-- ============================================================================")
[void]$sb.AppendLine("INSERT INTO alumnos (matricula, nombre_completo, nombres, apellidos, sexo, id_carrera, carrera, semestre, turno, cuenta_a, estatus, tiene_beca, porcentaje_beca)")
[void]$sb.AppendLine("VALUES")

for ($j = 0; $j -lt $sqlRows.Count; $j++) {
    $suffix = if ($j -eq $sqlRows.Count - 1) { "" } else { "," }
    [void]$sb.AppendLine($sqlRows[$j] + $suffix)
}

[void]$sb.AppendLine("ON CONFLICT (matricula) DO UPDATE SET")
[void]$sb.AppendLine("    cuenta_a = COALESCE(EXCLUDED.cuenta_a, alumnos.cuenta_a),")
[void]$sb.AppendLine("    id_carrera = COALESCE(EXCLUDED.id_carrera, alumnos.id_carrera),")
[void]$sb.AppendLine("    carrera = CASE WHEN EXCLUDED.id_carrera IS NOT NULL THEN EXCLUDED.carrera ELSE alumnos.carrera END,")
[void]$sb.AppendLine("    semestre = EXCLUDED.semestre;")

[System.IO.File]::WriteAllText($outputSql, $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Output "Exito: $($sqlRows.Count) alumnos consolidados guardados en $outputSql"
