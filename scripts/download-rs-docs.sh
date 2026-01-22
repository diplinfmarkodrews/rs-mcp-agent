#!/bin/bash

# ReportServer Documentation Downloader
# Downloads RS documentation files to the Data folder

# Note: Not using 'set -e' here because we want to continue downloading
# other files even if one fails

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
DATA_DIR="$PROJECT_ROOT/src/RSChatApp.RSChatApp/RSChatApp.Web/wwwroot/Data"

# Print functions
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

# Predefined URLs for RS documentation
# Add or modify URLs as needed
declare -a DOCS_URLS=(
    "https://reportserver.net/files/rs-documentation/config_RS5.0.pdf"
    "https://reportserver.net/files/rs-documentation/user_en_RS5.0.pdf"
    "https://reportserver.net/files/rs-documentation/admin_RS5.0.pdf"
    "https://reportserver.net/files/rs-documentation/script_RS5.0.pdf"
)

# Alternative: Add documentation URLs here
# Format: "URL|custom_filename" or just "URL" to keep original filename
# declare -a DOCS_URLS=(
#     "https://example.com/doc1.pdf|rs-doc1.pdf"
#     "https://example.com/doc2.html"
# )

echo "🚀 ReportServer Documentation Downloader"
echo "=========================================="
echo

# Create Data directory if it doesn't exist
print_step "Checking Data directory..."
if [ ! -d "$DATA_DIR" ]; then
    print_status "Creating Data directory at: $DATA_DIR"
    mkdir -p "$DATA_DIR"
else
    print_status "Data directory exists: $DATA_DIR"
fi

# Check if we have any URLs configured
if [ ${#DOCS_URLS[@]} -eq 0 ]; then
    print_error "No documentation URLs configured!"
    print_warning "Please edit the script and add URLs to the DOCS_URLS array."
    exit 1
fi

# Download each file
print_step "Starting downloads..."
echo

DOWNLOADED=0
FAILED=0

for url_entry in "${DOCS_URLS[@]}"; do
    # Parse URL and optional custom filename
    if [[ "$url_entry" == *"|"* ]]; then
        URL="${url_entry%|*}"
        CUSTOM_NAME="${url_entry#*|}"
        OUTPUT_FILE="$DATA_DIR/$CUSTOM_NAME"
    else
        URL="$url_entry"
        FILENAME=$(basename "$URL")
        OUTPUT_FILE="$DATA_DIR/$FILENAME"
    fi
    
    print_status "Downloading: $URL"
    
    # Try to download with wget, fallback to curl if not available
    if command -v wget &> /dev/null; then
        if wget -q --show-progress -O "$OUTPUT_FILE" "$URL"; then
            print_status "✓ Downloaded: $(basename "$OUTPUT_FILE")"
            ((DOWNLOADED++))
        else
            print_error "✗ Failed to download: $URL"
            ((FAILED++))
            # Remove partial file if it exists
            [ -f "$OUTPUT_FILE" ] && rm "$OUTPUT_FILE"
        fi
    elif command -v curl &> /dev/null; then
        if curl -L -# -o "$OUTPUT_FILE" "$URL"; then
            print_status "✓ Downloaded: $(basename "$OUTPUT_FILE")"
            ((DOWNLOADED++))
        else
            print_error "✗ Failed to download: $URL"
            ((FAILED++))
            # Remove partial file if it exists
            [ -f "$OUTPUT_FILE" ] && rm "$OUTPUT_FILE"
        fi
    else
        print_error "Neither wget nor curl is available. Please install one of them."
        exit 1
    fi
    
    echo
done

# Summary
echo "=========================================="
print_step "Download Summary"
echo "Total URLs: ${#DOCS_URLS[@]}"
echo -e "${GREEN}Successfully downloaded: $DOWNLOADED${NC}"
if [ $FAILED -gt 0 ]; then
    echo -e "${RED}Failed: $FAILED${NC}"
fi
echo "Files saved to: $DATA_DIR"
echo

# List downloaded files
if [ $DOWNLOADED -gt 0 ]; then
    print_status "Downloaded files:"
    ls -lh "$DATA_DIR" 2>/dev/null | grep -v "^total" | awk '{print "  - " $9 " (" $5 ")"}'
fi

echo
if [ $FAILED -eq 0 ]; then
    print_status "✨ All documentation files downloaded successfully!"
else
    print_warning "⚠️  Some downloads failed. Please check the URLs and try again."
fi
