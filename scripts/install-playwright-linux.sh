#!/bin/bash

# Playwright Installation Script for Linux
# This script installs Node.js, npm, and Playwright with browser support

set -e  # Exit on any error

echo "🚀 Starting Playwright installation for Linux..."
echo "================================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_step() {
    echo -e "${BLUE}[STEP]${NC} $1"
}

# Check if running as root
if [[ $EUID -eq 0 ]]; then
    print_warning "Running as root. This is not recommended for npm installations."
    read -p "Do you want to continue? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Detect Linux distribution
detect_distro() {
    if [ -f /etc/os-release ]; then
        . /etc/os-release
        OS=$NAME
        VER=$VERSION_ID
    elif type lsb_release >/dev/null 2>&1; then
        OS=$(lsb_release -si)
        VER=$(lsb_release -sr)
    elif [ -f /etc/lsb-release ]; then
        . /etc/lsb-release
        OS=$DISTRIB_ID
        VER=$DISTRIB_RELEASE
    elif [ -f /etc/debian_version ]; then
        OS=Debian
        VER=$(cat /etc/debian_version)
    else
        OS=$(uname -s)
        VER=$(uname -r)
    fi
    
    print_status "Detected OS: $OS $VER"
}

# Update package manager
update_packages() {
    print_step "Updating package manager..."
    
    if command -v apt-get &> /dev/null; then
        sudo apt-get update -y
    elif command -v yum &> /dev/null; then
        sudo yum update -y
    elif command -v dnf &> /dev/null; then
        sudo dnf update -y
    elif command -v pacman &> /dev/null; then
        sudo pacman -Sy
    elif command -v zypper &> /dev/null; then
        sudo zypper refresh
    else
        print_warning "Could not detect package manager. Please update manually."
    fi
}

# Install Node.js and npm
install_nodejs() {
    print_step "Installing Node.js and npm..."
    
    # Check if Node.js is already installed
    if command -v node &> /dev/null; then
        NODE_VERSION=$(node --version)
        print_status "Node.js is already installed: $NODE_VERSION"
        
        # Check if version is recent enough (v16+)
        NODE_MAJOR=$(echo $NODE_VERSION | cut -d'.' -f1 | sed 's/v//')
        if [ "$NODE_MAJOR" -lt 16 ]; then
            print_warning "Node.js version is too old. Installing newer version..."
        else
            print_status "Node.js version is sufficient."
            return 0
        fi
    fi
    
    # Install Node.js based on distribution
    if command -v apt-get &> /dev/null; then
        # Debian/Ubuntu
        print_status "Installing Node.js via NodeSource repository..."
        curl -fsSL https://deb.nodesource.com/setup_lts.x | sudo -E bash -
        sudo apt-get install -y nodejs
        
    elif command -v yum &> /dev/null; then
        # RHEL/CentOS
        print_status "Installing Node.js via NodeSource repository..."
        curl -fsSL https://rpm.nodesource.com/setup_lts.x | sudo bash -
        sudo yum install -y nodejs npm
        
    elif command -v dnf &> /dev/null; then
        # Fedora
        print_status "Installing Node.js via NodeSource repository..."
        curl -fsSL https://rpm.nodesource.com/setup_lts.x | sudo bash -
        sudo dnf install -y nodejs npm
        
    elif command -v pacman &> /dev/null; then
        # Arch Linux
        sudo pacman -S nodejs npm
        
    elif command -v zypper &> /dev/null; then
        # openSUSE
        sudo zypper install nodejs npm
        
    else
        print_error "Unsupported package manager. Please install Node.js manually from https://nodejs.org/"
        exit 1
    fi
    
    # Verify installation
    if command -v node &> /dev/null && command -v npm &> /dev/null; then
        print_status "Node.js $(node --version) and npm $(npm --version) installed successfully!"
    else
        print_error "Failed to install Node.js and npm"
        exit 1
    fi
}

# Install system dependencies for Playwright
install_system_dependencies() {
    print_step "Installing system dependencies for Playwright browsers..."
    
    if command -v apt-get &> /dev/null; then
        # Debian/Ubuntu/Kali
        sudo apt-get install -y \
            libasound2t64 \
            libatk-bridge2.0-0t64 \
            libatspi2.0-0t64 \
            libcairo2 \
            libcups2t64 \
            libdbus-1-3 \
            libdrm2 \
            libfontconfig1 \
            libgbm1 \
            libglib2.0-0t64 \
            libgtk-3-0t64 \
            libnspr4 \
            libnss3 \
            libpango-1.0-0 \
            libpangocairo-1.0-0 \
            libxcomposite1 \
            libxdamage1 \
            libxext6 \
            libxfixes3 \
            libxkbcommon0 \
            libxrandr2 \
            libxss1 \
            libxtst6 \
            fonts-unifont \
            fonts-liberation \
            libavcodec61 \
            libavformat61 \
            libavutil59 \
            libjpeg62-turbo \
            libpng16-16t64 \
            libwebp7 \
            libvpx9 \
            libenchant-2-2 \
            ca-certificates \
            fonts-liberation \
            libappindicator3-1 \
            libasound2 \
            libdrm2 \
            libxss1 \
            lsb-release \
            xdg-utils \
            wget
            
    elif command -v yum &> /dev/null; then
        # RHEL/CentOS
        sudo yum install -y \
            alsa-lib \
            atk \
            cups-libs \
            gtk3 \
            libdrm \
            libX11 \
            libXcomposite \
            libXdamage \
            libXext \
            libXfixes \
            libXrandr \
            libXss \
            libXtst \
            pango \
            cairo \
            gdk-pixbuf2 \
            nss \
            nspr
            
    elif command -v dnf &> /dev/null; then
        # Fedora
        sudo dnf install -y \
            alsa-lib \
            atk \
            cups-libs \
            gtk3 \
            libdrm \
            libX11 \
            libXcomposite \
            libXdamage \
            libXext \
            libXfixes \
            libXrandr \
            libXScrnSaver \
            libXtst \
            pango \
            cairo \
            gdk-pixbuf2 \
            nss \
            nspr
            
    elif command -v pacman &> /dev/null; then
        # Arch Linux
        sudo pacman -S --needed \
            alsa-lib \
            gtk3 \
            libxss \
            nss \
            ttf-liberation
            
    elif command -v zypper &> /dev/null; then
        # openSUSE
        sudo zypper install -y \
            alsa \
            gtk3 \
            libXScrnSaver1 \
            mozilla-nss \
            liberation-fonts
    else
        print_warning "Could not install system dependencies automatically. You may need to install them manually."
    fi
}

# Install Playwright globally
install_playwright_global() {
    print_step "Installing Playwright globally..."
    
    # Install Node.js Playwright
    sudo npm install -g playwright
    
    if command -v playwright &> /dev/null; then
        print_status "Node.js Playwright $(playwright --version) installed successfully!"
    else
        print_error "Failed to install Playwright globally"
        exit 1
    fi
}

# Install .NET Playwright CLI
install_playwright_dotnet() {
    print_step "Installing .NET Playwright CLI..."
    
    # Check if dotnet is available
    if command -v dotnet &> /dev/null; then
        print_status "Found .NET CLI: $(dotnet --version)"
        
        # Install .NET Playwright CLI tool
        dotnet tool install --global Microsoft.Playwright.CLI 2>/dev/null || {
            print_warning ".NET Playwright CLI may already be installed"
        }
        
        print_status ".NET Playwright CLI installed successfully!"
    else
        print_warning ".NET CLI not found. Skipping .NET Playwright installation."
        print_info "If you need .NET Playwright, install .NET first: https://dotnet.microsoft.com/download"
    fi
}

# Install Playwright browsers
install_playwright_browsers() {
    print_step "Installing Playwright browsers (Chromium, Firefox, WebKit)..."
    
    # Fix library compatibility first
    fix_library_compatibility
    
    # Install browsers globally for Node.js
    print_status "Installing browsers for Node.js Playwright..."
    sudo playwright install
    
    # Install browsers for .NET if dotnet is available
    if command -v dotnet &> /dev/null; then
        print_status "Installing browsers for .NET Playwright..."
        playwright install 2>/dev/null || print_warning "Some .NET browsers might not have been installed"
    fi
    
    # Try to install system dependencies
    print_status "Attempting to install system dependencies..."
    sudo playwright install-deps || print_warning "Some system dependencies might not have been installed. The browsers should still work."
    
    print_status "Playwright browsers installed successfully!"
}

# Create a test script
create_test_script() {
    print_step "Creating test script..."
    
    cat > playwright-test.js << 'EOF'
const { chromium, firefox, webkit } = require('playwright');

async function testPlaywright() {
    console.log('🧪 Testing Playwright installation...\n');
    
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
    
    console.log('\n🎉 Playwright installation test completed!');
}

testPlaywright();
EOF
    
    # Create .NET test script if dotnet is available
    if command -v dotnet &> /dev/null; then
        cat > dotnet-playwright-test.cs << 'EOF'
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
                Console.WriteLine($"🔍 Testing {browser.Name}...");
                await using var browserInstance = await browser.Type.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
                var page = await browserInstance.NewPageAsync();
                await page.GotoAsync("https://playwright.dev");
                var title = await page.TitleAsync();
                Console.WriteLine($"✅ {browser.Name} test passed! Page title: {title}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {browser.Name} test failed: {ex.Message}");
            }
        }
        
        Console.WriteLine("\n🎉 .NET Playwright installation test completed!");
    }
}
EOF
        print_status ".NET test script created as 'dotnet-playwright-test.cs'"
    fi
    
    print_status "Test script created as 'playwright-test.js'"
}

# Run test
run_test() {
    print_step "Running Playwright test..."
    
    if [ -f "playwright-test.js" ]; then
        node playwright-test.js
    else
        print_warning "Test script not found. Skipping test."
    fi
}

# Main installation process
main() {
    print_status "Starting Playwright installation process..."
    
    detect_distro
    update_packages
    install_nodejs
    install_system_dependencies
    install_playwright_global
    install_playwright_dotnet
    install_playwright_browsers
    create_test_script
    
    echo
    print_status "✨ Installation completed successfully!"
    echo
    echo -e "${GREEN}Next steps:${NC}"
    echo "1. Run 'playwright --version' to verify Node.js installation"
    if command -v dotnet &> /dev/null; then
        echo "2. Run 'dotnet tool list -g | grep playwright' to verify .NET installation"
        echo "3. Test Node.js: 'node playwright-test.js'"
        echo "4. Test .NET: Create a project and test with the generated script"
        echo "5. For .NET projects, install browsers with: 'pwsh bin/Debug/netX/playwright.ps1 install'"
    else
        echo "2. Run 'node playwright-test.js' to test the installation"
        echo "3. Install .NET if you need .NET Playwright support"
    fi
    echo "6. Start building awesome automation scripts!"
    echo
    echo -e "${BLUE}Documentation:${NC} https://playwright.dev/docs/intro"
    echo -e "${BLUE}Examples:${NC} https://github.com/microsoft/playwright"
    
    # Ask if user wants to run test now
    echo
    read -p "Would you like to run the test now? (Y/n): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]] || [[ -z $REPLY ]]; then
        run_test
    fi
}

# Run main function
main "$@"
