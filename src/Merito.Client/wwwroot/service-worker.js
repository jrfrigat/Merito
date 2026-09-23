// Development: always fetch from the network so every change is visible on the next load.
self.importScripts('./service-worker-notifications.js');
self.addEventListener('fetch', () => { });
