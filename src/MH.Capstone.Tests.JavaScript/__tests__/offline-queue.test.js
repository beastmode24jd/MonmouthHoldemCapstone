const { IDBFactory } = require('fake-indexeddb');

// Set up fake IndexedDB before requiring the module under test.
// The module reads indexedDB as a global, so we must set it before the require.
global.indexedDB = new IDBFactory();

const {
    generateGuid,
    enqueueOfflineSighting,
    getAllQueuedSightings,
    updateQueuedSighting,
    deleteQueuedSighting,
    syncOfflineQueue,
} = require('../../MH.Capstone.WebApp/wwwroot/js/offline-queue');

// ── Helpers ───────────────────────────────────────────────────────────────────

/** Unique test-user ID so each test gets an isolated IndexedDB store. */
function testUserId() {
    return `testuser-${generateGuid()}`;
}

function makeSightingItem(overrides = {}) {
    return {
        speciesName: 'Test Species',
        latitude: '45.00000',
        longitude: '-123.00000',
        timestamp: '2026-05-01T10:00',
        timezone: 'America/Los_Angeles',
        description: 'Jest test sighting',
        imageDataUrl: 'data:image/jpeg;base64,/9j/4AAQSkZJRg==',
        imageFileName: 'test.jpg',
        clientSightingId: generateGuid(),
        ...overrides,
    };
}

// ── generateGuid ──────────────────────────────────────────────────────────────

describe('generateGuid', () => {
    test('returns a string in UUID v4 format', () => {
        const guid = generateGuid();
        expect(typeof guid).toBe('string');
        expect(guid).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i);
    });

    test('generates unique values on successive calls', () => {
        const guids = new Set(Array.from({ length: 20 }, generateGuid));
        expect(guids.size).toBe(20);
    });
});

// ── enqueueOfflineSighting ────────────────────────────────────────────────────

describe('enqueueOfflineSighting', () => {
    test('stores a sighting and returns it with status "pending"', async () => {
        const userId = testUserId();
        const item = makeSightingItem();

        const result = await enqueueOfflineSighting(userId, item);

        expect(result.status).toBe('pending');
        expect(result.speciesName).toBe(item.speciesName);
        expect(result.clientSightingId).toBe(item.clientSightingId);
        expect(typeof result.id).toBe('string');
        expect(typeof result.enqueuedAt).toBe('string');
    });

    test('stored item is retrievable via getAllQueuedSightings', async () => {
        const userId = testUserId();
        const item = makeSightingItem({ speciesName: 'Mountain Goat' });

        await enqueueOfflineSighting(userId, item);
        const all = await getAllQueuedSightings(userId);

        expect(all.length).toBe(1);
        expect(all[0].speciesName).toBe('Mountain Goat');
    });

    test('multiple sightings can be queued independently', async () => {
        const userId = testUserId();
        await enqueueOfflineSighting(userId, makeSightingItem({ speciesName: 'Elk' }));
        await enqueueOfflineSighting(userId, makeSightingItem({ speciesName: 'Coyote' }));

        const all = await getAllQueuedSightings(userId);
        expect(all.length).toBe(2);
    });

    test('queues are isolated per user ID', async () => {
        const user1 = testUserId();
        const user2 = testUserId();

        await enqueueOfflineSighting(user1, makeSightingItem({ speciesName: 'For User 1' }));

        const user2Queue = await getAllQueuedSightings(user2);
        expect(user2Queue.length).toBe(0);
    });
});

// ── getAllQueuedSightings ─────────────────────────────────────────────────────

describe('getAllQueuedSightings', () => {
    test('returns an empty array when no items are queued', async () => {
        const userId = testUserId();
        const items = await getAllQueuedSightings(userId);
        expect(items).toEqual([]);
    });
});

// ── updateQueuedSighting ──────────────────────────────────────────────────────

describe('updateQueuedSighting', () => {
    test('updates the specified field on an existing item', async () => {
        const userId = testUserId();
        const enqueued = await enqueueOfflineSighting(userId, makeSightingItem());

        await updateQueuedSighting(userId, enqueued.id, { status: 'synced', serverId: 'server-abc' });

        const all = await getAllQueuedSightings(userId);
        expect(all[0].status).toBe('synced');
        expect(all[0].serverId).toBe('server-abc');
    });

    test('does not affect other fields when patching', async () => {
        const userId = testUserId();
        const enqueued = await enqueueOfflineSighting(userId, makeSightingItem({ speciesName: 'Wolf' }));

        await updateQueuedSighting(userId, enqueued.id, { status: 'failed' });

        const all = await getAllQueuedSightings(userId);
        expect(all[0].speciesName).toBe('Wolf');
        expect(all[0].status).toBe('failed');
    });

    test('is a no-op when the id does not exist', async () => {
        const userId = testUserId();
        await enqueueOfflineSighting(userId, makeSightingItem());

        // Should not throw
        await expect(
            updateQueuedSighting(userId, 'nonexistent-id', { status: 'synced' })
        ).resolves.toBeUndefined();

        const all = await getAllQueuedSightings(userId);
        expect(all[0].status).toBe('pending');
    });
});

// ── deleteQueuedSighting ──────────────────────────────────────────────────────

describe('deleteQueuedSighting', () => {
    test('removes the item from the queue', async () => {
        const userId = testUserId();
        const enqueued = await enqueueOfflineSighting(userId, makeSightingItem());

        await deleteQueuedSighting(userId, enqueued.id);

        const all = await getAllQueuedSightings(userId);
        expect(all.length).toBe(0);
    });

    test('only removes the targeted item when multiple items are queued', async () => {
        const userId = testUserId();
        const first = await enqueueOfflineSighting(userId, makeSightingItem({ speciesName: 'Eagle' }));
        await enqueueOfflineSighting(userId, makeSightingItem({ speciesName: 'Hawk' }));

        await deleteQueuedSighting(userId, first.id);

        const remaining = await getAllQueuedSightings(userId);
        expect(remaining.length).toBe(1);
        expect(remaining[0].speciesName).toBe('Hawk');
    });
});

// ── syncOfflineQueue ──────────────────────────────────────────────────────────

describe('syncOfflineQueue', () => {
    beforeEach(() => {
        global.fetch = jest.fn();
    });

    afterEach(() => {
        jest.restoreAllMocks();
    });

    test('marks item as synced after a successful server response', async () => {
        const userId = testUserId();
        await enqueueOfflineSighting(userId, makeSightingItem());

        global.fetch.mockResolvedValueOnce({ ok: true, redirected: false });

        await syncOfflineQueue(userId, 'test-csrf-token');

        const all = await getAllQueuedSightings(userId);
        expect(all[0].status).toBe('synced');
    });

    test('marks item as failed when server returns a non-OK response', async () => {
        const userId = testUserId();
        await enqueueOfflineSighting(userId, makeSightingItem());

        global.fetch.mockResolvedValueOnce({ ok: false, redirected: false, status: 500 });

        await syncOfflineQueue(userId, 'test-csrf-token');

        const all = await getAllQueuedSightings(userId);
        expect(all[0].status).toBe('failed');
        expect(all[0].failureReason).toContain('500');
    });

    test('marks item as failed when fetch throws a network error', async () => {
        const userId = testUserId();
        await enqueueOfflineSighting(userId, makeSightingItem());

        global.fetch.mockRejectedValueOnce(new Error('Network error'));

        await syncOfflineQueue(userId, 'test-csrf-token');

        const all = await getAllQueuedSightings(userId);
        expect(all[0].status).toBe('failed');
        expect(all[0].failureReason).toBe('Network error');
    });

    test('skips items that are already synced', async () => {
        const userId = testUserId();
        const item = await enqueueOfflineSighting(userId, makeSightingItem());
        await updateQueuedSighting(userId, item.id, { status: 'synced' });

        await syncOfflineQueue(userId, null);

        expect(global.fetch).not.toHaveBeenCalled();
    });

    test('retries items that previously failed', async () => {
        const userId = testUserId();
        const item = await enqueueOfflineSighting(userId, makeSightingItem());
        await updateQueuedSighting(userId, item.id, { status: 'failed', failureReason: 'previous error' });

        global.fetch.mockResolvedValueOnce({ ok: true, redirected: false });

        await syncOfflineQueue(userId, null);

        const all = await getAllQueuedSightings(userId);
        expect(all[0].status).toBe('synced');
    });
});
