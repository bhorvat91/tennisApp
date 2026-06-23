// Development service worker – passes requests straight through to the network.
// In production this file is replaced by service-worker.published.js which
// pre-caches all Blazor WASM assets.

self.addEventListener('fetch', () => { });
