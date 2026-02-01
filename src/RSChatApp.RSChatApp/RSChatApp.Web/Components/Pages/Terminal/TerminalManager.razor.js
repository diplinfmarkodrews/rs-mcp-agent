let isResizing = false;
let startY = 0;
let startHeight = 0;
let onHeightChange = null;

export function initResize(_element, dotNetHelper, callback) {
    onHeightChange = (height) => {
        dotNetHelper.invokeMethodAsync(callback, height);
    };
}

export function startResize(e, currentHeight) {
    isResizing = true;
    startY = e.clientY;
    startHeight = currentHeight;

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', stopResize);
    document.body.style.cursor = 'ns-resize';
    document.body.style.userSelect = 'none';
}

function handleMouseMove(e) {
    if (!isResizing) return;

    const deltaY = startY - e.clientY;
    const newHeight = Math.max(100, Math.min(600, startHeight + deltaY));

    if (onHeightChange) {
        onHeightChange(newHeight);
    }
}

function stopResize() {
    isResizing = false;
    document.removeEventListener('mousemove', handleMouseMove);
    document.removeEventListener('mouseup', stopResize);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
}

