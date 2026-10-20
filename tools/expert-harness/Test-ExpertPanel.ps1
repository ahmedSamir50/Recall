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

if ($ruleText -notmatch 'Do not launch expert subagents' -or $planningText -notmatch 'Do not launch expert subagents') {
    $failures.Add('Harness still allows a subagent per expert')
}

$rulesPath = Join-Path $harnessDir 'development-rules.md'
$signOffPath = Join-Path $harnessDir 'sign-off.md'
$rulesText = Get-Content -Raw -Path $rulesPath
$signOffText = Get-Content -Raw -Path $signOffPath

$manifestChecks = @(
    @{ Name = 'always-applied rule'; Text = $ruleText; Needles = @('execution manifest', 'Do not invent a file', 'stop and amend the plan', 'that plan finishes it', 'Write the YAML once', 'The user is the approval gate', 'Do not raise a score', 'Direct:', 'An id not on that line is advisory', 'score the closed check once', 'A third pass is a loop') },
    @{ Name = 'planning chain'; Text = $planningText; Needles = @('## Execution manifest', 'not executable', 'Do not invent', 'Do not paste that YAML in the chat', 'that plan finishes it', 'The user is the approval gate', 'Do not raise a score', 'does not re-sign itself', 'score the closed check once', 'A third pass is a loop', 'Alternatives:') },
    @{ Name = 'development rules'; Text = $rulesText; Needles = @('execution manifest', 'stops and names the missing line', 'Close means done') },
    @{ Name = 'sign-off'; Text = $signOffText; Needles = @('execution manifest', 'mitigation', 'The user is the approval gate', 'honest', 'Do not raise a score', 'Direct:', 'An id not on that line is advisory', 'out-of-scope') }
    @{ Name = 'planning chain direct line'; Text = $planningText; Needles = @('The first line under `## Expert sign-offs` is `Direct:`') }
)
foreach ($check in $manifestChecks) {
    foreach ($needle in $check.Needles) {
        if ($check.Text -notmatch [regex]::Escape($needle)) {
            $failures.Add("$($check.Name) is missing plan-grain clause: $needle")
        }
    }
}

$loopPhrases = @(
    'Do not score again',
    'including ids added later'
)
foreach ($loopPhrase in $loopPhrases) {
    foreach ($loopFile in @(
        (Join-Path $root '.cursor/rules/recall-harness.mdc'),
        (Join-Path $harnessDir 'sign-off.md'),
        (Join-Path $harnessDir 'planning-chain.md'),
        (Join-Path $harnessDir 'SKILL.md'),
        (Join-Path $harnessDir 'panel.md')
    )) {
        $loopText = Get-Content -Raw -Path $loopFile
        if ($loopText.Contains($loopPhrase)) {
            $failures.Add("Thinking-loop phrase still present: $loopPhrase ($loopFile)")
        }
    }
}

$sampleAsDefinition = 'On an architecture plan that includes'
foreach ($sampleFile in @(
    (Join-Path $root '.cursor/rules/recall-harness.mdc'),
    (Join-Path $harnessDir 'sign-off.md')
)) {
    $sampleText = Get-Content -Raw -Path $sampleFile
    if ($sampleText.Contains($sampleAsDefinition)) {
        $failures.Add("Direct experts are still a sample list: $sampleFile")
    }
}

$confidenceFloor = @(
    'Confidence is 80 or higher',
    'at least 80',
    'under 80',
    'scores under 80',
    'under 96',
    'at 96 or higher',
    'round a score up to 96',
    'not held to 96'
)
$confidenceFiles = @(
    (Join-Path $root '.cursor/rules/recall-harness.mdc'),
    (Join-Path $harnessDir 'planning-chain.md'),
    (Join-Path $harnessDir 'sign-off.md'),
    (Join-Path $skillsDir 'recall-expert-harness/SKILL.md')
)
foreach ($phrase in $confidenceFloor) {
    foreach ($confidenceFile in $confidenceFiles) {
        $confidenceText = Get-Content -Raw -Path $confidenceFile
        if ($confidenceText.Contains($phrase)) {
            $failures.Add("Numeric confidence gate still present: $phrase ($confidenceFile)")
        }
    }
}

$fanOut = @(
    'Launch all ten',
    'Research the repo and InitDocs',
    'Research the repo before you answer',
    'Research the plan against this skill, the canon, and the repo'
)
$scanRoots = @(
    (Join-Path $root '.cursor/rules'),
    (Join-Path $root '.cursor/agents'),
    (Join-Path $root '.cursor/skills')
)
foreach ($phrase in $fanOut) {
    foreach ($scanRoot in $scanRoots) {
        $hits = Get-ChildItem -LiteralPath $scanRoot -Recurse -File -Include *.md,*.mdc |
            Select-String -SimpleMatch -Pattern $phrase
        foreach ($hit in $hits) {
            $failures.Add("Fan-out phrase still present: $phrase ($($hit.Path))")
        }
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

Write-Output "Fan-out ban OK (no subagent per expert, no second read of the shared files)"
Write-Output "Execution manifest OK (plan names every file, signature, branch, sentence, and test)"
Write-Output "Close means done OK (a close request finishes that parent unless a sign-off names a blocker)"
Write-Output "Sign-off once OK (one or more direct experts per plan; advisory experts still check; the user is the approval gate)"
Write-Output "Expert panel OK ($($expected.Count) experts)"
