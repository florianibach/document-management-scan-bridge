const enabledKey = "paperless-scan-bridge.notifications.enabled";
const deliveredPrefix = "paperless-scan-bridge.notifications.delivered.";
let registrationPromise;

function serviceWorkerUrl() {
    return new URL("scanNotificationServiceWorker.js", document.baseURI).pathname;
}

async function ensureServiceWorker() {
    if (!("serviceWorker" in navigator)) return null;
    registrationPromise ??= navigator.serviceWorker.register(serviceWorkerUrl()).catch(error => {
        console.warn("Notification service worker could not be registered; using the browser fallback.", error);
        return null;
    });
    return registrationPromise;
}

export function getState() {
    if (!("Notification" in window)) return "unsupported";
    if (Notification.permission === "denied") return "denied";
    if (Notification.permission === "granted" && sessionStorage.getItem(enabledKey) === "true") return "enabled";
    return "disabled";
}

export async function enable() {
    if (!("Notification" in window)) return "unsupported";
    const permission = Notification.permission === "default"
        ? await Notification.requestPermission()
        : Notification.permission;
    const enabled = permission === "granted";
    sessionStorage.setItem(enabledKey, enabled ? "true" : "false");
    if (enabled) await ensureServiceWorker();
    return enabled ? "enabled" : permission === "denied" ? "denied" : "disabled";
}

export function disable() {
    sessionStorage.setItem(enabledKey, "false");
    return getState();
}

export async function show(title, message, eventKey) {
    if (getState() !== "enabled") return false;
    const deliveredKey = `${deliveredPrefix}${eventKey}`;
    if (sessionStorage.getItem(deliveredKey) === "true") return false;
    const options = { body: message, tag: eventKey, data: { url: document.baseURI } };
    try {
        const registration = await ensureServiceWorker();
        if (registration) await registration.showNotification(title, options);
        else {
            const notification = new Notification(title, options);
            notification.onclick = () => { window.focus(); notification.close(); };
        }
        sessionStorage.setItem(deliveredKey, "true");
        return true;
    } catch (error) {
        console.warn("Browser notification could not be delivered.", error);
        return false;
    }
}
