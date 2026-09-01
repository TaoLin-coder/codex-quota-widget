$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$source = Join-Path $root 'src'
$output = Join-Path $root 'dist'
$manifest = Join-Path $root 'app.manifest'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$wpf = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF'

New-Item -ItemType Directory -Force -Path $output | Out-Null

$references = @(
    (Join-Path $wpf 'WindowsBase.dll'),
    (Join-Path $wpf 'PresentationCore.dll'),
    (Join-Path $wpf 'PresentationFramework.dll'),
    'System.dll',
    'System.Core.dll',
    'System.Xaml.dll',
    'System.Web.Extensions.dll',
    'System.Windows.Forms.dll',
    'System.Drawing.dll'
)

$arguments = @(
    '/nologo',
    '/target:winexe',
    '/platform:x64',
    '/optimize+',
    ('/win32manifest:' + $manifest),
    '/main:CodexQuotaWidget.Program',
    ('/out:' + (Join-Path $output 'CodexQuotaWidget.exe'))
)
$arguments += $references | ForEach-Object { '/reference:' + $_ }
$arguments += Get-ChildItem -LiteralPath $source -Filter '*.cs' | Select-Object -ExpandProperty FullName

& $compiler $arguments
if ($LASTEXITCODE -ne 0) { throw "Compiler exited with code $LASTEXITCODE" }

Write-Host (Join-Path $output 'CodexQuotaWidget.exe')
