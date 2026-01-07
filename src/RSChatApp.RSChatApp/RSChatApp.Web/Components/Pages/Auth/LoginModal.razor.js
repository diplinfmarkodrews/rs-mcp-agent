window.setupAuthenticationListener = (dotNetRef) => {
        window.authDotNetRef = dotNetRef;
        console.log('Authentication listener set up');
        
        // Listen for messages from the iframe
        window.addEventListener('message', function(event) {
            console.log('Received message:', event.data);
            if (event.data && typeof event.data === 'object') {
                if (event.data.type === 'auth-success') {
                    console.log('Authentication success received');
                    if (window.authDotNetRef) {
                        window.authDotNetRef.invokeMethodAsync('OnAuthenticationSuccess');
                    }
                } else if (event.data.type === 'auth-error') {
                    console.log('Authentication error received:', event.data.message);
                    if (window.authDotNetRef) {
                        window.authDotNetRef.invokeMethodAsync('OnAuthenticationError', event.data.message || 'Authentication failed');
                    }
                } else if (event.data.type === 'auth-retry') {
                    console.log('Authentication retry requested');
                    if (window.authDotNetRef) {
                        window.authDotNetRef.invokeMethodAsync('OnAuthenticationRetry');
                    }
                } else if (event.data.type === 'auth-close') {
                    console.log('Authentication close requested');
                    if (window.authDotNetRef) {
                        window.authDotNetRef.invokeMethodAsync('OnAuthenticationClose');
                    }
                }
            }
        });
    };

    window.monitorAuthenticationIframe = (iframe) => {
        console.log('Starting iframe monitoring');
        let checkCount = 0;
        const maxChecks = 120; // 2 minutes timeout
        
        const checkAuthStatus = () => {
            checkCount++;
            console.log('Checking auth status, attempt:', checkCount);
            
            try {
                // Try to access iframe content
                const iframeDoc = iframe.contentDocument || iframe.contentWindow.document;
                const iframeUrl = iframe.contentWindow.location.href;
                console.log('Current iframe URL:', iframeUrl);
                
                // Check for success indicators
                if (iframeUrl.includes('/auth/popup-auth-success') || 
                    iframeUrl.includes('/signin-oidc') ||
                    iframeUrl.includes('access_token') ||
                    (iframeDoc && iframeDoc.body && iframeDoc.body.innerHTML.includes('Authentication Successful'))) {
                    
                    console.log('Success detected in iframe');
                    if (window.authDotNetRef) {
                        window.authDotNetRef.invokeMethodAsync('OnAuthenticationSuccess');
                    }
                    return;
                }
                
                // Check for error indicators in URL
                if (iframeUrl.includes('/auth/error') || 
                    iframeUrl.includes('error=') ||
                    iframeUrl.includes('access_denied')) {
                    
                    console.log('Error detected in iframe URL');
                    // Extract error details from URL if available
                    const urlParams = new URLSearchParams(iframeUrl.split('?')[1] || '');
                    const errorParam = urlParams.get('error') || urlParams.get('error_description') || urlParams.get('message') || 'Authentication failed';
                    
                    if (window.authDotNetRef) {
                        window.authDotNetRef.invokeMethodAsync('OnAuthenticationError', errorParam);
                    }
                    return;
                }
                
                // Check for error indicators in document content
                if (iframeDoc && iframeDoc.body) {
                    const bodyText = iframeDoc.body.innerText.toLowerCase();
                    if (bodyText.includes('error') || 
                        bodyText.includes('failed') || 
                        bodyText.includes('invalid') ||
                        bodyText.includes('denied')) {
                        
                        console.log('Error detected in iframe content');
                        if (window.authDotNetRef) {
                            window.authDotNetRef.invokeMethodAsync('OnAuthenticationError', 'Authentication failed - please check your credentials');
                        }
                        return;
                    }
                }
                
            } catch (e) {
                // Cross-origin restrictions prevent access - this is normal for external auth providers
                console.log('Cross-origin access restricted (normal):', e.message);
                // Continue monitoring unless we've hit the timeout
            }
            
            // Check for timeout
            if (checkCount >= maxChecks) {
                console.log('Authentication timeout reached');
                if (window.authDotNetRef) {
                    window.authDotNetRef.invokeMethodAsync('OnAuthenticationError', 'Authentication timed out - please try again');
                }
                return;
            }
            
            // Continue monitoring
            setTimeout(checkAuthStatus, 1000);
        };
        
        // Handle iframe load errors
        iframe.addEventListener('error', function(e) {
            console.log('Iframe load error:', e);
            if (window.authDotNetRef) {
                window.authDotNetRef.invokeMethodAsync('OnConnectionError');
            }
        });
        
        // Start monitoring after a short delay
        setTimeout(checkAuthStatus, 500);
    };