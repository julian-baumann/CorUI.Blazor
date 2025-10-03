export function afterStarted(blazor) {
    setTimeout(() => {

        try {
            // Prefer the native bridge you inject in WKWebView
            if (window.host?.notify) {
                window.host.notify('ready');
                return;
            }
        } catch {}

        // Fallback: raise a DOM event the host could listen for if needed
        try {
            document.dispatchEvent(new CustomEvent('blazor:ready'));
        } catch {}
    }, 100);
}