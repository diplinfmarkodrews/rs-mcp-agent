// Text viewer functionality
class TextViewer {
    constructor() {
        this.textContent = document.getElementById('textContent');
        this.searchInfo = document.getElementById('searchInfo');
        this.originalText = '';
        this.searchQuery = '';
        this.isPhrase = false;
        
        this.init();
    }
    
    async init() {
        try {
            // Extract parameters from URL
            const url = new URL(window.location);
            const fileUrl = url.searchParams.get('file');
            this.searchQuery = url.searchParams.get('search') || '';
            this.isPhrase = url.searchParams.get('phrase') === 'true';
            
            if (!fileUrl) {
                throw new Error('File not specified in the URL query string');
            }
            
            await this.loadTextFile(fileUrl);
            
            if (this.searchQuery) {
                this.highlightSearchTerms();
            }
            
        } catch (error) {
            this.showError(error.message);
        }
    }
    
    async loadTextFile(fileUrl) {
        try {
            const response = await fetch(fileUrl);
            
            if (!response.ok) {
                throw new Error(`Failed to load file: ${response.status} ${response.statusText}`);
            }
            
            this.originalText = await response.text();
            this.textContent.textContent = this.originalText;
            this.textContent.className = '';
            
        } catch (error) {
            throw new Error(`Error loading text file: ${error.message}`);
        }
    }
    
    highlightSearchTerms() {
        if (!this.searchQuery || !this.originalText) {
            return;
        }
        
        let highlightedText = this.originalText;
        let matchCount = 0;
        
        if (this.isPhrase) {
            // Search for exact phrase
            const regex = new RegExp(this.escapeRegex(this.searchQuery), 'gi');
            const matches = this.originalText.match(regex);
            matchCount = matches ? matches.length : 0;
            
            highlightedText = this.originalText.replace(regex, (match) => {
                return `<span class="highlight">${this.escapeHtml(match)}</span>`;
            });
        } else {
            // Search for individual words
            const words = this.searchQuery.split(/\s+/).filter(word => word.length > 0);
            
            words.forEach(word => {
                const regex = new RegExp(this.escapeRegex(word), 'gi');
                const matches = highlightedText.match(regex);
                if (matches) {
                    matchCount += matches.length;
                }
                
                highlightedText = highlightedText.replace(regex, (match) => {
                    return `<span class="highlight">${this.escapeHtml(match)}</span>`;
                });
            });
        }
        
        this.textContent.innerHTML = highlightedText;
        
        // Show search info
        if (matchCount > 0) {
            this.searchInfo.textContent = `Found ${matchCount} match${matchCount === 1 ? '' : 'es'} for "${this.searchQuery}"`;
            this.searchInfo.style.display = 'block';
            
            // Scroll to first match
            setTimeout(() => {
                const firstHighlight = document.querySelector('.highlight');
                if (firstHighlight) {
                    firstHighlight.scrollIntoView({ 
                        behavior: 'smooth', 
                        block: 'center' 
                    });
                }
            }, 100);
        } else {
            this.searchInfo.textContent = `No matches found for "${this.searchQuery}"`;
            this.searchInfo.style.display = 'block';
            this.searchInfo.style.backgroundColor = '#ff9800';
        }
    }
    
    escapeRegex(string) {
        return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }
    
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
    
    showError(message) {
        this.textContent.className = 'error';
        this.textContent.textContent = `Error: ${message}`;
    }
}

// Initialize the text viewer when the page loads
document.addEventListener('DOMContentLoaded', () => {
    new TextViewer();
});
