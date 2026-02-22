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
echo [STEP] Installing Playwright browsers (Chrome, Firefox, WebKit, MsEdge)...
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

:installation_complete
echo ✨ Installation completed successfully!
echo.
echo Next steps:
echo 1. Verify Node.js installation: playwright --version ^(or npx playwright --version^)

where dotnet >nul 2>&1
if %errorLevel% == 0 (
    echo 1. Verify .NET installation: dotnet tool list -g ^| findstr playwright  
    echo 2. For .NET projects, install browsers with: pwsh bin/Debug/netX/playwright.ps1 install    
    echo 3. Start building automation scripts!
) 
echo.
echo Documentation: https://playwright.dev/docs/intro
echo Examples: https://github.com/microsoft/playwright

echo.
echo 🎉 Thank you for installing Playwright!
echo.
pause
