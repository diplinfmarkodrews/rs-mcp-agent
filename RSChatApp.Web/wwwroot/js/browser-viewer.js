// BrowserViewer JavaScript helpers
window.browserViewerHelpers = {
    // Select all text in an input element
    selectText: function(selector) {
        try {
            const element = document.querySelector(selector);
            if (element && (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA')) {
                element.select();
                element.setSelectionRange(0, 99999); // For mobile devices
            }
        } catch (error) {
            console.warn('Error selecting text:', error);
        }
    },

    // Copy text to clipboard
    copyToClipboard: function(text) {
        try {
            if (navigator.clipboard && window.isSecureContext) {
                return navigator.clipboard.writeText(text);
            } else {
                // Fallback for older browsers
                const textArea = document.createElement('textarea');
                textArea.value = text;
                textArea.style.position = 'fixed';
                textArea.style.left = '-999999px';
                textArea.style.top = '-999999px';
                document.body.appendChild(textArea);
                textArea.focus();
                textArea.select();
                const result = document.execCommand('copy');
                textArea.remove();
                return Promise.resolve(result);
            }
        } catch (error) {
            console.error('Error copying to clipboard:', error);
            return Promise.reject(error);
        }
    },

    // Show temporary toast message
    showToast: function(message, type = 'info', duration = 3000) {
        try {
            // Remove existing toast
            const existingToast = document.querySelector('.browser-toast');
            if (existingToast) {
                existingToast.remove();
            }

            // Create toast element
            const toast = document.createElement('div');
            toast.className = `browser-toast browser-toast-${type}`;
            toast.textContent = message;
            toast.style.cssText = `
                position: fixed;
                top: 20px;
                right: 20px;
                background: ${type === 'error' ? '#dc3545' : type === 'success' ? '#28a745' : '#17a2b8'};
                color: white;
                padding: 12px 16px;
                border-radius: 4px;
                box-shadow: 0 4px 6px rgba(0,0,0,0.1);
                z-index: 10000;
                font-size: 14px;
                max-width: 300px;
                word-wrap: break-word;
                animation: slideInRight 0.3s ease;
            `;

            // Add animation styles
            if (!document.querySelector('#browser-toast-styles')) {
                const style = document.createElement('style');
                style.id = 'browser-toast-styles';
                style.textContent = `
                    @keyframes slideInRight {
                        from { transform: translateX(100%); opacity: 0; }
                        to { transform: translateX(0); opacity: 1; }
                    }
                    @keyframes slideOutRight {
                        from { transform: translateX(0); opacity: 1; }
                        to { transform: translateX(100%); opacity: 0; }
                    }
                `;
                document.head.appendChild(style);
            }

            document.body.appendChild(toast);

            // Auto remove after duration
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.style.animation = 'slideOutRight 0.3s ease';
                    setTimeout(() => toast.remove(), 300);
                }
            }, duration);

        } catch (error) {
            console.error('Error showing toast:', error);
        }
    },

    // Highlight element temporarily
    highlightElement: function(selector, duration = 2000) {
        try {
            const elements = document.querySelectorAll(selector);
            elements.forEach(element => {
                const originalStyle = element.style.cssText;
                element.style.cssText += `
                    outline: 3px solid #007bff !important;
                    outline-offset: 2px !important;
                    background-color: rgba(0,123,255,0.1) !important;
                    transition: all 0.3s ease !important;
                `;
                
                setTimeout(() => {
                    element.style.cssText = originalStyle;
                }, duration);
            });
            
            return elements.length;
        } catch (error) {
            console.error('Error highlighting element:', error);
            return 0;
        }
    },

    // Scroll element into view smoothly
    scrollToElement: function(selector) {
        try {
            const element = document.querySelector(selector);
            if (element) {
                element.scrollIntoView({ 
                    behavior: 'smooth', 
                    block: 'center',
                    inline: 'nearest'
                });
                return true;
            }
            return false;
        } catch (error) {
            console.error('Error scrolling to element:', error);
            return false;
        }
    },

    // Get element information
    getElementInfo: function(selector) {
        try {
            const elements = document.querySelectorAll(selector);
            return Array.from(elements).map(el => ({
                tagName: el.tagName.toLowerCase(),
                text: el.textContent?.trim() || '',
                value: el.value || '',
                href: el.href || '',
                src: el.src || '',
                id: el.id || '',
                className: el.className || '',
                type: el.type || '',
                disabled: el.disabled || false,
                visible: el.offsetParent !== null
            }));
        } catch (error) {
            console.error('Error getting element info:', error);
            return [];
        }
    }
};

// Make selectText available globally for backward compatibility
window.selectText = window.browserViewerHelpers.selectText;
