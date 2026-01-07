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