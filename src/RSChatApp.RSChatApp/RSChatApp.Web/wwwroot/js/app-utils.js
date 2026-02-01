// App-wide JavaScript utilities

// Submit logout request to controller endpoint
window.postLogoutRequest = async function() {
    try {
        const response = await fetch('/auth/logout/', { 
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        
        if (!response.ok) {
            const errorText = await response.text();
            console.error('Logout error:', errorText);
            showToast(errorText, 'error', 5000);   
        }
        return;
    } catch(err) {
        console.error('Logout error:', err);
        showToast(err.message, 'error', 5000);
    }
};

window.postLoginRequest = async function(loginRequest) {
    
    try {
        const response = await fetch('/auth/legacy-login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(loginRequest),
        });

        if (!response.ok) {
            const errorText = await response.text();
            return {
                success: false,
                errorMessage: `Login failed: ${response.status} ${errorText}`
            };
        }

        const data = await response.json();

        // Transform the response to match LoginResult format
        return {
            success: data.success || false,
            errorMessage: data.errorMessage || (data.success ? null : 'Login failed')
        };
    } catch(err) {
        console.error('Login error:', err);
        return {
            success: false,
            errorMessage: err.message || 'Network error during login'
        };
    }
};
window.reconnectBlazorCircuit = async function() {
    // Get the Blazor circuit
    const circuit = window['Blazor'];

    if (circuit && circuit._internal) {
        // Force disconnect
        circuit._internal.forceCloseConnection?.();

        // Wait a bit then reload
        await new Promise(resolve => setTimeout(resolve, 100));
        window.location.reload();
    } else {
        // Fallback - just reload
        window.location.reload();
    }
};
// Show temporary toast message
window.showToast = function(message, type = 'info', duration = 3000) {
    try {
        // Remove existing toast
        const existingToast = document.querySelector('.app-toast');
        if (existingToast) {
            existingToast.remove();
        }

        // Create toast element
        const toast = document.createElement('div');
        toast.className = `app-toast app-toast-${type}`;
        toast.textContent = message;
        toast.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: ${type === 'error' ? '#dc3545' : type === 'success' ? '#28a745' : type === 'warning' ? '#ffc107' : '#17a2b8'};
            color: ${type === 'warning' ? '#212529' : 'white'};
            padding: 12px 16px;
            border-radius: 6px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            z-index: 10000;
            font-size: 14px;
            font-weight: 500;
            max-width: 350px;
            word-wrap: break-word;
            animation: slideInRight 0.3s ease;
            border: none;
            backdrop-filter: blur(10px);
        `;

        // Add animation styles if not already present
        if (!document.querySelector('#app-toast-styles')) {
            const style = document.createElement('style');
            style.id = 'app-toast-styles';
            style.textContent = `
                @keyframes slideInRight {
                    from { transform: translateX(100%); opacity: 0; }
                    to { transform: translateX(0); opacity: 1; }
                }
                @keyframes slideOutRight {
                    from { transform: translateX(0); opacity: 1; }
                    to { transform: translateX(100%); opacity: 0; }
                }
                .app-toast {
                    cursor: pointer;
                    transition: transform 0.2s ease;
                }
                .app-toast:hover {
                    transform: translateX(-5px);
                }
            `;
            document.head.appendChild(style);
        }

        // Click to dismiss
        toast.addEventListener('click', () => {
            if (toast.parentNode) {
                toast.style.animation = 'slideOutRight 0.3s ease';
                setTimeout(() => toast.remove(), 300);
            }
        });

        document.body.appendChild(toast);

        // Auto remove after duration
        setTimeout(() => {
            if (toast.parentNode) {
                toast.style.animation = 'slideOutRight 0.3s ease';
                setTimeout(() => toast.remove(), 300);
            }
        }, duration);

        return true;
    } catch (error) {
        console.error('Error showing toast:', error);
        return false;
    }
}
