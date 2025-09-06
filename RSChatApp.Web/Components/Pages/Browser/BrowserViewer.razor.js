// BrowserViewer component JavaScript module
// This file is automatically loaded by Blazor for the BrowserViewer component

// Setup iframe event forwarding to Playwright page
export function setupIframeEventForwarding(iframeElement, dotNetHelper) {
    try {
        if (!iframeElement || !dotNetHelper) {
            console.warn('Invalid parameters for setupIframeEventForwarding');
            return;
        }

        console.log('Setting up iframe event forwarding...');

        // Wait for iframe to load and access its document
        const setupEvents = () => {
            try {
                const iframeDoc = iframeElement.contentDocument || iframeElement.contentWindow?.document;
                
                if (!iframeDoc) {
                    console.warn('Cannot access iframe document - may be cross-origin or not loaded');
                    return;
                }

                console.log('Iframe document accessible, setting up event listeners...');

                // Forward click events
                iframeDoc.addEventListener('click', function(event) {
                    // Always prevent default behavior on static content
                    event.preventDefault();
                    event.stopPropagation();
                    
                    // Check if the clicked element is a link
                    let clickedElement = event.target;
                    let linkElement = null;
                    
                    // Check if this might be a cookie banner or similar interactive element
                    let isCookieBannerClick = false;
                    let isButtonClick = false;
                    let currentElement = clickedElement;
                    while (currentElement && currentElement !== iframeDoc) {
                        const classList = currentElement.className ? currentElement.className.toLowerCase() : '';
                        const id = currentElement.id ? currentElement.id.toLowerCase() : '';
                        const innerText = currentElement.innerText ? currentElement.innerText.toLowerCase() : '';
                        const tagName = currentElement.tagName.toLowerCase();
                        const role = currentElement.getAttribute('role')?.toLowerCase() || '';
                        
                        // Detect interactive elements (buttons, popups, banners)
                        if (tagName === 'button' || 
                            role === 'button' || 
                            classList.includes('btn') ||
                            classList.includes('button')) {
                            isButtonClick = true;
                        }
                        
                        // Detect cookie banner related elements (expanded detection)
                        if (classList.includes('cookie') || 
                            classList.includes('consent') || 
                            classList.includes('banner') ||
                            classList.includes('privacy') ||
                            classList.includes('gdpr') ||
                            classList.includes('modal') ||
                            classList.includes('popup') ||
                            classList.includes('overlay') ||
                            classList.includes('dialog') ||
                            classList.includes('notice') ||
                            id.includes('cookie') ||
                            id.includes('consent') ||
                            id.includes('modal') ||
                            id.includes('popup') ||
                            innerText.includes('accept') ||
                            innerText.includes('decline') ||
                            innerText.includes('reject') ||
                            innerText.includes('cookie') ||
                            innerText.includes('privacy') ||
                            innerText.includes('agree') ||
                            innerText.includes('continue') ||
                            innerText.includes('understand')) {
                            isCookieBannerClick = true;
                            console.log('Detected interactive/cookie banner click on element:', currentElement, 'Text:', innerText);
                            break;
                        }
                        currentElement = currentElement.parentElement;
                    }
                    
                    // Find the nearest link element (traverse up the DOM)
                    currentElement = clickedElement;
                    while (currentElement && currentElement !== iframeDoc) {
                        if (currentElement.tagName === 'A' && currentElement.href) {
                            linkElement = currentElement;
                            break;
                        }
                        currentElement = currentElement.parentElement;
                    }
                    
                    // If we clicked on a link, handle it specially
                    if (linkElement && !isCookieBannerClick && !isButtonClick) {
                        const href = linkElement.href;
                        console.log('Link clicked:', href);
                        
                        // Filter out tracking URLs and unwanted redirects
                        if (href.includes('google.com/ccm/collect') || 
                            href.includes('googletagmanager.com') ||
                            href.includes('analytics.google.com') ||
                            href.includes('google-analytics.com')) {
                            console.log('Blocked tracking URL:', href);
                            return; // Don't forward tracking clicks
                        }
                        
                        // For legitimate navigation, navigate via Playwright instead of forwarding click
                        if (href.startsWith('http') || href.startsWith('/')) {
                            console.log('Navigating to URL via Playwright:', href);
                            dotNetHelper.invokeMethodAsync('ForwardNavigation', href)
                                .catch(error => console.error('Error forwarding navigation:', error));
                            return;
                        }
                    }
                    
                    // Get precise click coordinates 
                    // Use both iframe coordinates and document coordinates for better accuracy
                    const iframeRect = iframeElement.getBoundingClientRect();
                    const iframeDoc = iframeElement.contentDocument;
                    const scrollX = iframeDoc.documentElement.scrollLeft || iframeDoc.body.scrollLeft || 0;
                    const scrollY = iframeDoc.documentElement.scrollTop || iframeDoc.body.scrollTop || 0;
                    
                    // Calculate coordinates relative to the actual page (accounting for scrolling)
                    const x = event.clientX + scrollX;
                    const y = event.clientY + scrollY;
                    
                    console.log('Click coordinates - iframe:', {
                        clientX: event.clientX, 
                        clientY: event.clientY,
                        scrollX: scrollX,
                        scrollY: scrollY,
                        finalX: x,
                        finalY: y,
                        target: event.target.tagName,
                        targetText: event.target.innerText?.substring(0, 50)
                    });
                    
                    if (isCookieBannerClick || isButtonClick) {
                        console.log('Forwarding interactive element click to Playwright page at:', x, y);
                        // Use special method for interactive element clicks that provides longer delay
                        dotNetHelper.invokeMethodAsync('ForwardCookieBannerClick', x, y)
                            .catch(error => console.error('Error forwarding interactive click:', error));
                    } else {
                        console.log('Forwarding click to Playwright page at:', x, y);
                        dotNetHelper.invokeMethodAsync('ForwardClick', x, y)
                            .catch(error => console.error('Error forwarding click:', error));
                    }
                }, true);

                // Forward hover events for popups and tooltips
                iframeDoc.addEventListener('mouseover', function(event) {
                    const iframeDoc = iframeElement.contentDocument;
                    const scrollX = iframeDoc.documentElement.scrollLeft || iframeDoc.body.scrollLeft || 0;
                    const scrollY = iframeDoc.documentElement.scrollTop || iframeDoc.body.scrollTop || 0;
                    
                    const x = event.clientX + scrollX;
                    const y = event.clientY + scrollY;
                    
                    console.log('Forwarding hover to Playwright page at:', x, y);
                    dotNetHelper.invokeMethodAsync('ForwardHover', x, y)
                        .catch(error => console.error('Error forwarding hover:', error));
                }, true);

                // Forward mouse leave events
                iframeDoc.addEventListener('mouseleave', function(event) {
                    console.log('Mouse left iframe document');
                    dotNetHelper.invokeMethodAsync('ForwardMouseLeave')
                        .catch(error => console.error('Error forwarding mouse leave:', error));
                }, true);

                // Forward keyboard events
                iframeDoc.addEventListener('keydown', function(event) {
                    console.log('Forwarding key press to Playwright page:', event.key);
                    
                    // Don't prevent default for navigation keys, but forward them
                    dotNetHelper.invokeMethodAsync('ForwardKeyPress', event.key)
                        .catch(error => console.error('Error forwarding key press:', error));
                }, true);

                // Forward typing in input fields
                iframeDoc.addEventListener('input', function(event) {
                    if (event.target && (event.target.tagName === 'INPUT' || event.target.tagName === 'TEXTAREA')) {
                        const value = event.target.value;
                        console.log('Forwarding input to Playwright page:', value);
                        
                        dotNetHelper.invokeMethodAsync('ForwardType', value)
                            .catch(error => console.error('Error forwarding type:', error));
                    }
                }, true);

                // Forward form submissions
                iframeDoc.addEventListener('submit', function(event) {
                    event.preventDefault();
                    console.log('Form submission detected, preventing default and refreshing content');
                    
                    // Just refresh the content after a brief delay
                    setTimeout(() => {
                        dotNetHelper.invokeMethodAsync('RefreshContent')
                            .catch(error => console.error('Error refreshing content:', error));
                    }, 500);
                }, true);

                console.log('Iframe event forwarding setup complete');
            } catch (error) {
                console.error('Error setting up iframe event listeners:', error);
            }
        };

        // Setup events immediately if iframe is already loaded
        if (iframeElement.contentDocument && iframeElement.contentDocument.readyState === 'complete') {
            setupEvents();
        } else {
            // Wait for iframe to load
            iframeElement.addEventListener('load', setupEvents);
        }

    } catch (error) {
        console.error('Error in setupIframeEventForwarding:', error);
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

// Get iframe-specific element information with coordinates
export function getIframeElementInfo(iframeElement, selector) {
    try {
        const iframeDoc = iframeElement.contentDocument || iframeElement.contentWindow?.document;
        if (!iframeDoc) {
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
                iframeBounds: {
                    x: rect.x + iframeRect.x,
                    y: rect.y + iframeRect.y,
                    width: rect.width,
                    height: rect.height
                },
                localBounds: {
                    x: rect.x,
                    y: rect.y,
                    width: rect.width,
                    height: rect.height
                }
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
            return 0;
        }

        const elements = iframeDoc.querySelectorAll(selector);
        elements.forEach(element => {
            const originalStyle = element.style.cssText;
            element.style.cssText += `
                outline: 3px solid ${color} !important;
                outline-offset: 2px !important;
                background-color: ${color}1a !important;
                transition: all 0.3s ease !important;
                position: relative !important;
                z-index: 1000 !important;
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
