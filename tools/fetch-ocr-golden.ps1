<#
.SYNOPSIS
    Fetches the OCR golden corpus into artifacts/ocr-golden.

.DESCRIPTION
    Downloads validated (proofread quality 4) Wikisource pages: 30 Arabic, 30 English and
    20 French/German/Spanish. Each page writes <lang>/<slug>-pNN.png with the scan and
    <slug>-pNN.gt.txt with the validated text, plus manifest.csv with
    path,language,isPhone,mixed,sourceUrl,sha256.

    The four demo phone captures are copied in as isPhone=true rows so the phone ceiling keeps
    real photos. A language below its count writes artifacts/ocr-golden/missing.txt and exits 1.

.EXAMPLE
    pwsh tools/fetch-ocr-golden.ps1
#>
param(
    [string]$Root = "",
    [string]$DemoRoot = "",
    [int]$ArabicCount = 30,
    [int]$EnglishCount = 30,
    [int]$EuropeanCount = 20,
    [int]$ScanWidth = 2480
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$UserAgent = "Recall-OCR-Golden-Corpus/1.0 (local OCR regression corpus; https://github.com/recall-app/recall)"
$ApiDelayMs = 350
$script:PageNamespaceCache = @{}
$ProgressPreference = "SilentlyContinue"

if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Join-Path (Split-Path $PSScriptRoot -Parent) "artifacts/ocr-golden"
}

if ([string]::IsNullOrWhiteSpace($DemoRoot)) {
    $DemoRoot = $env:RECALL_DEMO_DATASET
    if ([string]::IsNullOrWhiteSpace($DemoRoot)) {
        $DemoRoot = "D:\A_S\Recall_DemoData\recall_demo_dataset"
    }
}

New-Item -ItemType Directory -Force -Path $Root | Out-Null

function Invoke-Api {
    param([string]$Language, [string]$Query)
    Start-Sleep -Milliseconds $ApiDelayMs
    $uri = "https://$Language.wikisource.org/w/api.php?format=json&$Query"
    return Invoke-RestMethod -Uri $uri -TimeoutSec 60 -Headers @{ "User-Agent" = $UserAgent }
}

function Get-PageNamespace {
    param([string]$Language)
    if ($script:PageNamespaceCache.ContainsKey($Language)) {
        return $script:PageNamespaceCache[$Language]
    }

    $response = Invoke-Api -Language $Language -Query "action=query&meta=siteinfo&siprop=namespaces"
    $id = 106
    foreach ($property in $response.query.namespaces.PSObject.Properties) {
        $ns = $property.Value
        if ($ns.canonical -eq 'Page') {
            $id = [int]$ns.id
            break
        }
    }

    $script:PageNamespaceCache[$Language] = $id
    return $id
}

function Get-RandomValidatedPage {
    param([string]$Language)
    $pageNamespace = Get-PageNamespace -Language $Language
    $response = Invoke-Api -Language $Language -Query "action=query&generator=random&grnnamespace=$pageNamespace&grnlimit=10&prop=proofread&prpprop=quality"
    if ($null -eq $response.query.pages) {
        return @()
    }

    $validated = @()
    foreach ($property in $response.query.pages.PSObject.Properties) {
        $page = $property.Value
        if ($null -ne $page.proofread -and $page.proofread.quality -eq 4) {
            $validated += $page.title
        }
    }

    return $validated
}

function Get-PageWikitext {
    param([string]$Language, [string]$Title)
    $encoded = [uri]::EscapeDataString($Title)
    $response = Invoke-Api -Language $Language -Query "action=query&prop=revisions&rvslots=main&rvprop=content&titles=$encoded"
    foreach ($property in $response.query.pages.PSObject.Properties) {
        $page = $property.Value
        if ($null -ne $page.revisions) {
            return [string]$page.revisions[0].slots.main.'*'
        }
    }

    return ""
}

function Get-IndexImage {
    param([string]$Language, [string]$IndexTitle)
    $encoded = [uri]::EscapeDataString($IndexTitle)
    $response = Invoke-Api -Language $Language -Query "action=parse&prop=wikitext&page=$encoded"
    $wikitext = [string]$response.parse.wikitext.'*'
    $match = [regex]::Match($wikitext, '(?m)^\s*\|\s*image\s*=\s*(.+?)\s*$')
    if ($match.Success) {
        return $match.Groups[1].Value.Trim()
    }

    return ($IndexTitle -replace '^Index:', '')
}

function ConvertFrom-Wikitext {
    param([string]$Text)
    $value = $Text
    $value = [regex]::Replace($value, '(?is)<ref[^>]*/>', ' ')
    $value = [regex]::Replace($value, '(?is)<ref[^>]*>.*?</ref>', ' ')
    $value = [regex]::Replace($value, '(?is)<[^>]+>', ' ')
    for ($pass = 0; $pass -lt 6; $pass++) {
        $value = [regex]::Replace($value, '(?s)\{\{[^{}]*\}\}', ' ')
    }

    $value = [regex]::Replace($value, '(?s)\[\[(?:File|Image):[^\]]*\]\]', ' ')
    $value = [regex]::Replace($value, '\[\[[^\]|]*\|([^\]]*)\]\]', '$1')
    $value = [regex]::Replace($value, '\[\[([^\]]*)\]\]', '$1')
    # Tables: keep the cell text. Deleting whole table rows made the legislative scans a ground
    # truth of leftovers, which measured Spanish CER above 100 percent on 4 November.
    $value = [regex]::Replace($value, '(?m)^\s*\{\|.*$', ' ')
    $value = [regex]::Replace($value, '(?m)^\s*\|\}.*$', ' ')
    $value = [regex]::Replace($value, '(?m)^\s*\|-.*$', ' ')
    $value = [regex]::Replace($value, '(?m)^\s*[|!]+', ' ')
    $value = [regex]::Replace($value, '\|\||!!', ' ')
    $value = [regex]::Replace($value, '[|!]', ' ')
    $value = [regex]::Replace($value, "'''''|'''|''", '')
    $value = [regex]::Replace($value, '(?m)^\s*-{3,}.*$', ' ')
    $value = [regex]::Replace($value, '(?m)^\s*[=]+(.*?)[=]+\s*$', '$1')
    $value = [regex]::Replace($value, '&nbsp;', ' ')
    $value = [regex]::Replace($value, '&amp;', '&')
    $value = [regex]::Replace($value, '&quot;', '"')
    $value = [regex]::Replace($value, '&#39;', "'")
    $value = [regex]::Replace($value, '[ \t]+', ' ')
    $value = [regex]::Replace($value, '(\r?\n){2,}', "`n")
    return $value.Trim()
}

function Get-ScanUrl {
    param([string]$Language, [string]$ImageName, [int]$PageNumber, [int]$Width)
    $file = $ImageName -replace '^File:', '' -replace '^Image:', ''
    $encoded = [uri]::EscapeDataString($file)
    $response = Invoke-Api -Language $Language -Query "action=query&prop=imageinfo&iiprop=url|size&iiurlwidth=$Width&titles=File:$encoded"
    foreach ($property in $response.query.pages.PSObject.Properties) {
        $info = $property.Value.imageinfo
        if ($null -eq $info) {
            continue
        }

        $thumb = ([string]$info[0].thumburl -split '\?')[0]
        if ($thumb -match 'page1-') {
            return ($thumb -replace 'page1-', "page$PageNumber-")
        }

        return ([string]$info[0].url -split '\?')[0]
    }

    throw "no imageinfo for $file"
}

function Save-Scan {
    param([string]$Url, [string]$Destination)
    Invoke-WebRequest -Uri $Url -OutFile $Destination -TimeoutSec 120 -Headers @{ "User-Agent" = $UserAgent } | Out-Null
    $bytes = [System.IO.File]::ReadAllBytes($Destination)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return (($sha.ComputeHash($bytes) | ForEach-Object { $_.ToString("x2") }) -join '')
    }
    finally {
        $sha.Dispose()
    }
}

function Add-Page {
    param(
        [string]$Language,
        [string]$Title,
        [string]$SourceUrl,
        [string]$Text,
        [string]$Slug,
        [int]$PageNumber,
        [string]$Destination,
        [System.Collections.Generic.List[object]]$Rows
    )

    $sha = Save-Scan -Url $SourceUrl -Destination $Destination
    $relative = $Destination.Substring($Root.Length).TrimStart('\', '/') -replace '\\', '/'
    $gtPath = [System.IO.Path]::ChangeExtension($Destination, ".gt.txt")
    [System.IO.File]::WriteAllText($gtPath, $Text)
    $mixed = ($Text -match '[\u0600-\u06FF]') -and ($Text -match '[A-Za-z]')
    $Rows.Add([pscustomobject]@{
            path      = $relative
            language  = $Language
            isPhone   = "false"
            mixed     = $mixed.ToString().ToLowerInvariant()
            sourceUrl = $SourceUrl
            sha256    = $sha
        })
}

function Get-Pages {
    param(
        [string]$Language,
        [int]$Count,
        [System.Collections.Generic.List[object]]$Rows
    )

    $languageRoot = Join-Path $Root $Language
    New-Item -ItemType Directory -Force -Path $languageRoot | Out-Null
    $fetched = 0
    $attempts = 0
    while ($fetched -lt $Count -and $attempts -lt ($Count * 30)) {
        $attempts++
        $titles = Get-RandomValidatedPage -Language $Language
        foreach ($title in $titles) {
            if ($fetched -ge $Count) {
                break
            }

            $match = [regex]::Match($title, '^[^:]+:(.+)/(\d+)$')
            if (-not $match.Success) {
                continue
            }

            $book = $match.Groups[1].Value
            $pageNumber = [int]$match.Groups[2].Value
            $slug = ($book -replace '\.(djvu|pdf|jpg|jpeg|png)$', '') -replace '[^A-Za-z0-9]+', '-'
            $slug = $slug.Trim('-').ToLowerInvariant()
            if ($slug.Length -eq 0) {
                $sha1 = [System.Security.Cryptography.SHA1]::Create()
                try {
                    $hash = $sha1.ComputeHash([Text.Encoding]::UTF8.GetBytes($book))
                }
                finally {
                    $sha1.Dispose()
                }

                $slug = "scan-" + (($hash[0..2] | ForEach-Object { $_.ToString("x2") }) -join '')
            }

            if ($slug.Length -gt 60) {
                $slug = $slug.Substring(0, 60)
            }

            $destination = Join-Path $languageRoot ("{0}-p{1}.png" -f $slug, $pageNumber)
            if (Test-Path $destination) {
                continue
            }

            $wikitext = Get-PageWikitext -Language $Language -Title $title
            $text = ConvertFrom-Wikitext -Text $wikitext
            if ($text.Length -lt 400) {
                continue
            }

            $image = Get-IndexImage -Language $Language -IndexTitle ("Index:$book")
            $url = Get-ScanUrl -Language $Language -ImageName $image -PageNumber $pageNumber -Width $ScanWidth
            try {
                Add-Page -Language $Language -Title $title -SourceUrl $url -Text $text -Slug $slug -PageNumber $pageNumber -Destination $destination -Rows $Rows
            }
            catch {
                Write-Warning "skip $title : $($_.Exception.Message)"
                continue
            }

            $fetched++
            Write-Host "$Language $fetched/$Count $destination"
        }
    }

    return $fetched
}

$rows = [System.Collections.Generic.List[object]]::new()
$counts = @{ ar = $ArabicCount; en = $EnglishCount }
$european = @{ fr = [int][Math]::Ceiling($EuropeanCount * 0.35); de = [int][Math]::Ceiling($EuropeanCount * 0.35) }
$european["es"] = [Math]::Max(0, $EuropeanCount - $european["fr"] - $european["de"])
foreach ($key in $european.Keys) {
    $counts[$key] = $european[$key]
}

$missing = @()
foreach ($language in @("ar", "en", "fr", "de", "es")) {
    $want = $counts[$language]
    if ($want -le 0) { continue }
    Write-Output "fetch $language $want"
    $got = Get-Pages -Language $language -Count $want -Rows $rows
    if ($got -lt $want) {
        $missing += "$language $got/$want"
    }
}

$phoneRoot = Join-Path $Root "phone"
New-Item -ItemType Directory -Force -Path $phoneRoot | Out-Null
$phoneCaptures = @(
    @{ Path = "images/sign_ar_shop_alexandria.jpg"; Gt = "ground_truth/images/sign_ar_shop_alexandria.gt.txt"; Language = "ar" },
    @{ Path = "images/receipt_en_supermarket.png"; Gt = "ground_truth/images/receipt_en_supermarket.gt.txt"; Language = "en" },
    @{ Path = "images/plaque_fr_rue_de_richelieu.jpg"; Gt = "ground_truth/images/plaque_fr_rue_de_richelieu.gt.txt"; Language = "fr" },
    @{ Path = "pdf_scanned/en_phone_blood_test_report.pdf"; Gt = "ground_truth/pdf_scanned/en_phone_blood_test_report.gt.txt"; Language = "en" }
)

foreach ($capture in $phoneCaptures) {
    $source = Join-Path $DemoRoot $capture.Path
    $gtSource = Join-Path $DemoRoot $capture.Gt
    if (-not (Test-Path $source) -or -not (Test-Path $gtSource)) {
        $missing += "phone $($capture.Path)"
        continue
    }

    $name = [System.IO.Path]::GetFileName($source)
    $destination = Join-Path $phoneRoot $name
    Copy-Item -Path $source -Destination $destination -Force
    Copy-Item -Path $gtSource -Destination ([System.IO.Path]::ChangeExtension($destination, ".gt.txt")) -Force
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $sha256 = (($sha.ComputeHash([System.IO.File]::ReadAllBytes($destination)) | ForEach-Object { $_.ToString("x2") }) -join '')
    }
    finally {
        $sha.Dispose()
    }

    $text = [System.IO.File]::ReadAllText($gtSource)
    $rows.Add([pscustomobject]@{
            path      = "phone/$name"
            language  = $capture.Language
            isPhone   = "true"
            mixed     = ((($text -match '[\u0600-\u06FF]') -and ($text -match '[A-Za-z]')).ToString().ToLowerInvariant())
            sourceUrl = "local:$($capture.Path)"
            sha256    = $sha256
        })
}

$manifest = Join-Path $Root "manifest.csv"
$rows | Export-Csv -Path $manifest -NoTypeInformation -Encoding utf8

if ($missing.Count -gt 0) {
    $missingPath = Join-Path $Root "missing.txt"
    $missing | Set-Content -Path $missingPath
    Write-Error "OCR golden corpus incomplete: $($missing -join ', '). See $missingPath"
    exit 1
}

$scans = @($rows | Where-Object { $_.isPhone -eq "false" }).Count
Write-Output "corpus ready: scans=$scans phone=$($rows.Count - $scans) manifest=$manifest"
exit 0







