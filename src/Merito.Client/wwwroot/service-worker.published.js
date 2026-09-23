// Offline shell for the published app. The API is never cached: every /api request goes to the
// network, and navigation requests are answered with the cached index.html.

self.importScripts('./service-worker-assets.js');
self.importScripts('./service-worker-notifications.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));
self.addEventListener('message', event => {
    if (event.data === 'skipWaiting') self.skipWaiting();
});

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff2?$/, /\.png$/, /\.svg$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/, /^service-worker-assets\.js$/ ];
// The version probe must read the deployed manifest, never a cached copy.
const networkOnly = [ /service-worker-assets\.js(\?|$)/, /\/api\// ];

const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
    // Claiming the open pages makes the update reload land on the new worker, not the old one.
    await self.clients.claim();
}

async function onFetch(event) {
    if (event.request.method !== 'GET' || networkOnly.some(pattern => pattern.test(event.request.url)))
        return safeFetch(event.request);

    const shouldServeIndexHtml = event.request.mode === 'navigate'
        && !manifestUrlList.some(url => url === event.request.url);

    const request = shouldServeIndexHtml ? 'index.html' : event.request;
    const cache = await caches.open(cacheName);
    const cachedResponse = await cache.match(request);
    if (cachedResponse) return cachedResponse;

    try {
        return await fetch(event.request);
    } catch {
        if (shouldServeIndexHtml) {
            const shell = await cache.match('index.html');
            if (shell) return shell;
        }
        return Response.error();
    }
}

async function safeFetch(request) {
    try {
        return await fetch(request);
    } catch {
        return Response.error();
    }
}
