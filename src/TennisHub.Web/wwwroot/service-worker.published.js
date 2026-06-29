// Production service worker for TennisHub PWA.
// Pre-caches all Blazor WASM assets listed in service-worker-assets.js,
// then serves them from cache-first on subsequent loads.

const cacheName = 'tennishub-cache-v1';

// Populated by the Blazor publish pipeline via ServiceWorkerAssetsManifest.
self.importScripts('./service-worker-assets.js');

self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

async function onInstall(event) {
    const cache = await caches.open(cacheName);
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => asset.url)
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await cache.addAll(assetsRequests);
}

async function onActivate(_event) {
    const cacheKeys = await caches.keys();
    await Promise.all(
        cacheKeys
            .filter(key => key !== cacheName)
            .map(key => caches.delete(key))
    );
}

async function onFetch(event) {
    if (event.request.method !== 'GET') return fetch(event.request);

    const cache = await caches.open(cacheName);
    const cachedResponse = await cache.match(event.request, { ignoreSearch: true });
    if (cachedResponse) return cachedResponse;

    // Navigation requests → serve the app shell so deep links work offline
    if (event.request.mode === 'navigate') {
        const cached = await cache.match('/index.html');
        if (cached) return cached;
    }

    return fetch(event.request);
}
