# Verifies every Recall expert has a skill, an agent file, and a panel row.
$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$agentsDir = Join-Path $root '.cursor/agents'
$skillsDir = Join-Path $root '.cursor/skills'
$panelPath = Join-Path $root '.cursor/skills/recall-expert-harness/panel.md'
$panelText = Get-Content -Raw -Path $panelPath

$expected = @(
    'dotnet-architect',
    'vectors-embeddings',
    'nlp',
    'maui-platform',
    'ux-ui',
    'privacy-local-first',
    'performance',
    'unit-tests',
    'retrieval-qa',
    'docs-mkdocs'
)

$failures = New-Object System.Collections.Generic.List[string]

foreach ($name in $expected) {
    $agent = Join-Path $agentsDir "$name.md"
    $skill = Join-Path $skillsDir "$name/SKILL.md"
    if (-not (Test-Path -LiteralPath $agent)) {
        $failures.Add("Missing agent file: $name")
    }
    if (-not (Test-Path -LiteralPath $skill)) {
        $failures.Add("Missing skill: $name")
    }
    if ($panelText -notmatch [regex]::Escape("``$name``")) {
        $failures.Add("Panel is missing expert id $name")
    }
}

Get-ChildItem -LiteralPath $agentsDir -Filter '*.md' | ForEach-Object {
    if ($expected -notcontains $_.BaseName) {
        $failures.Add("Agent $($_.BaseName) is not in the expected panel list")
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Output $_ }
    exit 1
}

Write-Output "Expert panel OK ($($expected.Count) experts)"
