[CmdletBinding()]
param(
    [string]$OrganizationName,
    [string]$ClientName,
    [string]$ClientProjectName,
    [string]$OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Read-TemplateValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Prompt,
        [string]$DefaultValue = ""
    )

    while ($true) {
        $displayPrompt = if ([string]::IsNullOrWhiteSpace($DefaultValue)) {
            $Prompt
        }
        else {
            "$Prompt [$DefaultValue]"
        }

        $value = Read-Host $displayPrompt
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value.Trim()
        }

        if (-not [string]::IsNullOrWhiteSpace($DefaultValue)) {
            return $DefaultValue.Trim()
        }
    }
}

function Convert-ToOrganizationAbbreviation {
    param([Parameter(Mandatory = $true)][string]$Value)

    $normalized = ($Value -replace '[^A-Za-z0-9]', '').Trim()
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        throw "Organization name must contain letters or digits."
    }

    return $normalized.ToUpperInvariant()
}

function Convert-ToNameSegment {
    param([Parameter(Mandatory = $true)][string]$Value)

    $tokens = @([regex]::Matches($Value.Trim(), '[A-Za-z0-9]+') | ForEach-Object { $_.Value })
    if ($tokens.Count -eq 0) {
        throw "Name values must contain letters or digits."
    }

    return ($tokens | ForEach-Object {
        if ($_.Length -eq 1) {
            $_.ToUpperInvariant()
        }
        else {
            $_.Substring(0, 1).ToUpperInvariant() + $_.Substring(1)
        }
    }) -join ''
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "The .NET SDK is required and 'dotnet' was not found on PATH."
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$templateRoot = Split-Path -Parent $scriptRoot

$rawOrganizationName = if ([string]::IsNullOrWhiteSpace($OrganizationName)) {
    Read-TemplateValue -Prompt "Organization abbreviation" -DefaultValue "CN"
}
else {
    $OrganizationName
}

$rawClientName = if ([string]::IsNullOrWhiteSpace($ClientName)) {
    Read-TemplateValue -Prompt "Client name"
}
else {
    $ClientName
}

$rawClientProjectName = if ([string]::IsNullOrWhiteSpace($ClientProjectName)) {
    Read-TemplateValue -Prompt "Client project name"
}
else {
    $ClientProjectName
}

$organizationSegment = Convert-ToOrganizationAbbreviation -Value $rawOrganizationName
$clientSegment = Convert-ToNameSegment -Value $rawClientName
$projectSegment = Convert-ToNameSegment -Value $rawClientProjectName
$rootName = "$organizationSegment.$clientSegment.$projectSegment"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $resolvedOutputDirectory = Join-Path (Get-Location) $rootName
}
else {
    $candidateOutputDirectory = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
        $OutputDirectory
    }
    else {
        Join-Path (Get-Location) $OutputDirectory
    }

    $resolvedOutputDirectory = [System.IO.Path]::GetFullPath($candidateOutputDirectory)
}

if (Test-Path $resolvedOutputDirectory) {
    throw "Output directory already exists: $resolvedOutputDirectory"
}

Write-Host ""
Write-Host "Template root name: $rootName"
Write-Host "Output directory:   $resolvedOutputDirectory"
Write-Host ""

$installArgs = @(
    "new",
    "install",
    $templateRoot
)

Write-Host "Installing template from $templateRoot"
& dotnet @installArgs
if ($LASTEXITCODE -ne 0) {
    throw "Template installation failed."
}

$createArgs = @(
    "new",
    "backend-template",
    "--organizationName", $organizationSegment,
    "--clientName", $clientSegment,
    "--clientProjectName", $projectSegment,
    "--output", $resolvedOutputDirectory
)

Write-Host ""
Write-Host "Creating project..."
& dotnet @createArgs
if ($LASTEXITCODE -ne 0) {
    throw "Project creation failed."
}

Write-Host ""
Write-Host "Created $rootName at $resolvedOutputDirectory"
