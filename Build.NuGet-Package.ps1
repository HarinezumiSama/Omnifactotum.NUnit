#Requires -Version 5.0

using namespace System;
using namespace System.IO;
using namespace System.Security;

[CmdletBinding(PositionalBinding = $false, SupportsTransactions = $false)]
param (
    [Parameter(Mandatory = $false)]
    [string] $NUnitBinPath
)

$script:ErrorActionPreference = [Management.Automation.ActionPreference]::Stop

function StopWithError([ValidateNotNullOrEmpty()] [string] $message)
{
    Write-Host "* ERROR: $message" -ForegroundColor Red
    #Pause
    exit 1
}

function CleanUpPath([string] $path)
{
    Write-Host "* Cleaning up ""$path""..." -ForegroundColor Cyan
    
    if (Test-Path -Path $path)
    {
        Remove-Item -Path $path -Recurse -Force | Out-Null
    }

    Write-Host "* Cleaning up ""$path"" - DONE." -ForegroundColor Cyan
}

function Run-Tool()
{
    [CmdletBinding(PositionalBinding = $true, SupportsTransactions = $false)]
    param (
        [Parameter(Position = 0)]
        [string] $Title,

        [Parameter(Position = 1)]
        [string] $ToolPath,

        [Parameter(Position = 2, ValueFromRemainingArguments = $true)]
        [string[]] $ToolArgs
    )

    Write-Host
    Write-Host "* ${Title}..." -ForegroundColor Cyan
    try
    {
        & "$ToolPath" @ToolArgs
    }
    catch
    {
        $ex = $_.Exception
        StopWithError "${Title} - FAILED.`r`nError message: $($ex.Message)"
    }

    [int] $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0)
    {
        StopWithError "${Title} - FAILED (exit code: ${exitCode})."
    }

    Write-Host "* ${Title} - DONE." -ForegroundColor Cyan
}

cls # For debugging

[ValidateNotNullOrEmpty()] [string] $scriptDir = $PSScriptRoot
[string] $solutionPath = [Path]::Combine($scriptDir, "src\Omnifactotum.NUnit.sln")
[string] $projectPath = [Path]::Combine($scriptDir, "src\Omnifactotum.NUnit\Omnifactotum.NUnit.csproj")
[string] $releaseNotesPath = [Path]::Combine($scriptDir, "src\Omnifactotum.NUnit.ReleaseNotes.txt")
[string] $nuGetProjectPath = $projectPath
[string] $baseOutputDir = [Path]::Combine($scriptDir, "bin")
[string] $outputDir = [Path]::Combine($baseOutputDir, "AnyCpu\Release")
[string] $nuGetOutputDir = [Path]::Combine($baseOutputDir, "NuGet")

[string] $appDir = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86, [Environment+SpecialFolderOption]::DoNotVerify)
[string] $actualNUnitBinPath = if ([string]::IsNullOrEmpty($NUnitBinPath)) { [Path]::Combine($appDir, "NUnit\bin") } else { $NUnitBinPath }
[string] $nunitConsolePath = [Path]::Combine($actualNUnitBinPath, "nunit-console.exe")
[string] $msBuildPath = [Path]::Combine($appDir, "MSBuild\14.0\Bin\MSBuild.exe")

if (!(Test-Path -Path $nunitConsolePath -PathType Leaf))
{
    StopWithError "NUnit Console executable is not found at ""$nunitConsolePath""."
}

[string] $releaseNotes = [string]::Empty
if (Test-Path -Path $releaseNotesPath -PathType Leaf)
{
    $releaseNotes = Get-Content -Path $releaseNotesPath -Raw
}

[string] $escapedReleaseNotes = [SecurityElement]::Escape($releaseNotes)

CleanUpPath $baseOutputDir

Run-Tool "Building ""$solutionPath""" $msBuildPath $solutionPath /target:Rebuild /p:Configuration="Release" /p:Platform="Any CPU" /DetailedSummary

[string] $testDllDir = [Path]::Combine($outputDir, "Tests")
[string] $testDllPath = [Path]::Combine($testDllDir, "Omnifactotum.NUnit.Tests.dll")
Run-Tool "Running automated tests" $nunitConsolePath $testDllPath /basepath="$testDllDir" /work="$testDllDir" /labels /stoponerror /noshadow

if ($env:SIGN_OMNIFACTOTUM_NUNIT -ine "1")
{
    StopWithError "The assembly signing is not enabled. The NuGet package will not be created."
}

CleanUpPath $nuGetOutputDir
New-Item -Path $nuGetOutputDir -ItemType Directory -Force

Run-Tool "Creating NuGet package" nuget.exe pack "$nuGetProjectPath" -Verbosity detailed -OutputDirectory "$nuGetOutputDir" -Symbols -MSBuildVersion 14 -NonInteractive -Properties "Configuration=""Release"";Platform=""AnyCPU"";PkgReleaseNotes=""$escapedReleaseNotes"""

Write-Host "*** DONE ***" -ForegroundColor Green
exit 0
