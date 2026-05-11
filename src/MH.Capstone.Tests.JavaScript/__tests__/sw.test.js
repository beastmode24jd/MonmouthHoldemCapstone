// sw.test.js — unit tests for the service worker fetch routing and caching logic.
//
// The SW runs in a ServiceWorkerGlobalScope that jsdom doesn't provide, so we
// stub the required globals (self, clients, caches, fetch, Response) before
// loading the module.  Handler functions are then called directly.

const ORIGIN = 'https://localhost:5001';
const CACHE_NAME = 'cwsa-offline-v1';

// ── Global stubs (must be set before require) ─────────────────────────────────

const mockCache = {
    put: jest.fn(),
    match: jest.fn(),
};

// jsdom defines `self` as a non-writable getter returning `window`, so a plain
// assignment silently fails.  Object.defineProperty (configurable: true in jsdom)
// lets us replace it with an object that has SW-specific methods.
const mockSelf = {
    skipWaiting: jest.fn(),
    location: { origin: ORIGIN },
    addEventListener: jest.fn(),
};
Object.defineProperty(global, 'self', { value: mockSelf, writable: true, configurable: true });

global.clients = { claim: jest.fn() };

global.caches = {
    open: jest.fn().mockResolvedValue(mockCache),
    match: jest.fn(),
    keys: jest.fn().mockResolvedValue([]),
    delete: jest.fn().mockResolvedValue(true),
};

global.fetch = jest.fn();

if (typeof global.Response === 'undefined') global.Response = {};
global.Response.error = jest.fn().mockReturnValue({ type: 'error', status: 0 });

const { handleInstall, handleActivate, handleFetch, networkFirst, cacheFirst } =
    require('../../MH.Capstone.WebApp/wwwroot/js/sw');

// ── Helpers ───────────────────────────────────────────────────────────────────

function makeNetworkResponse(ok = true) {
    const resp = { ok, type: 'basic', status: ok ? 200 : 500, clone: () => ({ ok, type: 'basic' }) };
    return resp;
}

function makeNavEvent(path) {
    let captured;
    const event = {
        request: { url: `${ORIGIN}${path}`, mode: 'navigate' },
        respondWith: jest.fn(p => { captured = p; }),
    };
    return { event, getResponse: () => captured };
}

function makeAssetEvent(path) {
    let captured;
    const event = {
        request: { url: `${ORIGIN}${path}`, mode: 'no-cors' },
        respondWith: jest.fn(p => { captured = p; }),
    };
    return { event, getResponse: () => captured };
}

beforeEach(() => {
    jest.clearAllMocks();
    global.caches.keys.mockResolvedValue([]);
    global.caches.match.mockResolvedValue(undefined);
    global.caches.open.mockResolvedValue(mockCache);
    global.caches.delete.mockResolvedValue(true);
});

// ── Install ───────────────────────────────────────────────────────────────────

describe('handleInstall', () => {
    it('calls skipWaiting', async () => {
        await handleInstall();
        expect(global.self.skipWaiting).toHaveBeenCalledTimes(1);
    });
});

// ── Activate ──────────────────────────────────────────────────────────────────

describe('handleActivate', () => {
    it('deletes old caches and keeps current one', async () => {
        global.caches.keys.mockResolvedValue(['cwsa-offline-v0', CACHE_NAME]);
        await handleActivate();
        expect(global.caches.delete).toHaveBeenCalledWith('cwsa-offline-v0');
        expect(global.caches.delete).not.toHaveBeenCalledWith(CACHE_NAME);
    });

    it('claims clients', async () => {
        await handleActivate();
        expect(global.clients.claim).toHaveBeenCalledTimes(1);
    });
});

// ── Fetch — offline pages (network-first) ─────────────────────────────────────

describe('handleFetch — /Sighting/Upload (network-first)', () => {
    it('tries network first and caches the response when online', async () => {
        const networkResp = makeNetworkResponse(true);
        global.fetch.mockResolvedValue(networkResp);
        const { event, getResponse } = makeNavEvent('/Sighting/Upload');

        handleFetch(event);

        expect(event.respondWith).toHaveBeenCalled();
        const response = await getResponse();
        expect(global.fetch).toHaveBeenCalled();
        expect(mockCache.put).toHaveBeenCalled();
        expect(response).toBe(networkResp);
    });

    it('returns cached response when network fails', async () => {
        const cachedResp = makeNetworkResponse(true);
        global.fetch.mockRejectedValue(new Error('offline'));
        global.caches.match.mockResolvedValue(cachedResp);
        const { event, getResponse } = makeNavEvent('/Sighting/Upload');

        handleFetch(event);

        const response = await getResponse();
        expect(response).toBe(cachedResp);
        expect(mockCache.put).not.toHaveBeenCalled();
    });

    it('returns Response.error() when network fails and no cache', async () => {
        global.fetch.mockRejectedValue(new Error('offline'));
        global.caches.match.mockResolvedValue(undefined);
        const { event, getResponse } = makeNavEvent('/Sighting/Upload');

        handleFetch(event);

        const response = await getResponse();
        expect(global.Response.error).toHaveBeenCalled();
        expect(response).toEqual({ type: 'error', status: 0 });
    });
});

describe('handleFetch — /Sighting/OfflineQueue (network-first)', () => {
    it('intercepts navigation and applies network-first strategy', async () => {
        const networkResp = makeNetworkResponse(true);
        global.fetch.mockResolvedValue(networkResp);
        const { event, getResponse } = makeNavEvent('/Sighting/OfflineQueue');

        handleFetch(event);

        expect(event.respondWith).toHaveBeenCalled();
        const response = await getResponse();
        expect(global.fetch).toHaveBeenCalled();
        expect(response).toBe(networkResp);
    });
});

describe('handleFetch — /Sighting/Create (network-first)', () => {
    it('intercepts the upload page alias', async () => {
        global.fetch.mockResolvedValue(makeNetworkResponse(true));
        const { event } = makeNavEvent('/Sighting/Create');

        handleFetch(event);

        expect(event.respondWith).toHaveBeenCalled();
    });
});

// ── Fetch — static assets (cache-first) ──────────────────────────────────────

describe('handleFetch — /js/offline-queue.js (cache-first)', () => {
    it('serves from cache without hitting network when cached', async () => {
        const cachedResp = makeNetworkResponse(true);
        global.caches.match.mockResolvedValue(cachedResp);
        const { event, getResponse } = makeAssetEvent('/js/offline-queue.js');

        handleFetch(event);

        expect(event.respondWith).toHaveBeenCalled();
        const response = await getResponse();
        expect(global.fetch).not.toHaveBeenCalled();
        expect(response).toBe(cachedResp);
    });

    it('fetches and caches the asset on first miss', async () => {
        const networkResp = makeNetworkResponse(true);
        global.caches.match.mockResolvedValue(undefined);
        global.fetch.mockResolvedValue(networkResp);
        const { event, getResponse } = makeAssetEvent('/js/offline-queue.js');

        handleFetch(event);

        const response = await getResponse();
        expect(global.fetch).toHaveBeenCalled();
        expect(mockCache.put).toHaveBeenCalled();
        expect(response).toBe(networkResp);
    });
});

// ── Fetch — pass-through (no interception) ────────────────────────────────────

describe('handleFetch — /Home/Index (pass-through)', () => {
    it('does not call respondWith for non-offline-page navigations', () => {
        const { event } = makeNavEvent('/Home/Index');

        handleFetch(event);

        expect(event.respondWith).not.toHaveBeenCalled();
    });
});
