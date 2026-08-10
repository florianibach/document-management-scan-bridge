let cleanup;

export function open(element, component) {
    close();
    const abort = new AbortController();
    const dismiss = event => {
        if (event.type === "keydown" && event.key !== "Escape") return;
        if (event.type === "pointerdown" && element.contains(event.target)) return;
        component.invokeMethodAsync("DismissAsync");
    };
    document.addEventListener("pointerdown", dismiss, { signal: abort.signal });
    document.addEventListener("keydown", dismiss, { signal: abort.signal });
    cleanup = () => abort.abort();
}

export function close() {
    cleanup?.();
    cleanup = undefined;
}
