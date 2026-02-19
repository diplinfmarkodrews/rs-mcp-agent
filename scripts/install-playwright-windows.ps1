# Playwright Installation Script for Windows (PowerShell)
# This script installs Node.js, npm, and Playwright with browser support
# Run with: PowerShell -ExecutionPolicy Bypass -File install-playwright-windows.ps1

param(
    [switch]$SkipTest,
    [switch]$GlobalInstall = $true,
    [string]$NodeVersion = "lts"
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Colors for output
$Colors = @{
    Info = "Green"
    Warning = "Yellow" 
    Error = "Red"
    Step = "Cyan"
    Success = "Green"
}

function Write-ColorText {
    param(
        [string]$Text,
        [string]$Color = "White"
    )
    Write-Host $Text -ForegroundColor $Color
}

function Write-Step {
    param([string]$Message)
    Write-ColorText "[STEP] $Message" $Colors.Step
}

function Write-Info {
    param([string]$Message)
    Write-ColorText "[INFO] $Message" $Colors.Info
}

function Write-Warning {
    param([string]$Message)
    Write-ColorText "[WARNING] $Message" $Colors.Warning
}

function Write-ErrorMessage {
    param([string]$Message)
    Write-ColorText "[ERROR] $Message" $Colors.Error
}

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-CommandExists {
    param([string]$Command)
    $null = Get-Command $Command -ErrorAction SilentlyContinue
    return $?
}

function Install-Chocolatey {
    Write-Step "Installing Chocolatey package manager..."
    
    if (Test-CommandExists "choco") {
        Write-Info "Chocolatey is already installed."
        return $true
    }
    
    try {
        Set-ExecutionPolicy Bypass -Scope Process -Force
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
        iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
        
        # Refresh environment variables
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
        
        if (Test-CommandExists "choco") {
            Write-Info "Chocolatey installed successfully!"
            return $true
        } else {
            Write-Warning "Chocolatey installation verification failed."
            return $false
        }
    } catch {
        Write-ErrorMessage "Failed to install Chocolatey: $($_.Exception.Message)"
        return $false
    }
}

function Install-NodeJS {
    Write-Step "Installing Node.js and npm..."
    
    # Check if Node.js is already installed
    if (Test-CommandExists "node") {
        $nodeVersion = node --version
        Write-Info "Node.js is already installed: $nodeVersion"
        
        # Check if version is recent enough (v16+)
        $majorVersion = [int]($nodeVersion -replace "v(\d+)\..*", '$1')
        if ($majorVersion -ge 16) {
            Write-Info "Node.js version is sufficient."
            return $true
        } else {
            Write-Warning "Node.js version is too old. Installing newer version..."
        }
    }
    
    # Try to install via Chocolatey first
    if (Test-CommandExists "choco") {
        try {
            Write-Info "Installing Node.js via Chocolatey..."
            choco install nodejs -y
            
            # Refresh environment variables
            $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
            
            if (Test-CommandExists "node") {
                $nodeVersion = node --version
                $npmVersion = npm --version
                Write-Info "Node.js $nodeVersion and npm $npmVersion installed successfully!"
                return $true
            }
        } catch {
            Write-Warning "Failed to install Node.js via Chocolatey: $($_.Exception.Message)"
        }
    }
    
    # Fallback to manual installation
    Write-Info "Please install Node.js manually:"
    Write-Info "1. Go to https://nodejs.org/"
    Write-Info "2. Download the LTS version for Windows"
    Write-Info "3. Run the installer and follow the instructions"
    Write-Info "4. Make sure to check 'Add to PATH' during installation"
    Write-Info "5. Restart PowerShell after installation"
    
    $choice = Read-Host "Press Enter after installing Node.js manually, or 'q' to quit"
    if ($choice -eq 'q') {
        exit 1
    }
    
    # Check again after manual installation
    if (Test-CommandExists "node") {
        $nodeVersion = node --version
        $npmVersion = npm --version
        Write-Info "Node.js $nodeVersion and npm $npmVersion found!"
        return $true
    } else {
        Write-ErrorMessage "Node.js still not found. Please restart PowerShell and run this script again."
        exit 1
    }
}

function Install-Playwright {
    Write-Step "Installing Playwright..."
    
    try {
        # Install Node.js Playwright
        if ($GlobalInstall) {
            Write-Info "Installing Node.js Playwright globally..."
            npm install -g playwright
        } else {
            Write-Info "Installing Node.js Playwright locally..."
            npm install playwright
        }
        
        # Verify Node.js Playwright installation
        $playwrightAvailable = $false
        if (Test-CommandExists "playwright") {
            $playwrightVersion = playwright --version
            Write-Info "Node.js Playwright $playwrightVersion installed successfully!"
            $playwrightAvailable = $true
        } elseif (Test-CommandExists "npx") {
            try {
                $playwrightVersion = npx playwright --version
                Write-Info "Node.js Playwright installed and available via npx!"
                $playwrightAvailable = $true
            } catch {
                # npx might fail if playwright isn't installed
            }
        }
        
        if (-not $playwrightAvailable) {
            throw "Node.js Playwright installation verification failed"
        }
        
        return $true
    } catch {
        Write-ErrorMessage "Failed to install Node.js Playwright: $($_.Exception.Message)"
        return $false
    }
}

function Install-PlaywrightDotNet {
    Write-Step "Installing .NET Playwright CLI..."
    
    try {
        # Check if .NET is available
        if (Test-CommandExists "dotnet") {
            $dotnetVersion = dotnet --version
            Write-Info "Found .NET CLI: $dotnetVersion"
            
            # Install .NET Playwright CLI tool
            $result = dotnet tool install --global Microsoft.Playwright.CLI 2>$null
            if ($LASTEXITCODE -ne 0) {
                Write-Warning ".NET Playwright CLI may already be installed"
            } else {
                Write-Info ".NET Playwright CLI installed successfully!"
            }
            
            return $true
        } else {
            Write-Warning ".NET CLI not found. Skipping .NET Playwright installation."
            Write-Info "If you need .NET Playwright, install .NET first: https://dotnet.microsoft.com/download"
            return $false
        }
    } catch {
        Write-Warning "Failed to install .NET Playwright CLI: $($_.Exception.Message)"
        return $false
    }
}

function Install-PlaywrightBrowsers {
    Write-Step "Installing Playwright browsers (Chromium, Firefox, WebKit)..."
    
    try {
        # Install browsers for Node.js Playwright
        Write-Info "Installing browsers for Node.js Playwright..."
        if (Test-CommandExists "playwright") {
            playwright install
        } else {
            npx playwright install
        }
        
        # Install browsers for .NET Playwright if available
        if (Test-CommandExists "dotnet") {
            Write-Info "Installing browsers for .NET Playwright..."
            try {
                playwright install
            } catch {
                Write-Warning "Some .NET browsers might not have been installed: $($_.Exception.Message)"
            }
        }
        
        Write-Info "Playwright browsers installed successfully!"
        return $true
    } catch {
        Write-ErrorMessage "Failed to install Playwright browsers: $($_.Exception.Message)"
        return $false
    }
}

function Create-TestScript {
    Write-Step "Creating test script..."
    
    $testScript = @'
const { chromium, firefox, webkit } = require('playwright');

async function testPlaywright() {
    console.log('🧪 Testing Node.js Playwright installation...\n');
    
    const browsers = [
        { name: 'Chromium', engine: chromium },
        { name: 'Firefox', engine: firefox },
        { name: 'WebKit', engine: webkit }
    ];
    
    for (const browser of browsers) {
        try {
            console.log(`🔍 Testing ${browser.name}...`);
            const browserInstance = await browser.engine.launch({ headless: true });
            const page = await browserInstance.newPage();
            await page.goto('https://playwright.dev');
            const title = await page.title();
            console.log(`✅ ${browser.name} test passed! Page title: ${title}`);
            await browserInstance.close();
        } catch (error) {
            console.log(`❌ ${browser.name} test failed:`, error.message);
        }
    }
    
    console.log('\n🎉 Node.js Playwright installation test completed!');
}

testPlaywright();
'@

    $testScript | Out-File -FilePath "playwright-test.js" -Encoding UTF8
    Write-Info "Node.js test script created as 'playwright-test.js'"
    
    # Create .NET test script if .NET is available
    if (Test-CommandExists "dotnet") {
        $dotnetTestScript = @"
using Microsoft.Playwright;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🧪 Testing .NET Playwright installation...\n");
        
        using var playwright = await Playwright.CreateAsync();
        
        var browsers = new[]
        {
            new { Name = "Chromium", Type = playwright.Chromium },
            new { Name = "Firefox", Type = playwright.Firefox },
            new { Name = "WebKit", Type = playwright.Webkit }
        };
        
        foreach (var browser in browsers)
        {
            try
            {
                Console.WriteLine(`$"🔍 Testing {browser.Name}..."`);
                await using var browserInstance = await browser.Type.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
                var page = await browserInstance.NewPageAsync();
                await page.GotoAsync("https://playwright.dev");
                var title = await page.TitleAsync();
                Console.WriteLine(`$"✅ {browser.Name} test passed! Page title: {title}"`);
            }
            catch (Exception ex)
            {
                Console.WriteLine(`$"❌ {browser.Name} test failed: {ex.Message}"`);
            }
        }
        
        Console.WriteLine("\n🎉 .NET Playwright installation test completed!");
    }
}
"@
        $dotnetTestScript | Out-File -FilePath "DotNetPlaywrightTest.cs" -Encoding UTF8
        Write-Info ".NET test script created as 'DotNetPlaywrightTest.cs'"
    }
}

function Create-HelperScript {
    Write-Step "Creating PowerShell helper script..."
    
    $helperScript = @"
# Playwright Helper Script for PowerShell
# Run with: .\playwright-helper.ps1

Write-Host "🎭 Playwright Helper Script" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host ""

# Check if Playwright is available
if (Get-Command "playwright" -ErrorAction SilentlyContinue) {
    Write-Host "✅ Playwright is available globally" -ForegroundColor Green
    playwright --version
} elseif (Get-Command "npx" -ErrorAction SilentlyContinue) {
    Write-Host "✅ Playwright is available via npx" -ForegroundColor Green
    try {
        npx playwright --version
    } catch {
        Write-Host "❌ Playwright not properly installed" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "❌ Playwright not found" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Available commands:" -ForegroundColor Yellow
Write-Host "1. Test installation: node playwright-test.js" -ForegroundColor White
Write-Host "2. Install browsers: playwright install (or npx playwright install)" -ForegroundColor White
Write-Host "3. Run codegen: playwright codegen (or npx playwright codegen)" -ForegroundColor White
Write-Host "4. Show help: playwright --help (or npx playwright --help)" -ForegroundColor White
Write-Host ""

`$choice = Read-Host "Would you like to run the test now? (Y/n)"
if (`$choice -eq "" -or `$choice -eq "Y" -or `$choice -eq "y") {
    Write-Host "Running Playwright test..." -ForegroundColor Cyan
    node playwright-test.js
}
"@

    $helperScript | Out-File -FilePath "playwright-helper.ps1" -Encoding UTF8
    Write-Info "PowerShell helper script created as 'playwright-helper.ps1'"
}

function Test-Installation {
    if ($SkipTest) {
        Write-Info "Skipping test as requested."
        return
    }
    
    Write-Step "Running Playwright test..."
    
    try {
        if (Test-Path "playwright-test.js") {
            node playwright-test.js
            Write-Info "Test completed successfully!"
        } else {
            Write-Warning "Test script not found. Skipping test."
        }
    } catch {
        Write-Warning "Test completed with some issues: $($_.Exception.Message)"
    }
}

function Main {
    Write-ColorText "🚀 Starting Playwright installation for Windows..." $Colors.Step
    Write-ColorText "================================================" $Colors.Step
    Write-Host ""
    
    # Check if running as administrator
    if (Test-Administrator) {
        Write-Info "Running with administrator privileges."
    } else {
        Write-Warning "Not running as administrator. Some operations might require elevation."
        Write-Warning "Consider running PowerShell as administrator for best results."
        
        $continue = Read-Host "Do you want to continue? (Y/n)"
        if ($continue -eq "n" -or $continue -eq "N") {
            exit 1
        }
    }
    
    Write-Host ""
    
    try {
        # Install Chocolatey if not available and user wants it
        if (-not (Test-CommandExists "choco")) {
            $installChoco = Read-Host "Chocolatey package manager not found. Install it for easier Node.js installation? (Y/n)"
            if ($installChoco -ne "n" -and $installChoco -ne "N") {
                Install-Chocolatey
            }
        }
        
        # Install Node.js
        if (-not (Install-NodeJS)) {
            throw "Failed to install Node.js"
        }
        
        # Install Node.js Playwright
        if (-not (Install-Playwright)) {
            throw "Failed to install Node.js Playwright"
        }
        
        # Install .NET Playwright CLI
        Install-PlaywrightDotNet | Out-Null
        
        # Install browsers
        if (-not (Install-PlaywrightBrowsers)) {
            throw "Failed to install Playwright browsers"
        }
        
        # Create helper scripts
        Create-TestScript
        Create-HelperScript
        
        # Show success message
        Write-Host ""
        Write-ColorText "✨ Installation completed successfully!" $Colors.Success
        Write-Host ""
        Write-ColorText "Next steps:" $Colors.Info
        Write-Host "1. Verify Node.js installation: playwright --version (or npx playwright --version)"
        if (Test-CommandExists "dotnet") {
            Write-Host "2. Verify .NET installation: dotnet tool list -g | findstr playwright"
            Write-Host "3. Test Node.js: node playwright-test.js"
            Write-Host "4. Test .NET: Create a project and test with the generated script"
            Write-Host "5. For .NET projects, install browsers with: pwsh bin/Debug/netX/playwright.ps1 install"
            Write-Host "6. Use PowerShell helper: .\playwright-helper.ps1"
            Write-Host "7. Start building automation scripts!"
        } else {
            Write-Host "2. Test installation: node playwright-test.js"
            Write-Host "3. Use PowerShell helper: .\playwright-helper.ps1"
            Write-Host "4. Install .NET if you need .NET Playwright support"
            Write-Host "5. Start building automation scripts!"
        }
        Write-Host ""
        Write-ColorText "Documentation:" $Colors.Info
        Write-Host "• Playwright Docs: https://playwright.dev/docs/intro"
        Write-Host "• Examples: https://github.com/microsoft/playwright"
        Write-Host ""
        
        # Run test
        if (-not $SkipTest) {
            $runTest = Read-Host "Would you like to run the test now? (Y/n)"
            if ($runTest -eq "" -or $runTest -eq "Y" -or $runTest -eq "y") {
                Test-Installation
            }
        }
        
    } catch {
        Write-ErrorMessage "Installation failed: $($_.Exception.Message)"
        exit 1
    }
    
    Write-Host ""
    Write-ColorText "🎉 Thank you for installing Playwright!" $Colors.Success
}

# Run main function
Main
