function pushSupported() {
    return 'Notification' in window
        && 'serviceWorker' in navigator
        && 'PushManager' in window;
}

async function workerRegistration() {
    const existing = await navigator.serviceWorker.getRegistration();
    return existing ?? navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' });
}

function subscriptionJson(subscription) {
    if (!subscription) return '';
    const value = subscription.toJSON();
    return JSON.stringify({
        endpoint: subscription.endpoint,
        p256dh: value.keys?.p256dh ?? '',
        auth: value.keys?.auth ?? ''
    });
}

function applicationServerKey(value) {
    const padding = '='.repeat((4 - value.length % 4) % 4);
    const base64 = (value + padding).replace(/-/g, '+').replace(/_/g, '/');
    const raw = atob(base64);
    return Uint8Array.from(raw, character => character.charCodeAt(0));
}

window.meritoNotifications = {
    getState: async () => {
        if (!pushSupported()) return 'unsupported';
        if (Notification.permission !== 'granted') return Notification.permission;
        const subscription = await (await workerRegistration()).pushManager.getSubscription();
        return subscription ? 'enabled' : 'disabled';
    },

    getSubscription: async () => {
        if (!pushSupported() || Notification.permission !== 'granted') return '';
        return subscriptionJson(await (await workerRegistration()).pushManager.getSubscription());
    },

    subscribe: async publicKey => {
        if (!pushSupported()) return '';
        const permission = await Notification.requestPermission();
        if (permission !== 'granted') return '';

        const registration = await workerRegistration();
        const existing = await registration.pushManager.getSubscription();
        const subscription = existing ?? await registration.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: applicationServerKey(publicKey)
        });
        return subscriptionJson(subscription);
    },

    unsubscribe: async () => {
        if (!pushSupported()) return false;
        const subscription = await (await workerRegistration()).pushManager.getSubscription();
        return subscription ? subscription.unsubscribe() : true;
    }
};
