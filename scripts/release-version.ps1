# =============================================================================
# NVIDIA Lite - Release Script
# Usage: .\scripts\script-release.ps1 --version 1.0.0
# =============================================================================

param(
    [Parameter(Mandatory = $true)]
    [string]$version
)

# --- Helpers -----------------------------------------------------------------

function Write-Step([string]$msg) {
    Write-Host "`n  --> $msg" -ForegroundColor Cyan
}

function Write-Success([string]$msg) {
    Write-Host "  [OK] $msg" -ForegroundColor Green
}

function Write-Fail([string]$msg) {
    Write-Host "  [FAIL] $msg" -ForegroundColor Red
    exit 1
}

function Write-Banner {
    Write-Host ""
    Write-Host "  =============================================" -ForegroundColor DarkGreen
    Write-Host "   NVIDIA Lite - Release Builder" -ForegroundColor Green
    Write-Host "   Version: v$version" -ForegroundColor White
    Write-Host "  =============================================" -ForegroundColor DarkGreen
    Write-Host ""
}

# --- Validate version format (semver: x.y.z or x.y.z-suffix) ----------------

function Assert-Version([string]$v) {
    if ($v -notmatch '^\d+\.\d+\.\d+(-[\w\.]+)?$') {
        Write-Fail "Invalid version format '$v'. Expected: 1.0.0 or 1.0.0-beta"
    }
}

# --- Check prerequisites -----------------------------------------------------

function Assert-Prerequisites {
    Write-Step "Checking prerequisites..."

    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Write-Fail ".NET SDK not found. Install from https://dotnet.microsoft.com/download"
    }
    Write-Success ".NET SDK found: $(dotnet --version)"

    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        Write-Fail "Git not found."
    }
    Write-Success "Git found: $(git --version)"

    # Must be run from repo root
    if (-not (Test-Path "NvidiaCi.csproj")) {
        Write-Fail "Run this script from the project root (where NvidiaCi.csproj lives)."
    }
    Write-Success "Project file found."
}

# --- Check git working tree is clean -----------------------------------------

function Assert-CleanGit {
    Write-Step "Checking git status..."

    $status = git status --porcelain
    if ($status) {
        Write-Host ""
        Write-Host "  Uncommitted changes detected:" -ForegroundColor Yellow
        $status | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkYellow }
        Write-Host ""
        $confirm = Read-Host "  Continue anyway? (y/N)"
        if ($confirm -ne 'y') {
            Write-Fail "Aborted. Commit or stash your changes first."
        }
    } else {
        Write-Success "Working tree is clean."
    }
}

# --- Update version in .csproj -----------------------------------------------

function Update-CsprojVersion([string]$v) {
    Write-Step "Updating version in NvidiaCi.csproj to $v..."

    $csproj = "NvidiaCi.csproj"
    $content = Get-Content $csproj -Raw

    # Inject or replace <Version> and <AssemblyVersion> tags
    if ($content -match '<Version>.*?</Version>') {
        $content = $content -replace '<Version>.*?</Version>', "<Version>$v</Version>"
    } else {
        $content = $content -replace '</PropertyGroup>', "    <Version>$v</Version>`n  </PropertyGroup>"
    }

    if ($content -match '<AssemblyVersion>.*?</AssemblyVersion>') {
        $content = $content -replace '<AssemblyVersion>.*?</AssemblyVersion>', "<AssemblyVersion>$v.0</AssemblyVersion>"
    } else {
        $content = $content -replace '</PropertyGroup>', "    <AssemblyVersion>$v.0</AssemblyVersion>`n  </PropertyGroup>"
    }

    Set-Content $csproj $content -NoNewline
    Write-Success "csproj version updated."
}

# --- Build & Publish ---------------------------------------------------------

function Invoke-Publish([string]$v) {
    Write-Step "Building and publishing (Release, win-x64, self-contained)..."

    $publishDir = ".\publish\v$v"
    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

    dotnet publish NvidiaCi.csproj `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:Version=$v `
        --output $publishDir `
        --nologo -v quiet

    if ($LASTEXITCODE -ne 0) {
        Write-Fail "dotnet publish failed."
    }
    Write-Success "Published to $publishDir"
    return $publishDir
}

# --- Zip artifact ------------------------------------------------------------

function New-ZipArtifact([string]$publishDir, [string]$v) {
    Write-Step "Creating zip artifact..."

    $distDir = ".\dist"
    if (-not (Test-Path $distDir)) { New-Item -ItemType Directory -Path $distDir | Out-Null }

    $zipPath = "$distDir\NvidiaLite-v$v-win-x64.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath
    $sizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
    Write-Success "Artifact: $zipPath ($sizeMB MB)"
    return $zipPath
}

# --- Git tag & push ----------------------------------------------------------

function Invoke-GitTag([string]$v) {
    Write-Step "Tagging release as v$v..."

    $tag = "v$v"

    # Check if tag already exists
    $existing = git tag --list $tag
    if ($existing) {
        Write-Host "  Tag $tag already exists." -ForegroundColor Yellow
        $confirm = Read-Host "  Delete and re-create? (y/N)"
        if ($confirm -eq 'y') {
            git tag -d $tag | Out-Null
            git push origin --delete $tag 2>$null
        } else {
            Write-Fail "Aborted. Tag already exists."
        }
    }

    # Stage csproj version bump
    git add NvidiaCi.csproj
    $commitMsg = "chore: bump version to v$v"
    git diff --cached --quiet
    if ($LASTEXITCODE -ne 0) {
        git commit -m $commitMsg | Out-Null
        Write-Success "Committed version bump."
    }

    git tag -a $tag -m "Release $tag"
    Write-Success "Tag $tag created."

    $pushConfirm = Read-Host "`n  Push tag to origin? (y/N)"
    if ($pushConfirm -eq 'y') {
        git push origin HEAD
        git push origin $tag
        Write-Success "Pushed to origin. GitHub Actions release workflow will start automatically."
    } else {
        Write-Host "  Skipped push. Run manually:" -ForegroundColor Yellow
        Write-Host "    git push origin HEAD && git push origin $tag" -ForegroundColor DarkYellow
    }
}

# --- Summary -----------------------------------------------------------------

function Write-Summary([string]$v, [string]$zipPath) {
    Write-Host ""
    Write-Host "  =============================================" -ForegroundColor DarkGreen
    Write-Host "   Release v$v complete!" -ForegroundColor Green
    Write-Host "  =============================================" -ForegroundColor DarkGreen
    Write-Host ""
    Write-Host "   Artifact : $zipPath" -ForegroundColor White
    Write-Host "   Tag      : v$v" -ForegroundColor White
    Write-Host ""
    Write-Host "   Next steps:" -ForegroundColor Gray
    Write-Host "   - Check GitHub Actions for the release workflow" -ForegroundColor Gray
    Write-Host "   - Update RELEASES.md with changelog for v$v" -ForegroundColor Gray
    Write-Host ""
}

# =============================================================================
# MAIN
# =============================================================================

Write-Banner
Assert-Version $version
Assert-Prerequisites
Assert-CleanGit
Update-CsprojVersion $version
$publishDir = Invoke-Publish $version
$zipPath    = New-ZipArtifact $publishDir $version
Invoke-GitTag $version
Write-Summary $version $zipPath
