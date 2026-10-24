param([Parameter(Mandatory = $true)][string]$MsgFile)
if (-not (Test-Path -LiteralPath $MsgFile)) {
    exit 0
}
$lines = Get-Content -LiteralPath $MsgFile -ErrorAction SilentlyContinue
if ($null -eq $lines) {
    exit 0
}
$filtered = $lines | Where-Object { $_ -notmatch '(?i)^\s*Co-authored-by:\s*Cursor\s*<cursoragent@cursor\.com>\s*$' }
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllLines($MsgFile, [string[]]$filtered, $utf8NoBom)
exit 0
