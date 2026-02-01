// BrowserViewer JavaScript Module
// Screenshot-based browser interaction support

// =============================================================================
// FORM ELEMENT DETECTION
// =============================================================================

// Enhanced element detection for form filling
window.getElementAtPosition = function(clientX, clientY) {
    try {
        const element = document.elementFromPoint(clientX, clientY);
        if (!element) return null;

        return {
            tagName: element.tagName.toLowerCase(),
            type: element.type || '',
            id: element.id || '',
            className: element.className || '',
            name: element.name || '',
            placeholder: element.placeholder || '',
            value: element.value || '',
            textContent: element.textContent?.trim() || '',
            selector: generateSelector(element),
            isFormElement: isFormElement(element),
            isClickable: isClickableElement(element)
        };
    } catch (error) {
        console.error('Error detecting element at position:', error);
        return null;
    }
};

// Generate CSS selector for an element
function generateSelector(element) {
    if (element.id) {
        return '#' + element.id;
    }
    
    if (element.name) {
        return `[name="${element.name}"]`;
    }
    
    if (element.className) {
        const firstClass = element.className.split(' ')[0];
        if (firstClass) {
            return '.' + firstClass;
        }
    }
    
    // Fallback to tag name
    return element.tagName.toLowerCase();
}

// Check if element is a form element
function isFormElement(element) {
    const formTags = ['input', 'textarea', 'select', 'button'];
    const formTypes = ['text', 'email', 'password', 'search', 'tel', 'url', 'number'];
    
    if (formTags.includes(element.tagName.toLowerCase())) {
        if (element.tagName.toLowerCase() === 'input') {
            return formTypes.includes(element.type?.toLowerCase() || 'text');
        }
        return true;
    }
    
    return false;
}

// Check if element is clickable
function isClickableElement(element) {
    const clickableTags = ['button', 'a', 'input'];
    const clickableTypes = ['button', 'submit', 'reset'];
    const clickableRoles = ['button', 'link'];
    
    const tagName = element.tagName.toLowerCase();
    const type = element.type?.toLowerCase();
    const role = element.getAttribute('role')?.toLowerCase();
    
    return clickableTags.includes(tagName) ||
           clickableTypes.includes(type) ||
           clickableRoles.includes(role) ||
           element.onclick !== null ||
           element.style.cursor === 'pointer';
}

// =============================================================================
// SCREENSHOT INTERACTION HELPERS
// =============================================================================

// Handle screenshot click with smart element detection
window.handleScreenshotClick = function(event, dotNetHelper) {
    try {
        const rect = event.target.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;
        
        console.log('Screenshot clicked at:', { x, y });
        
        // Try to detect what type of element might be at this position
        // This would be enhanced with the actual page content analysis
        dotNetHelper.invokeMethodAsync('HandleSmartClick', x, y)
            .catch(error => console.error('Error handling smart click:', error));
            
    } catch (error) {
        console.error('Error handling screenshot click:', error);
    }
};

// Handle keyboard input for form filling
window.handleKeyboardInput = function(event, dotNetHelper) {
    try {
        // Only handle special keys and input when focused on screenshot
        const key = event.key;
        
        if (key === 'Tab' || key === 'Enter' || key === 'Escape') {
            dotNetHelper.invokeMethodAsync('HandleKeyPress', key)
                .catch(error => console.error('Error handling key press:', error));
        }
        
    } catch (error) {
        console.error('Error handling keyboard input:', error);
    }
};

// =============================================================================
// FORM FILLING UTILITIES
// =============================================================================

// Validate CSS selector
window.validateSelector = function(selector) {
    try {
        document.querySelector(selector);
        return { isValid: true, error: null };
    } catch (error) {
        return { isValid: false, error: error.message };
    }
};

// Get element information by selector (for validation)
window.getElementInfo = function(selector) {
    try {
        const element = document.querySelector(selector);
        if (!element) {
            return { found: false };
        }
        
        return {
            found: true,
            tagName: element.tagName.toLowerCase(),
            type: element.type || '',
            visible: element.offsetParent !== null,
            enabled: !element.disabled,
            value: element.value || '',
            textContent: element.textContent?.trim() || ''
        };
    } catch (error) {
        return { found: false, error: error.message };
    }
};

// =============================================================================
// UTILITY FUNCTIONS
// =============================================================================

// Test function to verify module loading
window.testBrowserViewerJS = function() {
    console.log('🧪 BrowserViewer JavaScript module loaded successfully');
    return {
        status: 'loaded',
        features: [
            'Screenshot click detection',
            'Form element detection',
            'Smart selector generation',
            'Keyboard input handling'
        ]
    };
};

// Initialize module
console.log('✅ BrowserViewer JavaScript module initialized');


// BrowserUtil component JavaScript module
// Shared browser-related utility functions

// Select all text in an input element
export function selectText(selector) {
    try {
        const element = document.querySelector(selector);
        if (element && (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA')) {
            element.select();
            element.setSelectionRange(0, 99999); // For mobile devices
            return true;
        }
        return false;
    } catch (error) {
        console.warn('Error selecting text:', error);
        return false;
    }
}

// Highlight element temporarily
export function highlightElement(selector, duration = 2000) {
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
}

// Scroll element into view smoothly
export function scrollToElement(selector) {
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
}

// Get element information
export function getElementInfo(selector) {
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

// Get iframe-specific element information with coordinates
export function getIframeElementInfo(iframeElement, selector) {
    try {
        const iframeDoc = iframeElement.contentDocument || iframeElement.contentWindow?.document;
        if (!iframeDoc) {
            console.warn('Cannot access iframe document');
            return [];
        }

        const elements = iframeDoc.querySelectorAll(selector);
        const iframeRect = iframeElement.getBoundingClientRect();

        return Array.from(elements).map(el => {
            const rect = el.getBoundingClientRect();
            return {
                tagName: el.tagName.toLowerCase(),
                text: el.textContent?.trim() || '',
                value: el.value || '',
                href: el.href || '',
                src: el.src || '',
                id: el.id || '',
                className: el.className || '',
                type: el.type || '',
                disabled: el.disabled || false,
                visible: el.offsetParent !== null,
                // Coordinates relative to the parent page
                x: rect.left + iframeRect.left,
                y: rect.top + iframeRect.top,
                width: rect.width,
                height: rect.height
            };
        });
    } catch (error) {
        console.error('Error getting iframe element info:', error);
        return [];
    }
}

// Highlight elements within the iframe
export function highlightIframeElement(iframeElement, selector, duration = 2000, color = '#ff6b6b') {
    try {
        const iframeDoc = iframeElement.contentDocument || iframeElement.contentWindow?.document;
        if (!iframeDoc) {
            console.warn('Cannot access iframe document');
            return 0;
        }

        const elements = iframeDoc.querySelectorAll(selector);
        elements.forEach(element => {
            const originalStyle = element.style.cssText;
            element.style.cssText += `
                outline: 3px solid ${color} !important;
                outline-offset: 2px !important;
                background-color: ${color}20 !important;
                transition: all 0.3s ease !important;
            `;

            setTimeout(() => {
                element.style.cssText = originalStyle;
            }, duration);
        });

        return elements.length;
    } catch (error) {
        console.error('Error highlighting iframe element:', error);
        return 0;
    }
}

// Helper function to check if iframe content is accessible
export function checkIframeAccess(iframeElement) {
    try {
        const doc = iframeElement.contentDocument || iframeElement.contentWindow?.document;
        return doc !== null;
    } catch (error) {
        console.warn('Iframe access check failed:', error);
        return false;
    }
}
