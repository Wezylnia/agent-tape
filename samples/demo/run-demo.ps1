#!/usr/bin/env pwsh
# AgentTape Demo Script – Windows (PowerShell)
# Creates a temporary project, initializes git, records a session, and generates reports.

$ErrorActionPreference = "Stop"
$demoDir = Join-Path $env:TEMP "agenttape-demo-$(Get-Random)"

Write-Host "=== AgentTape Demo ===" -ForegroundColor Cyan
Write-Host ""

# Create demo directory
New-Item -ItemType Directory -Path $demoDir -Force | Out-Null
Push-Location $demoDir

try {
    # Initialize git
    git init
    git config user.email "demo@agenttape.dev"
    git config user.name "AgentTape Demo"
    echo "# Demo Project" > README.md
    git add README.md
    git commit -m "initial commit"

    Write-Host "1. Running: agenttape init" -ForegroundColor Yellow
    dotnet run --project "$PSScriptRoot\..\src\AgentTape.Cli" -- init

    Write-Host ""
    Write-Host "2. Running: agenttape record --name demo -- dotnet --version" -ForegroundColor Yellow
    dotnet run --project "$PSScriptRoot\..\src\AgentTape.Cli" -- record --name demo -- dotnet --version

    Write-Host ""
    Write-Host "3. Running: agenttape list" -ForegroundColor Yellow
    dotnet run --project "$PSScriptRoot\..\src\AgentTape.Cli" -- list

    Write-Host ""
    Write-Host "4. Modifying a file and recording again..." -ForegroundColor Yellow
    echo "// Demo change" >> Program.cs
    dotnet run --project "$PSScriptRoot\..\src\AgentTape.Cli" -- record --name file-change -- dotnet build

    Write-Host ""
    Write-Host "5. Running: agenttape list" -ForegroundColor Yellow
    dotnet run --project "$PSScriptRoot\..\src\AgentTape.Cli" -- list

    Write-Host ""
    Write-Host "6. Generating PR summary..." -ForegroundColor Yellow
    dotnet run --project "$PSScriptRoot\..\src\AgentTape.Cli" -- export --github-pr --output pr-summary.md

    Write-Host ""
    Write-Host "=== Demo Complete ===" -ForegroundColor Green
    Write-Host "Reports: $(Get-Item .agenttape\reports\latest.html | Select-Object -ExpandProperty FullName)"
    Write-Host "PR Summary: $(Get-Item pr-summary.md | Select-Object -ExpandProperty FullName)"
}
finally {
    Pop-Location
}
