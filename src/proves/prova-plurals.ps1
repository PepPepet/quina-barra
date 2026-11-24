# Quina Barra! · compila i passa les proves dels plurals, de les llengües de reserva i dels codis de llengua.
# Ús: powershell -NoProfile -ExecutionPolicy Bypass -File proves\prova-plurals.ps1
# No toca l'exe del giny: compila un exe de proves a la carpeta temporal i l'esborra en acabar.
$ErrorActionPreference = 'Stop'

$proves = $PSScriptRoot
$src    = Split-Path $proves -Parent
$fw     = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$csc    = Join-Path $fw 'csc.exe'
$exe    = Join-Path ([IO.Path]::GetTempPath()) ('QuinaBarra-proves-' + [Guid]::NewGuid().ToString('N') + '.exe')

$opcions = @('/nologo', '/target:exe', '/optimize+', '/codepage:65001', '/utf8output', "/out:$exe",
    "/reference:$(Join-Path $fw 'System.Web.Extensions.dll')", "/reference:$(Join-Path $fw 'System.Xml.dll')",
    "/reference:$(Join-Path $fw 'System.Core.dll')", "/reference:$(Join-Path $fw 'System.dll')")
foreach ($t in Get-ChildItem (Join-Path $src 'textos') -Filter *.json) { $opcions += "/resource:$($t.FullName),TempsBarra.textos.$($t.Name)" }
foreach ($p in Get-ChildItem (Join-Path $src 'plurals') -Filter *.xml) { $opcions += "/resource:$($p.FullName),TempsBarra.plurals.$($p.Name)" }
$opcions += @((Join-Path $src 'Textos.cs'), (Join-Path $src 'Plurals.cs'), (Join-Path $src 'Json.cs'), (Join-Path $proves 'ProvesPlurals.cs'))

& $csc @opcions
if ($LASTEXITCODE -ne 0) { throw "La compilació de les proves ha fallat (codi $LASTEXITCODE)" }
try {
    [Console]::OutputEncoding = [Text.Encoding]::UTF8
    & $exe
    $codi = $LASTEXITCODE
}
finally { Remove-Item $exe -ErrorAction SilentlyContinue }
exit $codi
