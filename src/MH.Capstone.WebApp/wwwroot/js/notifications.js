// ── Pure helpers ────────────────────────────────────────────────────────────

function isTableEmpty() {
    const table = document.querySelector('.notifications-table');
    const tbody = table && table.tBodies && table.tBodies[0];
    return !tbody || tbody.rows.length === 0;
}

function hasUnreadRows() {
    return document.querySelectorAll('.notification-row.unread').length > 0;
}

function applyMarkAllReadToDOM() {
    document.querySelectorAll('.notification-row.unread').forEach(row => {
        row.classList.remove('unread');
        const notifId = row.getAttribute('data-notif-id');
        const readBtn   = row.querySelector(`button[data-notif-id="${notifId}-read"]`);
        const unreadBtn = row.querySelector(`button[data-notif-id="${notifId}-unread"]`);
        if (readBtn)   readBtn.classList.add('d-none');
        if (unreadBtn) unreadBtn.classList.remove('d-none');
    });
}

function applyToggleReadToDOM(row, notifId) {
    if (!row) return;
    row.classList.toggle('unread');
    const readBtn   = row.querySelector(`button[data-notif-id="${notifId}-read"]`);
    const unreadBtn = row.querySelector(`button[data-notif-id="${notifId}-unread"]`);
    if (readBtn)   readBtn.classList.toggle('d-none');
    if (unreadBtn) unreadBtn.classList.toggle('d-none');
}

function fadeAndRemoveRow(row, onEmpty) {
    row.style.transition = 'opacity 220ms ease, height 220ms ease, margin 220ms ease';
    row.style.opacity = '0';
    row.style.height = '0';
    row.style.margin = '0';
    setTimeout(() => {
        row.remove();
        if (isTableEmpty()) onEmpty();
    }, 240);
}

// ── Async fetch wrappers (fetchFn injectable for testing) ───────────────────

async function submitMarkAllRead(form, fetchFn) {
    const token = form.querySelector('input[name="__RequestVerificationToken"]').value;
    return fetchFn(form.action, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token
        }
    });
}

async function submitDeleteAll(form, fetchFn) {
    const token = form.querySelector('input[name="__RequestVerificationToken"]').value;
    return fetchFn(form.action, {
        method: 'DELETE',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token
        }
    });
}

async function submitUpdateNotification(form, fetchFn) {
    const token          = form.querySelector('input[name="__RequestVerificationToken"]').value;
    const toggleReadValue = form.querySelector('input[name="toggleRead"]').value;
    return fetchFn(form.action, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token
        },
        body: new URLSearchParams({ toggleRead: toggleReadValue })
    });
}

async function submitDeleteNotification(form, fetchFn) {
    const token = form.querySelector('input[name="__RequestVerificationToken"]').value;
    return fetchFn(form.action, {
        method: 'DELETE',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token
        }
    });
}

// ── DOM wiring ──────────────────────────────────────────────────────────────

function initNotifications({ fetchFn, reloadFn, alertRefresh } = {}) {
    const _fetch   = fetchFn   || fetch;
    const _reload  = reloadFn  || (() => location.reload());
    const _refresh = alertRefresh || (() => {
        if (typeof notificationAlertRefresh === 'function') notificationAlertRefresh();
    });

    // Mark All as Read (bulk)
    const markAllReadForm = document.getElementById('markAllReadForm');
    if (markAllReadForm) {
        markAllReadForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            try {
                const res = await submitMarkAllRead(this, _fetch);
                if (!res.ok) { console.error('Failed to mark all notifications as read.', res.status); return; }
                applyMarkAllReadToDOM();
                this.classList.add('d-none');
                _refresh();
            } catch (err) {
                console.error('Error marking all as read:', err);
            }
        });
    }

    // Delete All (bulk)
    const deleteAllForm = document.getElementById('deleteAllForm');
    if (deleteAllForm) {
        deleteAllForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            try {
                const res = await submitDeleteAll(this, _fetch);
                if (!res.ok) { console.error('Failed to delete all notifications.', res.status); return; }
                _reload();
            } catch (err) {
                console.error('Error deleting all notifications:', err);
            }
        });
    }

    // Per-row toggle read
    document.querySelectorAll('.notificationUpdateForm').forEach(form => {
        form.addEventListener('submit', async function(e) {
            e.preventDefault();
            const notifId = this.getAttribute('data-notif-id');
            try {
                const res = await submitUpdateNotification(this, _fetch);
                if (res.ok) {
                    const row = document.querySelector(`tr[data-notif-id="${notifId}"]`);
                    applyToggleReadToDOM(row, notifId);
                    _refresh();
                } else {
                    console.error('Failed to update notification status.');
                }
            } catch (err) {
                console.error('Error:', err);
            }
        });
    });

    // Per-row delete
    document.querySelectorAll('.notificationDeleteForm').forEach(form => {
        form.addEventListener('submit', async function(e) {
            e.preventDefault();
            if (!confirm('Delete this notification? This action cannot be undone.')) return;

            const btn     = this.querySelector('.btn-delete');
            const notifId = this.getAttribute('data-notif-id');
            if (!notifId) { console.error('Notification id not found on delete form.'); return; }

            if (btn) { btn.disabled = true; btn.classList.add('disabled'); }

            try {
                const res = await submitDeleteNotification(this, _fetch);
                if (!res.ok) {
                    console.error('Failed to delete notification', res.status);
                    if (btn) { btn.disabled = false; btn.classList.remove('disabled'); }
                    return;
                }
                const row = document.querySelector(`tr[data-notif-id="${notifId}"]`);
                if (row) fadeAndRemoveRow(row, _reload);
                _refresh();
            } catch (err) {
                console.error('Error deleting notification:', err);
                if (btn) { btn.disabled = false; btn.classList.remove('disabled'); }
            }
        });
    });
}

// ── Browser entry point ─────────────────────────────────────────────────────

if (typeof document !== 'undefined') {
    document.addEventListener('DOMContentLoaded', () => initNotifications());
}

// ── Jest / Node exports ─────────────────────────────────────────────────────

if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        isTableEmpty,
        hasUnreadRows,
        applyMarkAllReadToDOM,
        applyToggleReadToDOM,
        fadeAndRemoveRow,
        submitMarkAllRead,
        submitDeleteAll,
        submitUpdateNotification,
        submitDeleteNotification,
        initNotifications
    };
}
