# Verifies every Recall expert has a skill, an agent file, and a panel row.
$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$agentsDir = Join-Path $root '.cursor/agents'
$skillsDir = Join-Path $root '.cursor/skills'
$harnessDir = Join-Path $skillsDir 'recall-expert-harness'
$panelPath = Join-Path $harnessDir 'panel.md'
$panelText = Get-Content -Raw -Path $panelPath
$planningText = Get-Content -Raw -Path (Join-Path $harnessDir 'planning-chain.md')
$ruleText = Get-Content -Raw -Path (Join-Path $root '.cursor/rules/recall-harness.mdc')

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
    if (-not (Test-Path -LiteralPath $skill)) {
        $failures.Add("Missing skill: $name")
    }
    else {
        $skillText = Get-Content -Raw -Path $skill
        if ($skillText -notmatch '## Panel duty') {
            $failures.Add("Skill $name has no panel duty")
        }
    }
    if ($panelText -notmatch [regex]::Escape("``$name``")) {
        $failures.Add("Panel is missing expert id $name")
    }
    if ($planningText -notmatch [regex]::Escape("``$name``")) {
        $failures.Add("Planning chain is missing expert id $name")
    }
    if (-not (Test-Path -LiteralPath $agent)) {
        $failures.Add("Missing agent file: $name")
    }
    else {
        $agentText = Get-Content -Raw -Path $agent
        if ($agentText -notmatch 'sign-off\.md') {
            $failures.Add("Agent $name does not require a sign-off")
        }
    }
}

foreach ($requiredName in @('development-rules.md', 'test-honesty.md', 'sign-off.md', 'planning-chain.md', 'canon.md')) {
    if (-not (Test-Path -LiteralPath (Join-Path $harnessDir $requiredName))) {
        $failures.Add("Missing harness file $requiredName")
    }
}

if ($ruleText -notmatch '@recall:' -or $ruleText -notmatch '@reacall:') {
    $failures.Add('Always-apply rule does not trigger both @recall: and @reacall:')
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
