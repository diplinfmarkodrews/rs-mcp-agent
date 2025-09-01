// Browser Viewer JavaScript Helpers
window.browserViewerHelpers = {
    // Select all text in an input element
    selectText: function(selector) {
        try {
            const element = document.querySelector(selector);
            if (element && element.select) {
                element.select();
                element.setSelectionRange(0, 99999); // For mobile devices
                return true;
            }
            return false;
        } catch (error) {
            console.error('Error selecting text:', error);
            return false;
        }
    },

    // Focus an element
    focusElement: function(selector) {
        try {
            const element = document.querySelector(selector);
            if (element && element.focus) {
                element.focus();
                return true;
            }
            return false;
        } catch (error) {
            console.error('Error focusing element:', error);
            return false;
        }
    },

    // Copy text to clipboard
    copyToClipboard: function(text) {
        try {
            if (navigator.clipboard && window.isSecureContext) {
                return navigator.clipboard.writeText(text).then(() => true).catch(() => false);
            } else {
                // Fallback for older browsers or non-secure contexts
                const textArea = document.createElement('textarea');
                textArea.value = text;
                textArea.style.position = 'fixed';
                textArea.style.left = '-999999px';
                textArea.style.top = '-999999px';
                document.body.appendChild(textArea);
                textArea.focus();
                textArea.select();
                
                try {
                    const result = document.execCommand('copy');
                    document.body.removeChild(textArea);
                    return Promise.resolve(result);
                } catch (error) {
                    document.body.removeChild(textArea);
                    return Promise.resolve(false);
                }
            }
        } catch (error) {
            console.error('Error copying to clipboard:', error);
            return Promise.resolve(false);
        }
    },

    // Get current URL from iframe
    getIframeUrl: function(iframeSelector) {
        try {
            const iframe = document.querySelector(iframeSelector);
            if (iframe && iframe.contentWindow) {
                try {
                    return iframe.contentWindow.location.href;
                } catch (error) {
                    // Cross-origin restriction - return src attribute instead
                    return iframe.src || '';
                }
            }
            return '';
        } catch (error) {
            console.error('Error getting iframe URL:', error);
            return '';
        }
    },

    // Monitor iframe navigation (limited by CORS)
    monitorIframeNavigation: function(iframeSelector, callback) {
        try {
            const iframe = document.querySelector(iframeSelector);
            if (!iframe) return false;

            // Listen for load events
            iframe.addEventListener('load', function() {
                try {
                    const url = iframe.contentWindow.location.href;
                    callback(url);
                } catch (error) {
                    // Cross-origin restriction
                    callback(iframe.src || '');
                }
            });

            return true;
        } catch (error) {
            console.error('Error monitoring iframe navigation:', error);
            return false;
        }
    },

    // Send message to iframe (for same-origin content)
    sendMessageToIframe: function(iframeSelector, message) {
        try {
            const iframe = document.querySelector(iframeSelector);
            if (iframe && iframe.contentWindow) {
                iframe.contentWindow.postMessage(message, '*');
                return true;
            }
            return false;
        } catch (error) {
            console.error('Error sending message to iframe:', error);
            return false;
        }
    },

    // Listen for messages from iframe
    listenForIframeMessages: function(callback) {
        try {
            window.addEventListener('message', function(event) {
                // Basic security check - you may want to add origin validation
                if (event.source && event.data) {
                    callback(event.data, event.origin);
                }
            });
            return true;
        } catch (error) {
            console.error('Error listening for iframe messages:', error);
            return false;
        }
    },

    // Scroll iframe to top
    scrollIframeToTop: function(iframeSelector) {
        try {
            const iframe = document.querySelector(iframeSelector);
            if (iframe && iframe.contentWindow) {
                try {
                    iframe.contentWindow.scrollTo(0, 0);
                    return true;
                } catch (error) {
                    // Cross-origin restriction
                    return false;
                }
            }
            return false;
        } catch (error) {
            console.error('Error scrolling iframe:', error);
            return false;
        }
    },

    // Get iframe document title (for same-origin content)
    getIframeTitle: function(iframeSelector) {
        try {
            const iframe = document.querySelector(iframeSelector);
            if (iframe && iframe.contentWindow && iframe.contentDocument) {
                return iframe.contentDocument.title || '';
            }
            return '';
        } catch (error) {
            // Cross-origin restriction
            return '';
        }
    },

    // Resize iframe to content (for same-origin content)
    resizeIframeToContent: function(iframeSelector) {
        try {
            const iframe = document.querySelector(iframeSelector);
            if (iframe && iframe.contentDocument) {
                const body = iframe.contentDocument.body;
                const html = iframe.contentDocument.documentElement;
                
                const height = Math.max(
                    body.scrollHeight,
                    body.offsetHeight,
                    html.clientHeight,
                    html.scrollHeight,
                    html.offsetHeight
                );
                
                iframe.style.height = height + 'px';
                return true;
            }
            return false;
        } catch (error) {
            console.error('Error resizing iframe:', error);
            return false;
        }
    },

    // Check if element is visible in viewport
    isElementVisible: function(selector) {
        try {
            const element = document.querySelector(selector);
            if (!element) return false;

            const rect = element.getBoundingClientRect();
            return (
                rect.top >= 0 &&
                rect.left >= 0 &&
                rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
                rect.right <= (window.innerWidth || document.documentElement.clientWidth)
            );
        } catch (error) {
            console.error('Error checking element visibility:', error);
            return false;
        }
    },

    // Smooth scroll to element
    scrollToElement: function(selector, behavior = 'smooth') {
        try {
            const element = document.querySelector(selector);
            if (element) {
                element.scrollIntoView({ behavior: behavior, block: 'center' });
                return true;
            }
            return false;
        } catch (error) {
            console.error('Error scrolling to element:', error);
            return false;
        }
    },

    // Debounce function for performance
    debounce: function(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    },

    // Throttle function for performance
    throttle: function(func, limit) {
        let inThrottle;
        return function() {
            const args = arguments;
            const context = this;
            if (!inThrottle) {
                func.apply(context, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    },

    // Format URL for display (truncate if too long)
    formatUrlForDisplay: function(url, maxLength = 60) {
        if (!url || url.length <= maxLength) return url;
        
        try {
            const urlObj = new URL(url);
            const domain = urlObj.hostname;
            const path = urlObj.pathname + urlObj.search;
            
            if (domain.length + path.length <= maxLength) {
                return domain + path;
            }
            
            const availableLength = maxLength - domain.length - 3; // 3 for "..."
            if (availableLength > 0) {
                return domain + path.substring(0, availableLength) + '...';
            }
            
            return domain;
        } catch (error) {
            // If URL parsing fails, just truncate the string
            return url.substring(0, maxLength - 3) + '...';
        }
    },

    // Validate URL format
    isValidUrl: function(string) {
        try {
            new URL(string);
            return true;
        } catch (error) {
            // Try with protocol prefix
            try {
                new URL('https://' + string);
                return true;
            } catch (error2) {
                return false;
            }
        }
    },

    // Get favicon URL for a domain
    getFaviconUrl: function(url) {
        try {
            const urlObj = new URL(url);
            return `${urlObj.protocol}//${urlObj.hostname}/favicon.ico`;
        } catch (error) {
            return '';
        }
    }
};

// Global helper functions for backward compatibility
window.selectText = window.browserViewerHelpers.selectText;
window.focusElement = window.browserViewerHelpers.focusElement;
window.copyToClipboard = window.browserViewerHelpers.copyToClipboard;

// Initialize helpers when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
        console.log('Browser Viewer JavaScript helpers loaded');
    });
} else {
    console.log('Browser Viewer JavaScript helpers loaded');
}
