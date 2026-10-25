# Re-download invoice PNG fixtures into Recall.Maui.UnitTests TestAssets.
$ErrorActionPreference = 'Stop'
$outDir = Join-Path $PSScriptRoot '..' 'tests' 'Recall.Maui.UnitTests' 'TestAssets' 'ocr-fixtures'
$outDir = [System.IO.Path]::GetFullPath($outDir)
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$base = 'https://raw.githubusercontent.com/SalorWorks/shopify-invoice-test-pack/main/fixtures'
$map = @{
    'ocr-en-invoice.png'    = "$base/01-simple-english/invoice.png"
    'ocr-ar-total.png'      = "$base/03-arabic/invoice.png"
    'ocr-mixed-invoice.png' = "$base/04-bilingual/invoice.png"
}

foreach ($entry in $map.GetEnumerator()) {
    $dest = Join-Path $outDir $entry.Key
    Write-Host "GET $($entry.Value) -> $dest"
    Invoke-WebRequest -Uri $entry.Value -OutFile $dest -UseBasicParsing
}

Write-Host "Done."
