const enabledKey = "paperless-scan-bridge.notifications.enabled";
const deliveredPrefix = "paperless-scan-bridge.notifications.delivered.";

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
    return enabled ? "enabled" : permission === "denied" ? "denied" : "disabled";
}

export function disable() {
    sessionStorage.setItem(enabledKey, "false");
    return getState();
}

export function show(title, message, eventKey) {
    if (getState() !== "enabled") return false;
    const deliveredKey = `${deliveredPrefix}${eventKey}`;
    if (sessionStorage.getItem(deliveredKey) === "true") return false;
    sessionStorage.setItem(deliveredKey, "true");
    const notification = new Notification(title, { body: message, tag: eventKey });
    notification.onclick = () => {
        window.focus();
        notification.close();
    };
    return true;
}
