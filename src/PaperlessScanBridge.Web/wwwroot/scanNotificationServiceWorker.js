self.addEventListener("notificationclick", event => {
    event.notification.close();
    const targetUrl = event.notification.data?.url ?? self.registration.scope;
    event.waitUntil((async () => {
        const windows = await self.clients.matchAll({ type: "window", includeUncontrolled: true });
        const existing = windows.find(client => new URL(client.url).origin === new URL(targetUrl).origin);
        if (existing) {
            await existing.focus();
            if ("navigate" in existing) await existing.navigate(targetUrl);
            return;
        }
        await self.clients.openWindow(targetUrl);
    })());
});
