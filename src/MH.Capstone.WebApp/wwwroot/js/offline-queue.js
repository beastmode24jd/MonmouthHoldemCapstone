// offline-queue.js — CSP-177: IndexedDB-backed offline sightings queue with auto-sync.

// Each user gets their own database so we never need to bump the version to add
// new object stores. One DB per user, always version 1, single 'queue' store.
const DB_VERSION = 1;
const STORE_NAME = 'queue';

function dbNameForUser(userId) {
    return 'wildlifeAid_offlineQueue_' + userId;
}

function generateGuid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        const r = (Math.random() * 16) | 0;
        return (c === 'x' ? r : (r & 0x3) | 0x8).toString(16);
    });
}

function openDb(userId) {
    return new Promise((resolve, reject) => {
        const req = indexedDB.open(dbNameForUser(userId), DB_VERSION);

        req.onupgradeneeded = function (e) {
            e.target.result.createObjectStore(STORE_NAME, { keyPath: 'id' });
        };

        req.onsuccess = function (e) {
            resolve({ db: e.target.result, storeName: STORE_NAME });
        };

        req.onerror = (e) => reject(e.target.error);
    });
}

function txn(db, storeName, mode, fn) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, mode);
        const store = tx.objectStore(storeName);
        const req = fn(store);
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
}

function txnAll(db, storeName) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readonly');
        const req = tx.objectStore(storeName).getAll();
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
}

// ── Public API ────────────────────────────────────────────────────────────────

async function enqueueOfflineSighting(userId, item) {
    const { db, storeName } = await openDb(userId);
    const record = {
        id: generateGuid(),
        clientSightingId: generateGuid(),
        status: 'pending',
        serverId: null,
        failureReason: null,
        enqueuedAt: new Date().toISOString(),
        ...item
    };
    await txn(db, storeName, 'readwrite', (s) => s.put(record));
    db.close();
    return record;
}

async function getAllQueuedSightings(userId) {
    const { db, storeName } = await openDb(userId);
    const items = await txnAll(db, storeName);
    db.close();
    return items;
}

async function updateQueuedSighting(userId, id, patch) {
    const { db, storeName } = await openDb(userId);
    const existing = await txn(db, storeName, 'readonly', (s) => s.get(id));
    if (!existing) { db.close(); return; }
    const updated = { ...existing, ...patch };
    await txn(db, storeName, 'readwrite', (s) => s.put(updated));
    db.close();
}

async function deleteQueuedSighting(userId, id) {
    const { db, storeName } = await openDb(userId);
    await txn(db, storeName, 'readwrite', (s) => s.delete(id));
    db.close();
}

// ── Sync ──────────────────────────────────────────────────────────────────────

async function syncOfflineQueue(userId, antiForgeryToken) {
    const items = await getAllQueuedSightings(userId);
    const pending = items.filter(i => i.status === 'pending' || i.status === 'failed');

    for (const item of pending) {
        await updateQueuedSighting(userId, item.id, { status: 'syncing' });
        renderQueueUI(userId);

        try {
            const blob = dataUrlToBlob(item.imageDataUrl);
            const formData = new FormData();
            formData.append('Latitude', item.latitude);
            formData.append('Longitude', item.longitude);
            formData.append('Timestamp', item.timestamp);
            formData.append('DeviceTimezone', item.timezone);
            formData.append('SpeciesName', item.speciesName);
            formData.append('Description', item.description || '');
            formData.append('ClientSightingId', item.clientSightingId);
            formData.append('UploadedImage', blob, item.imageFileName || 'sighting.jpg');
            if (antiForgeryToken) {
                formData.append('__RequestVerificationToken', antiForgeryToken);
            }

            const response = await fetch('/Sighting/Upload', { method: 'POST', body: formData });

            if (response.ok || response.redirected) {
                await updateQueuedSighting(userId, item.id, { status: 'synced', failureReason: null });
            } else {
                const reason = `Server returned ${response.status}`;
                await updateQueuedSighting(userId, item.id, { status: 'failed', failureReason: reason });
            }
        } catch (err) {
            await updateQueuedSighting(userId, item.id, { status: 'failed', failureReason: err.message });
        }

        renderQueueUI(userId);
    }
}

function dataUrlToBlob(dataUrl) {
    const [header, data] = dataUrl.split(',');
    const mime = header.match(/:(.*?);/)[1];
    const binary = atob(data);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
    return new Blob([bytes], { type: mime });
}

// ── Queue UI renderer ─────────────────────────────────────────────────────────

async function renderQueueUI(userId) {
    const grid = document.getElementById('offlineQueueGrid');
    const empty = document.getElementById('offlineQueueEmpty');
    if (!grid) return;

    const items = await getAllQueuedSightings(userId);

    if (items.length === 0) {
        grid.innerHTML = '';
        if (empty) empty.classList.remove('d-none');
        return;
    }

    if (empty) empty.classList.add('d-none');

    grid.innerHTML = items.map(item => `
        <div class="queue-item-row card mb-2 p-3" data-client-id="${item.id}">
            <div class="d-flex justify-content-between align-items-start">
                <div>
                    <strong class="queue-item-species">${escapeHtml(item.speciesName || 'Unknown')}</strong>
                    <div class="text-muted small">${escapeHtml(item.timestamp || '')}</div>
                    ${item.description ? `<div class="text-muted small">${escapeHtml(item.description)}</div>` : ''}
                    ${item.failureReason ? `<div class="text-danger small">Error: ${escapeHtml(item.failureReason)}</div>` : ''}
                    ${item.serverId ? `<div class="text-success small">Synced (ID: ${escapeHtml(item.serverId)})</div>` : ''}
                </div>
                <div class="d-flex flex-column align-items-end gap-1">
                    <span class="badge queue-item-status ${statusBadgeClass(item.status)}">${escapeHtml(item.status)}</span>
                    ${item.status !== 'synced' ? `
                    <button class="btn btn-sm btn-outline-primary retryQueueItemBtn" data-id="${item.id}">Retry</button>
                    ` : ''}
                    <button class="btn btn-sm btn-outline-secondary editQueueItemBtn" data-id="${item.id}"
                        data-species="${escapeHtml(item.speciesName || '')}" data-description="${escapeHtml(item.description || '')}">Edit</button>
                    <button class="btn btn-sm btn-outline-danger deleteQueueItemBtn" data-id="${item.id}">Delete</button>
                </div>
            </div>
        </div>
    `).join('');

    attachQueueItemHandlers(userId);
}

function statusBadgeClass(status) {
    switch (status) {
        case 'pending': return 'bg-secondary';
        case 'syncing': return 'bg-info text-dark';
        case 'synced': return 'bg-success';
        case 'failed': return 'bg-danger';
        default: return 'bg-secondary';
    }
}

function escapeHtml(str) {
    const d = document.createElement('div');
    d.appendChild(document.createTextNode(str));
    return d.innerHTML;
}

function attachQueueItemHandlers(userId) {
    document.querySelectorAll('.deleteQueueItemBtn').forEach(btn => {
        btn.addEventListener('click', async function () {
            await deleteQueuedSighting(userId, this.dataset.id);
            renderQueueUI(userId);
        });
    });

    document.querySelectorAll('.retryQueueItemBtn').forEach(btn => {
        btn.addEventListener('click', async function () {
            await updateQueuedSighting(userId, this.dataset.id, { status: 'pending', failureReason: null });
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            syncOfflineQueue(userId, token);
        });
    });

    document.querySelectorAll('.editQueueItemBtn').forEach(btn => {
        btn.addEventListener('click', function () {
            const modal = document.getElementById('editQueueItemModal');
            if (!modal) return;
            modal.dataset.editId = this.dataset.id;
            const speciesInput = modal.querySelector('#editSpeciesInput');
            const descInput = modal.querySelector('#editDescriptionInput');
            if (speciesInput) speciesInput.value = this.dataset.species;
            if (descInput) descInput.value = this.dataset.description;
            const bsModal = bootstrap.Modal.getOrCreateInstance(modal);
            bsModal.show();
        });
    });
}

// ── Page init ─────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', function () {
    const userIdEl = document.getElementById('currentUserId');
    const userId = userIdEl ? userIdEl.textContent.trim() : null;

    // Offline queue page
    if (userId && document.getElementById('offlineQueueGrid')) {
        renderQueueUI(userId);

        const syncBtn = document.getElementById('syncNowBtn');
        if (syncBtn) {
            syncBtn.addEventListener('click', function () {
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                syncOfflineQueue(userId, token);
            });
        }

        // Save edit
        const saveEditBtn = document.getElementById('saveEditBtn');
        if (saveEditBtn) {
            saveEditBtn.addEventListener('click', async function () {
                const modal = document.getElementById('editQueueItemModal');
                if (!modal) return;
                const id = modal.dataset.editId;
                const species = modal.querySelector('#editSpeciesInput')?.value.trim();
                const description = modal.querySelector('#editDescriptionInput')?.value.trim();
                await updateQueuedSighting(userId, id, { speciesName: species, description });
                bootstrap.Modal.getInstance(modal)?.hide();
                renderQueueUI(userId);
            });
        }
    }

    // Sighting upload page: inject a clientSightingId if not already set
    const clientSightingIdInput = document.getElementById('clientSightingIdInput');
    if (clientSightingIdInput && !clientSightingIdInput.value) {
        clientSightingIdInput.value = generateGuid();
    }

});

// Auto-sync when connectivity is restored (applies on any page)
window.addEventListener('online', function () {
    const userIdEl = document.getElementById('currentUserId');
    const userId = userIdEl ? userIdEl.textContent.trim() : null;
    if (!userId) return;
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    syncOfflineQueue(userId, token);
});

// Expose key functions on window so Selenium acceptance tests can call them
// via ExecuteScript (e.g. to pre-seed the queue for non-offline scenarios).
if (typeof window !== 'undefined') {
    window.enqueueOfflineSighting = enqueueOfflineSighting;
    window.getAllQueuedSightings = getAllQueuedSightings;
}

// Export for Jest tests (Node/CommonJS environment)
if (typeof module !== 'undefined') {
    module.exports = { enqueueOfflineSighting, getAllQueuedSightings, updateQueuedSighting, deleteQueuedSighting, syncOfflineQueue, generateGuid };
}
