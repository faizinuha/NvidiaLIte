# PowerShell Script to build and compile NVIDIA LITE Installer

Write-Host "--- 0. Stopping any running application instances ---" -ForegroundColor Cyan
Stop-Process -Name "NvidiaCi" -Force -ErrorAction SilentlyContinue


$ProjectRoot = Resolve-Path "$PSScriptRoot\.."
$BuildDir = "$ProjectRoot\bin\Release\net9.0-windows\win-x64\publish"
$InnoSetupExe = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
$IssScript = "$PSScriptRoot\compile.iss"

# Signing Configuration
$SignTool = "$PSScriptRoot\bin\osslsigncode.exe"
$CertFile = "$ProjectRoot\ZeroMixCert.pfx"
$CertPassword = "ZeroMixPass"
$TimestampServer = "http://timestamp.digicert.com"

function Sign-File($FilePath) {
    if (Test-Path $CertFile) {
        Write-Host "--- Signing: $FilePath ---" -ForegroundColor Yellow
        & $SignTool sign -pkcs12 "$CertFile" -pass "$CertPassword" -ts "$TimestampServer" -in "$FilePath" -out "$FilePath.signed"
        if ($LASTEXITCODE -eq 0) {
            Move-Item -Path "$FilePath.signed" -Destination "$FilePath" -Force
            Write-Host "Successfully signed $FilePath" -ForegroundColor Green
        } else {
            Write-Warning "Failed to sign $FilePath. Proceeding without signature."
        }
    } else {
        Write-Warning "Certificate $CertFile not found. Skipping signing."
    }
}

Write-Host "--- 1. Cleaning old builds ---" -ForegroundColor Cyan
if (Test-Path $BuildDir) { Remove-Item -Recurse -Force $BuildDir }

Write-Host "--- 2. Publishing .NET Project (Self-Contained) ---" -ForegroundColor Cyan
Set-Location $ProjectRoot
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishReadyToRun=true

if ($LASTEXITCODE -ne 0) {
    Write-Error "Dotnet publish failed!"
    exit $LASTEXITCODE
}

# Sign the main executable
Sign-File "$BuildDir\NvidiaCi.exe"

Write-Host "--- 3. Compiling Installer with Inno Setup ---" -ForegroundColor Cyan
if (Test-Path $InnoSetupExe) {
    & $InnoSetupExe "$IssScript"
    
    # Sign the resulting installer
    $InstallerPath = "$PSScriptRoot\Output\NvidiaLite_Setup_v1.4.exe"
    if (Test-Path $InstallerPath) {
        Sign-File $InstallerPath
    }
} else {
    Write-Error "Inno Setup Compiler (ISCC.exe) not found at $InnoSetupExe"
    Write-Host "Please install Inno Setup 6 or adjust the path in this script." -ForegroundColor Yellow
}

Write-Host "--- DONE! Check build\Output folder ---" -ForegroundColor Green
