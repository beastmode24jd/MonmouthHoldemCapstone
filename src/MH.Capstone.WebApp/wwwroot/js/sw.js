const CACHE_NAME = 'cwsa-offline-v1';

const OFFLINE_PAGES = [
    '/Sighting/Upload',
    '/Sighting/Create',
    '/Sighting/OfflineQueue',
];

function isOfflinePage(pathname) {
    return OFFLINE_PAGES.some(p => p.toLowerCase() === pathname.toLowerCase());
}

function isStaticAsset(pathname) {
    return pathname.startsWith('/js/') ||
        pathname.startsWith('/css/') ||
        pathname.startsWith('/lib/');
}

async function handleInstall() {
    self.skipWaiting();
}

async function handleActivate() {
    const keys = await caches.keys();
    await Promise.all(keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k)));
    await clients.claim();
}

// Network-first: try network, cache on success, fall back to cache when offline.
async function networkFirst(request) {
    try {
        const response = await fetch(request);
        if (response.ok) {
            const cache = await caches.open(CACHE_NAME);
            cache.put(request, response.clone());
        }
        return response;
    } catch {
        const cached = await caches.match(request);
        return cached != null ? cached : Response.error();
    }
}

// Cache-first: serve from cache immediately; fill cache on first network hit.
async function cacheFirst(request) {
    const cached = await caches.match(request);
    if (cached != null) return cached;
    try {
        const response = await fetch(request);
        if (response.ok || response.type === 'opaque') {
            const cache = await caches.open(CACHE_NAME);
            cache.put(request, response.clone());
        }
        return response;
    } catch {
        return Response.error();
    }
}

// Handles POST form submissions to offline pages. Normally sighting-upload.js
// intercepts these before they reach the SW via its submit listener, but if
// navigator.onLine is stale (e.g. Chrome DevTools throttling keeps it true while
// blocking the network) the JS gate doesn't fire and the POST reaches the SW.
// On network failure we redirect to the offline queue rather than surfacing a
// browser-level ERR_FAILED / "cannot find resource" error.
async function handleOfflineFormPost(request) {
    try {
        return await fetch(request);
    } catch {
        return Response.redirect('/Sighting/OfflineQueue', 302);
    }
}

function handleFetch(event) {
    const url = new URL(event.request.url);
    if (event.request.mode === 'navigate' && isOfflinePage(url.pathname)) {
        if (event.request.method === 'POST') {
            event.respondWith(handleOfflineFormPost(event.request));
            return;
        }
        event.respondWith(networkFirst(event.request));
        return;
    }
    if (url.origin === self.location.origin && isStaticAsset(url.pathname)) {
        event.respondWith(cacheFirst(event.request));
    }
}

self.addEventListener('install', (event) => event.waitUntil(handleInstall()));
self.addEventListener('activate', (event) => event.waitUntil(handleActivate()));
self.addEventListener('fetch', handleFetch);

if (typeof module !== 'undefined') {
    module.exports = { handleInstall, handleActivate, handleFetch, handleOfflineFormPost, networkFirst, cacheFirst };
}
