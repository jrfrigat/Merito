self.addEventListener('push', event => {
    let payload = {};
    try {
        payload = event.data?.json() ?? {};
    } catch {
        payload = { body: event.data?.text() ?? '' };
    }

    const title = payload.title || 'Merito';
    const options = {
        body: payload.body || '',
        icon: 'icon-192.png',
        badge: 'icon-192.png',
        tag: payload.tag || 'merito-notification',
        data: { url: safeNotificationUrl(payload.url) }
    };
    event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('notificationclick', event => {
    event.notification.close();
    const target = safeNotificationUrl(event.notification.data?.url);
    event.waitUntil(focusOrOpen(target));
});

function safeNotificationUrl(value) {
    try {
        const url = new URL(value || '/notifications', self.location.origin);
        return url.origin === self.location.origin ? url.href : new URL('/notifications', self.location.origin).href;
    } catch {
        return new URL('/notifications', self.location.origin).href;
    }
}

async function focusOrOpen(target) {
    const windows = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
    const existing = windows.find(client => new URL(client.url).origin === self.location.origin);
    if (existing) {
        if ('navigate' in existing) await existing.navigate(target);
        return existing.focus();
    }
    return self.clients.openWindow(target);
}
