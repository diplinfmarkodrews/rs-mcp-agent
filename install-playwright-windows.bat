@echo off
REM Playwright Installation Script for Windows
REM This script installs Node.js, npm, and Playwright with browser support

setlocal enabledelayedexpansion

echo.
echo 🚀 Starting Playwright installation for Windows...
echo ================================================
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% == 0 (
    echo [INFO] Running with administrator privileges.
) else (
    echo [WARNING] Not running as administrator. Some operations might fail.
    echo [WARNING] Consider running this script as administrator for best results.
    echo.
    pause
)

REM Function to check if a command exists
where node >nul 2>&1
if %errorLevel% == 0 (
    echo [INFO] Node.js is already installed.
    node --version
    npm --version
    echo.
    goto :install_playwright
) else (
    echo [INFO] Node.js not found. Installing Node.js...
    goto :install_nodejs
)

:install_nodejs
echo [STEP] Installing Node.js and npm...
echo.

REM Check if Chocolatey is installed
where choco >nul 2>&1
if %errorLevel% == 0 (
    echo [INFO] Chocolatey found. Installing Node.js via Chocolatey...
    choco install nodejs -y
    if %errorLevel% == 0 (
        echo [INFO] Node.js installed successfully via Chocolatey!
        goto :verify_nodejs
    ) else (
        echo [ERROR] Failed to install Node.js via Chocolatey.
        goto :manual_nodejs_install
    )
) else (
    echo [INFO] Chocolatey not found. Proceeding with manual installation...
    goto :manual_nodejs_install
)

:manual_nodejs_install
echo [INFO] Please install Node.js manually:
echo 1. Go to https://nodejs.org/
echo 2. Download the LTS version for Windows
echo 3. Run the installer and follow the instructions
echo 4. Make sure to check "Add to PATH" during installation
echo 5. Restart your command prompt after installation
echo.
echo After installing Node.js, run this script again.
echo.
pause
exit /b 1

:verify_nodejs
REM Refresh environment variables
call refreshenv.cmd 2>nul || echo [WARNING] Could not refresh environment variables. Please restart your command prompt if Node.js commands are not found.

where node >nul 2>&1
if %errorLevel% == 0 (
    echo [INFO] Node.js verification successful!
    node --version
    npm --version
    echo.
) else (
    echo [ERROR] Node.js installation verification failed.
    echo Please restart your command prompt and run this script again.
    pause
    exit /b 1
)

:install_playwright
echo [STEP] Installing Playwright...
echo.

REM Install Node.js Playwright globally
npm install -g playwright
if %errorLevel% == 0 (
    echo [INFO] Node.js Playwright installed successfully!
) else (
    echo [ERROR] Failed to install Node.js Playwright globally.
    echo [INFO] Trying to install without global flag...
    npm install playwright
    if %errorLevel% == 0 (
        echo [INFO] Node.js Playwright installed locally.
    ) else (
        echo [ERROR] Failed to install Node.js Playwright.
        pause
        exit /b 1
    )
)

REM Install .NET Playwright CLI if .NET is available
where dotnet >nul 2>&1
if %errorLevel% == 0 (
    echo [INFO] Installing .NET Playwright CLI...
    dotnet tool install --global Microsoft.Playwright.CLI >nul 2>&1
    if %errorLevel% == 0 (
        echo [INFO] .NET Playwright CLI installed successfully!
    ) else (
        echo [WARNING] .NET Playwright CLI may already be installed.
    )
) else (
    echo [WARNING] .NET CLI not found. Skipping .NET Playwright installation.
    echo [INFO] Install .NET if you need .NET Playwright support.
)

REM Check Playwright installation
where playwright >nul 2>&1
if %errorLevel% == 0 (
    echo [INFO] Playwright command available globally.
    playwright --version
) else (
    echo [INFO] Playwright installed locally. Use 'npx playwright' to run commands.
)

echo.

:install_browsers
echo [STEP] Installing Playwright browsers (Chromium, Firefox, WebKit)...
echo.

REM Install browsers for Node.js Playwright
echo [INFO] Installing browsers for Node.js Playwright...
where playwright >nul 2>&1
if %errorLevel% == 0 (
    playwright install
) else (
    npx playwright install
)

if %errorLevel% == 0 (
    echo [INFO] Node.js Playwright browsers installed successfully!
) else (
    echo [ERROR] Failed to install Node.js Playwright browsers.
    pause
    exit /b 1
)

REM Install browsers for .NET Playwright if available
where dotnet >nul 2>&1
if %errorLevel% == 0 (
    echo [INFO] Installing browsers for .NET Playwright...
    playwright install >nul 2>&1
    if %errorLevel% == 0 (
        echo [INFO] .NET Playwright browsers installed successfully!
    ) else (
        echo [WARNING] Some .NET browsers might not have been installed.
    )
)

echo.

:create_test_script
echo [STEP] Creating test script...
echo.

REM Create test script
(
echo const { chromium, firefox, webkit } = require^('playwright'^);
echo.
echo async function testPlaywright^(^) {
echo     console.log^('🧪 Testing Playwright installation...\n'^);
echo     
echo     const browsers = [
echo         { name: 'Chromium', engine: chromium },
echo         { name: 'Firefox', engine: firefox },
echo         { name: 'WebKit', engine: webkit }
echo     ];
echo     
echo     for ^(const browser of browsers^) {
echo         try {
echo             console.log^(`🔍 Testing ${browser.name}...`^);
echo             const browserInstance = await browser.engine.launch^({ headless: true }^);
echo             const page = await browserInstance.newPage^(^);
echo             await page.goto^('https://playwright.dev'^);
echo             const title = await page.title^(^);
echo             console.log^(`✅ ${browser.name} test passed! Page title: ${title}`^);
echo             await browserInstance.close^(^);
echo         } catch ^(error^) {
echo             console.log^(`❌ ${browser.name} test failed:`, error.message^);
echo         }
echo     }
echo     
echo     console.log^('\n🎉 Playwright installation test completed!'^);
echo }
echo.
echo testPlaywright^(^);
) > playwright-test.js

echo [INFO] Test script created as 'playwright-test.js'
echo.

:create_powershell_script
echo [STEP] Creating PowerShell helper script...
echo.

REM Create PowerShell script for easier management
(
echo # Playwright Helper Script for PowerShell
echo # Run with: .\playwright-helper.ps1
echo.
echo Write-Host "🎭 Playwright Helper Script" -ForegroundColor Cyan
echo Write-Host "=========================" -ForegroundColor Cyan
echo Write-Host ""
echo.
echo # Check if Playwright is available
echo if ^(Get-Command "playwright" -ErrorAction SilentlyContinue^) {
echo     Write-Host "✅ Playwright is available globally" -ForegroundColor Green
echo     playwright --version
echo } elseif ^(Get-Command "npx" -ErrorAction SilentlyContinue^) {
echo     Write-Host "✅ Playwright is available via npx" -ForegroundColor Green
echo     npx playwright --version
echo } else {
echo     Write-Host "❌ Playwright not found" -ForegroundColor Red
echo     exit 1
echo }
echo.
echo Write-Host ""
echo Write-Host "Available commands:" -ForegroundColor Yellow
echo Write-Host "1. Test installation: node playwright-test.js" -ForegroundColor White
echo Write-Host "2. Install browsers: playwright install ^(or npx playwright install^)" -ForegroundColor White
echo Write-Host "3. Run codegen: playwright codegen ^(or npx playwright codegen^)" -ForegroundColor White
echo Write-Host "4. Show help: playwright --help ^(or npx playwright --help^)" -ForegroundColor White
echo Write-Host ""
echo.
echo $choice = Read-Host "Would you like to run the test now? ^(Y/n^)"
echo if ^($choice -eq "" -or $choice -eq "Y" -or $choice -eq "y"^) {
echo     Write-Host "Running Playwright test..." -ForegroundColor Cyan
echo     node playwright-test.js
echo }
) > playwright-helper.ps1

echo [INFO] PowerShell helper script created as 'playwright-helper.ps1'
echo.

:installation_complete
echo ✨ Installation completed successfully!
echo.
echo Next steps:
echo 1. Verify Node.js installation: playwright --version ^(or npx playwright --version^)

where dotnet >nul 2>&1
if %errorLevel% == 0 (
    echo 2. Verify .NET installation: dotnet tool list -g ^| findstr playwright
    echo 3. Test Node.js: node playwright-test.js
    echo 4. Test .NET: Create a project and test with the generated script
    echo 5. For .NET projects, install browsers with: pwsh bin/Debug/netX/playwright.ps1 install
    echo 6. Use PowerShell helper: .\playwright-helper.ps1
    echo 7. Start building automation scripts!
) else (
    echo 2. Test installation: node playwright-test.js
    echo 3. Use PowerShell helper: .\playwright-helper.ps1
    echo 4. Install .NET if you need .NET Playwright support
    echo 5. Start building automation scripts!
)
echo.
echo Documentation: https://playwright.dev/docs/intro
echo Examples: https://github.com/microsoft/playwright
echo.

:run_test
set /p choice="Would you like to run the test now? (Y/n): "
if /i "%choice%"=="n" goto :end
if /i "%choice%"=="no" goto :end

echo.
echo [STEP] Running Playwright test...
echo.

REM Run the test
node playwright-test.js
if %errorLevel% == 0 (
    echo.
    echo [INFO] Test completed successfully!
) else (
    echo.
    echo [WARNING] Test completed with some issues. Check the output above.
)

:end
echo.
echo 🎉 Thank you for installing Playwright!
echo.
pause
