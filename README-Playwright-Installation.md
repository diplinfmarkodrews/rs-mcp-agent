# Playwright Installation Scripts

This repository contains automated installation scripts for Playwright with Node.js and npm support on both Linux and Windows systems.

## 📋 Overview

Playwright is a framework for Web Testing and Automation. It allows testing Chromium, Firefox, and WebKit with a single API. These scripts automate the entire installation process including:

- **Node.js and npm** installation  
- **Node.js Playwright** framework installation
- **.NET Playwright CLI** installation (if .NET is available)
- **Browser binaries** (Chromium, Firefox, WebKit) for both Node.js and .NET
- **System dependencies** and compatibility fixes
- **Test scripts** for verification

## 🐧 Linux Installation

### Prerequisites
- Any modern Linux distribution (Ubuntu, Debian, Kali, CentOS, Fedora, Arch, openSUSE)
- Internet connection
- sudo privileges

### Quick Start

1. **Download and run the script:**
   ```bash
   curl -fsSL https://raw.githubusercontent.com/your-repo/install-playwright-linux.sh | bash
   ```

2. **Or download and run locally:**
   ```bash
   wget https://raw.githubusercontent.com/your-repo/install-playwright-linux.sh
   chmod +x install-playwright-linux.sh
   ./install-playwright-linux.sh
   ```

3. **Or if you have the script locally:**
   ```bash
   chmod +x install-playwright-linux.sh
   ./install-playwright-linux.sh
   ```

### What the Linux script does

1. **Detects your Linux distribution** automatically
2. **Updates package manager** (apt, yum, dnf, pacman, zypper)
3. **Installs Node.js and npm** via NodeSource repository or distribution packages
4. **Installs system dependencies** required for browsers
5. **Installs Node.js Playwright globally** via npm
6. **Installs .NET Playwright CLI** (if .NET is available)
7. **Downloads browser binaries** for both Node.js and .NET (Chromium, Firefox, WebKit)
8. **Fixes library compatibility issues** (especially for Kali Linux)
9. **Creates test scripts** to verify both installations
10. **Runs the test** (optional)

### Supported Linux Distributions:
- ✅ Ubuntu/Debian (including Kali Linux)
- ✅ CentOS/RHEL
- ✅ Fedora
- ✅ Arch Linux
- ✅ openSUSE
- ⚠️ Other distributions (basic support)

## 🪟 Windows Installation

### Prerequisites
- Windows 10 or later
- Internet connection
- Administrator privileges (recommended)

### Option 1: PowerShell Script (Recommended)

1. **Open PowerShell as Administrator**
2. **Run the installation script:**
   ```powershell
   PowerShell -ExecutionPolicy Bypass -File install-playwright-windows.ps1
   ```

3. **Or with parameters:**
   ```powershell
   # Install locally instead of globally
   .\install-playwright-windows.ps1 -GlobalInstall:$false
   
   # Skip the test at the end
   .\install-playwright-windows.ps1 -SkipTest
   ```

### Option 2: Batch Script

1. **Open Command Prompt as Administrator**
2. **Run the batch script:**
   ```cmd
   install-playwright-windows.bat
   ```

### What the Windows scripts do

1. **Check for administrator privileges**
2. **Install Chocolatey** package manager (optional)
3. **Install Node.js and npm** via Chocolatey or manual download
4. **Install Node.js Playwright** globally or locally
5. **Install .NET Playwright CLI** (if .NET is available)
6. **Download browser binaries** for both Node.js and .NET (Chromium, Firefox, WebKit)
7. **Create test and helper scripts** for both platforms
8. **Run the test** (optional)

### Features of PowerShell Script:
- ✅ Robust error handling
- ✅ Colored output
- ✅ Parameter support
- ✅ Chocolatey integration
- ✅ Automatic environment refresh

## 🧪 Testing Installation

After installation, both scripts create a test file `playwright-test.js` that you can run:

```bash
# Linux/macOS
node playwright-test.js

# Windows
node playwright-test.js
```

### Test Script Features:
- Tests all three browsers (Chromium, Firefox, WebKit)
- Navigates to playwright.dev
- Verifies page title
- Reports success/failure for each browser

## 🔧 Manual Installation

If the automated scripts don't work for your system, you can install manually:

### 1. Install Node.js
- Visit [nodejs.org](https://nodejs.org/)
- Download and install the LTS version
- Verify: `node --version` and `npm --version`

### 2. Install Node.js Playwright
```bash
# Global installation
npm install -g playwright

# Or local installation
npm install playwright
```

### 3. Install .NET Playwright (Optional)
```bash
# Install .NET Playwright CLI tool
dotnet tool install --global Microsoft.Playwright.CLI
```

### 4. Install Browsers
```bash
# For Node.js Playwright
playwright install

# For .NET projects, from the project directory:
pwsh bin/Debug/netX/playwright.ps1 install
```

### 5. Install System Dependencies (Linux only)
```bash
# Ubuntu/Debian
sudo apt-get install -y libasound2 libgbm1 libnss3 libxss1 libgtk-3-0 libdrm2

# CentOS/RHEL/Fedora
sudo yum install -y alsa-lib gtk3 libXScrnSaver nss

# Arch Linux
sudo pacman -S alsa-lib gtk3 libxss nss
```

### 6. Fix Library Compatibility (Kali Linux)
If you encounter missing library errors on Kali Linux, create symbolic links:
```bash
# ICU libraries
sudo ln -sf /usr/lib/x86_64-linux-gnu/libicudata.so.72 /usr/lib/x86_64-linux-gnu/libicudata.so.66
sudo ln -sf /usr/lib/x86_64-linux-gnu/libicui18n.so.72 /usr/lib/x86_64-linux-gnu/libicui18n.so.66
sudo ln -sf /usr/lib/x86_64-linux-gnu/libicuuc.so.72 /usr/lib/x86_64-linux-gnu/libicuuc.so.66

# Other libraries
sudo ln -sf /usr/lib/x86_64-linux-gnu/libjpeg.so.62 /usr/lib/x86_64-linux-gnu/libjpeg.so.8
sudo ln -sf /usr/lib/x86_64-linux-gnu/libwebp.so.7 /usr/lib/x86_64-linux-gnu/libwebp.so.6
sudo ln -sf /usr/lib/x86_64-linux-gnu/libffi.so.8 /usr/lib/x86_64-linux-gnu/libffi.so.7
```

## 📚 Usage Examples

### Basic Playwright Script

```javascript
const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.goto('https://playwright.dev/');
  await page.screenshot({ path: 'example.png' });
  await browser.close();
})();
```

### Testing Multiple Browsers

```javascript
const { chromium, firefox, webkit } = require('playwright');

async function runInAllBrowsers() {
  for (const browserType of [chromium, firefox, webkit]) {
    const browser = await browserType.launch();
    const page = await browser.newPage();
    await page.goto('https://playwright.dev/');
    console.log(`${browserType.name()}: ${await page.title()}`);
    await browser.close();
  }
}

runInAllBrowsers();
```

## 🛠️ Troubleshooting

### Common Issues

1. **Permission Denied (Linux)**
   ```bash
   chmod +x install-playwright-linux.sh
   sudo ./install-playwright-linux.sh
   ```

2. **Execution Policy Error (Windows)**
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

3. **Node.js Not Found After Installation**
   - Restart your terminal/command prompt
   - Check if Node.js is in PATH: `echo $PATH` (Linux) or `echo $env:PATH` (PowerShell)

4. **Browser Dependencies Missing (Linux)**
   ```bash
   sudo playwright install-deps
   ```

5. **Playwright Command Not Found**
   - Use `npx playwright` instead of `playwright`
   - Or install globally: `npm install -g playwright`

6. **.NET Playwright Browser Missing Error**
   ```bash
   # For .NET projects, install browsers from the project directory:
   cd YourProject
   dotnet build
   pwsh bin/Debug/net9.0/playwright.ps1 install
   ```

7. **Library Compatibility Issues (Kali Linux)**
   - Run the installation script which automatically fixes these issues
   - Or manually create symbolic links as shown in the manual installation section

### Getting Help

1. **Check Playwright documentation**: <https://playwright.dev/docs/intro>
2. **View installation logs** for error details
3. **Run with verbose output**:
   ```bash
   # Linux
   bash -x install-playwright-linux.sh
   
   # Windows PowerShell
   .\install-playwright-windows.ps1 -Verbose
   ```

## 📄 File Structure

After running the installation scripts, you'll have:

```text
.
├── install-playwright-linux.sh         # Linux installation script
├── install-playwright-windows.bat      # Windows batch installation script
├── install-playwright-windows.ps1      # Windows PowerShell installation script
├── playwright-test.js                  # Node.js test script (created after installation)
├── dotnet-playwright-test.cs           # .NET test script (created if .NET is available)
├── DotNetPlaywrightTest.cs             # Windows .NET test script (created after installation)
├── playwright-helper.ps1               # Windows helper script (created after installation)
└── README-Playwright-Installation.md   # This file
```

## 🚀 Quick Commands Reference

### Linux/macOS
```bash
# Check installation
playwright --version
node --version
npm --version

# Run test
node playwright-test.js

# Generate code
playwright codegen https://example.com

# Run Playwright tests
npx playwright test
```

### Windows
```cmd
# Check installation
playwright --version
node --version
npm --version

# Run test
node playwright-test.js

# Use helper script
.\playwright-helper.ps1

# Generate code
playwright codegen https://example.com
```

## 📝 License

These scripts are provided as-is for educational and development purposes. Playwright itself is licensed under the Apache License 2.0.

## 🤝 Contributing

Feel free to submit issues, fork the repository, and create pull requests for any improvements.

---

**Happy Testing with Playwright! 🎭**
