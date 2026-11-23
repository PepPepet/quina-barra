# Quina Barra! · compila QuinaBarra.exe amb el compilador de C# del .NET Framework que porta el Windows.
# Ús: powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
# Sortida: ..\exe\QuinaBarra.exe. No cal instal·lar res ni baixar res.
$ErrorActionPreference = 'Stop'

$src     = $PSScriptRoot
$arrel   = Split-Path $src -Parent
$sortida = Join-Path $arrel 'exe'
$fw      = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$wpf     = Join-Path $fw 'WPF'
$csc     = Join-Path $fw 'csc.exe'
if (-not (Test-Path $csc)) { throw "No trobo el compilador: $csc" }
if (-not (Test-Path $sortida)) { New-Item -ItemType Directory -Path $sortida | Out-Null }

$opcions = @(
    '/nologo', '/target:winexe', '/platform:anycpu', '/optimize+', '/codepage:65001', '/utf8output',
    "/out:$(Join-Path $sortida 'QuinaBarra.exe')",
    "/win32manifest:$(Join-Path $src 'QuinaBarra.manifest')",
    "/win32icon:$(Join-Path $src 'QuinaBarra.ico')",
    "/reference:$(Join-Path $wpf 'PresentationFramework.dll')",
    "/reference:$(Join-Path $wpf 'PresentationCore.dll')",
    "/reference:$(Join-Path $wpf 'WindowsBase.dll')",
    "/reference:$(Join-Path $fw 'System.Xaml.dll')",
    "/reference:$(Join-Path $fw 'System.Web.Extensions.dll')",
    "/reference:$(Join-Path $fw 'System.Windows.Forms.dll')",
    "/reference:$(Join-Path $fw 'System.Drawing.dll')",
    "/reference:$(Join-Path $fw 'System.Xml.dll')",
    "/reference:$(Join-Path $fw 'System.Core.dll')",
    "/reference:$(Join-Path $fw 'System.dll')"
)
# Els XAML van dins de l'exe com a recursos (TempsBarra.<nom>.xaml)
foreach ($x in Get-ChildItem (Join-Path $src 'xaml') -Filter *.xaml) { $opcions += "/resource:$($x.FullName),TempsBarra.$($x.Name)" }
$opcions += (Get-ChildItem $src -Filter *.cs | ForEach-Object { $_.FullName })

& $csc @opcions
if ($LASTEXITCODE -ne 0) { throw "La compilació ha fallat (codi $LASTEXITCODE)" }
$exe = Get-Item (Join-Path $sortida 'QuinaBarra.exe')
Write-Host ("Fet: {0} ({1:N0} bytes)" -f $exe.FullName, $exe.Length)
