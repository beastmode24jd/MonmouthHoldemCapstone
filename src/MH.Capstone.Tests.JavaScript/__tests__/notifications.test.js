const {
    isTableEmpty,
    hasUnreadRows,
    applyMarkAllReadToDOM,
    applyToggleReadToDOM,
    fadeAndRemoveRow,
    submitMarkAllRead,
    submitDeleteAll,
    submitUpdateNotification,
    submitDeleteNotification,
} = require('../../MH.Capstone.WebApp/wwwroot/js/notifications');

// ── DOM builders ─────────────────────────────────────────────────────────────

/**
 * Builds a single notification <tr>.
 * The button pattern mirrors Notifications.cshtml:
 *   - read button  (d-none when already read)
 *   - unread button (d-none when already unread)
 */
function makeRow(notifId, isRead = true) {
    return `
        <tr class="notification-row ${isRead ? '' : 'unread'}" data-notif-id="${notifId}">
            <td>
                <button class="${isRead ? 'd-none' : ''}"  data-notif-id="${notifId}-read"></button>
                <button class="${isRead ? '' : 'd-none'}" data-notif-id="${notifId}-unread"></button>
            </td>
        </tr>`;
}

function makeTable(rows = []) {
    return `
        <table class="notifications-table">
            <tbody>${rows.join('')}</tbody>
        </table>`;
}

/** Builds a form that mirrors the CSRF token pattern used in Notifications.cshtml. */
function makeAntiForgeryForm(action, extraInputs = '') {
    return `
        <form action="${action}">
            <input name="__RequestVerificationToken" value="test-token" />
            ${extraInputs}
        </form>`;
}

// ── Reset DOM between tests ───────────────────────────────────────────────────

beforeEach(() => {
    document.body.innerHTML = '';
});

// ── isTableEmpty ──────────────────────────────────────────────────────────────

describe('isTableEmpty', () => {
    test('returns true when no table exists in the DOM', () => {
        expect(isTableEmpty()).toBe(true);
    });

    test('returns true when table has no tbody element', () => {
        document.body.innerHTML = '<table class="notifications-table"></table>';
        expect(isTableEmpty()).toBe(true);
    });

    test('returns true when tbody exists but has no rows', () => {
        document.body.innerHTML = makeTable();
        expect(isTableEmpty()).toBe(true);
    });

    test('returns false when tbody has at least one row', () => {
        document.body.innerHTML = makeTable([makeRow('n1')]);
        expect(isTableEmpty()).toBe(false);
    });
});

// ── hasUnreadRows ─────────────────────────────────────────────────────────────

describe('hasUnreadRows', () => {
    test('returns false when there are no rows at all', () => {
        expect(hasUnreadRows()).toBe(false);
    });

    test('returns false when all rows are read', () => {
        document.body.innerHTML = makeTable([makeRow('n1', true), makeRow('n2', true)]);
        expect(hasUnreadRows()).toBe(false);
    });

    test('returns true when at least one row is unread', () => {
        document.body.innerHTML = makeTable([makeRow('n1', true), makeRow('n2', false)]);
        expect(hasUnreadRows()).toBe(true);
    });
});

// ── applyMarkAllReadToDOM ─────────────────────────────────────────────────────

describe('applyMarkAllReadToDOM', () => {
    test('does not throw when DOM is empty', () => {
        expect(() => applyMarkAllReadToDOM()).not.toThrow();
    });

    test('removes unread class from every unread row', () => {
        document.body.innerHTML = makeTable([makeRow('n1', false), makeRow('n2', false)]);
        applyMarkAllReadToDOM();
        expect(document.querySelectorAll('.notification-row.unread').length).toBe(0);
    });

    test('does not affect rows that are already read', () => {
        document.body.innerHTML = makeTable([makeRow('n1', true)]);
        applyMarkAllReadToDOM();
        expect(document.querySelector('.notification-row').classList.contains('unread')).toBe(false);
    });

    test('hides the read button (adds d-none) after marking read', () => {
        document.body.innerHTML = makeTable([makeRow('n1', false)]);
        applyMarkAllReadToDOM();
        expect(document.querySelector('[data-notif-id="n1-read"]').classList.contains('d-none')).toBe(true);
    });

    test('reveals the unread button (removes d-none) after marking read', () => {
        document.body.innerHTML = makeTable([makeRow('n1', false)]);
        applyMarkAllReadToDOM();
        expect(document.querySelector('[data-notif-id="n1-unread"]').classList.contains('d-none')).toBe(false);
    });
});

// ── applyToggleReadToDOM ──────────────────────────────────────────────────────

describe('applyToggleReadToDOM', () => {
    test('does not throw when row is null', () => {
        expect(() => applyToggleReadToDOM(null, 'n1')).not.toThrow();
    });

    test('adds unread class when row is currently read', () => {
        document.body.innerHTML = makeTable([makeRow('n1', true)]);
        const row = document.querySelector('tr[data-notif-id="n1"]');
        applyToggleReadToDOM(row, 'n1');
        expect(row.classList.contains('unread')).toBe(true);
    });

    test('removes unread class when row is currently unread', () => {
        document.body.innerHTML = makeTable([makeRow('n1', false)]);
        const row = document.querySelector('tr[data-notif-id="n1"]');
        applyToggleReadToDOM(row, 'n1');
        expect(row.classList.contains('unread')).toBe(false);
    });

    test('reveals read button when toggling a read row to unread', () => {
        document.body.innerHTML = makeTable([makeRow('n1', true)]);
        const row = document.querySelector('tr[data-notif-id="n1"]');
        applyToggleReadToDOM(row, 'n1');
        // Originally read → read button had d-none; after toggle (now unread) it should NOT have d-none
        expect(document.querySelector('[data-notif-id="n1-read"]').classList.contains('d-none')).toBe(false);
    });

    test('hides unread button when toggling a read row to unread', () => {
        document.body.innerHTML = makeTable([makeRow('n1', true)]);
        const row = document.querySelector('tr[data-notif-id="n1"]');
        applyToggleReadToDOM(row, 'n1');
        expect(document.querySelector('[data-notif-id="n1-unread"]').classList.contains('d-none')).toBe(true);
    });

    test('two successive toggles return row to its original state', () => {
        document.body.innerHTML = makeTable([makeRow('n1', true)]);
        const row = document.querySelector('tr[data-notif-id="n1"]');
        applyToggleReadToDOM(row, 'n1');
        applyToggleReadToDOM(row, 'n1');
        expect(row.classList.contains('unread')).toBe(false);
    });
});

// ── fadeAndRemoveRow ──────────────────────────────────────────────────────────

describe('fadeAndRemoveRow', () => {
    beforeEach(() => { jest.useFakeTimers(); });
    afterEach(() => { jest.useRealTimers(); });

    test('sets opacity to 0 immediately', () => {
        document.body.innerHTML = makeTable([makeRow('n1')]);
        const row = document.querySelector('tr');
        fadeAndRemoveRow(row, jest.fn());
        expect(row.style.opacity).toBe('0');
    });

    test('sets height to 0 immediately', () => {
        document.body.innerHTML = makeTable([makeRow('n1')]);
        const row = document.querySelector('tr');
        fadeAndRemoveRow(row, jest.fn());
        expect(row.style.height).toBe('0');
    });

    test('sets margin to 0 immediately', () => {
        document.body.innerHTML = makeTable([makeRow('n1')]);
        const row = document.querySelector('tr');
        fadeAndRemoveRow(row, jest.fn());
        expect(row.style.margin).toBe('0');
    });

    test('removes the row from the DOM after 240 ms', () => {
        document.body.innerHTML = makeTable([makeRow('n1')]);
        const row = document.querySelector('tr[data-notif-id="n1"]');
        fadeAndRemoveRow(row, jest.fn());
        jest.advanceTimersByTime(240);
        expect(document.querySelector('tr[data-notif-id="n1"]')).toBeNull();
    });

    test('row is still in DOM before 240 ms elapses', () => {
        document.body.innerHTML = makeTable([makeRow('n1')]);
        const row = document.querySelector('tr[data-notif-id="n1"]');
        fadeAndRemoveRow(row, jest.fn());
        jest.advanceTimersByTime(239);
        expect(document.querySelector('tr[data-notif-id="n1"]')).not.toBeNull();
    });

    test('calls onEmpty when the table is empty after removal', () => {
        document.body.innerHTML = makeTable([makeRow('n1')]);
        const onEmpty = jest.fn();
        fadeAndRemoveRow(document.querySelector('tr'), onEmpty);
        jest.advanceTimersByTime(240);
        expect(onEmpty).toHaveBeenCalledTimes(1);
    });

    test('does not call onEmpty when other rows remain', () => {
        document.body.innerHTML = makeTable([makeRow('n1'), makeRow('n2')]);
        const onEmpty = jest.fn();
        fadeAndRemoveRow(document.querySelector('tr[data-notif-id="n1"]'), onEmpty);
        jest.advanceTimersByTime(240);
        expect(onEmpty).not.toHaveBeenCalled();
    });
});

// ── submitMarkAllRead ─────────────────────────────────────────────────────────

describe('submitMarkAllRead', () => {
    test('calls fetch with PUT method', async () => {
        const fetchFn = jest.fn().mockResolvedValue({ ok: true });
        document.body.innerHTML = makeAntiForgeryForm('/notifications/mark-all-read');
        await submitMarkAllRead(document.querySelector('form'), fetchFn);
        expect(fetchFn.mock.calls[0][1].method).toBe('PUT');
    });

    test('calls fetch with the form action URL', async () => {
        const fetchFn = jest.fn().mockResolvedValue({ ok: true });
        document.body.innerHTML = makeAntiForgeryForm('/notifications/mark-all-read');
        await submitMarkAllRead(document.querySelector('form'), fetchFn);
        expect(fetchFn.mock.calls[0][0]).toBe('/notifications/mark-all-read');
    });

    test('sends the CSRF token in the RequestVerificationToken header', async () => {
        const fetchFn = jest.fn().mockResolvedValue({ ok: true });
        document.body.innerHTML = makeAntiForgeryForm('/notifications/mark-all-read');
        await submitMarkAllRead(document.querySelector('form'), fetchFn);
        expect(fetchFn.mock.calls[0][1].headers['RequestVerificationToken']).toBe('test-token');
    });

    test('returns the fetch response', async () => {
        const fakeResponse = { ok: true, status: 200 };
        const fetchFn = jest.fn().mockResolvedValue(fakeResponse);
        document.body.innerHTML = makeAntiForgeryForm('/notifications/mark-all-read');
        const result = await submitMarkAllRead(document.querySelector('form'), fetchFn);
        expect(result).toBe(fakeResponse);
    });
});

// ── submitDeleteAll ───────────────────────────────────────────────────────────

describe('submitDeleteAll', () => {
    test('calls fetch with DELETE method', async () => {
        const fetchFn = jest.fn().mockResolvedValue({ ok: true });
        document.body.innerHTML = makeAntiForgeryForm('/notifications/all');
        await submitDeleteAll(document.querySelector('form'), fetchFn);
        expect(fetchFn.mock.calls[0][1].method).toBe('DELETE');
    });

    test('calls fetch with the form action URL', async () => {
        const fetchFn = jest.fn().mockResolvedValue({ ok: true });
        document.body.innerHTML = makeAntiForgeryForm('/notifications/all');
        await submitDeleteAll(document.querySelector('form'), fetchFn);
        expect(fetchFn.mock.calls[0][0]).toBe('/notifications/all');
    });

    test('sends the CSRF token in the RequestVerificationToken header', async () => {
        const fetchFn = jest.fn().mockResolvedValue({ ok: true });
        document.body.innerHTML = makeAntiForgeryForm('/notifications/all');
        await submitDeleteAll(document.querySelector('form'), fetchFn);
        expect(fetchFn.mock.calls[0][1].headers['RequestVerificationToken']).toBe('test-token');
    });
});

// ── submitUpdateNotification ──────────────────────────────────────────────────

describe('submitUpdateNotification', () => {
    test('calls fetch with PUT method', async () => {
        const fetchFn = jest.fn().mockResolvedValue({ ok: true });
        document.body.innerHTML = makeAntiForgeryForm('/notifications/abc/update', '<input name="toggleRead" value="true" />');
        await submitUpdateNotification(document.querySelector('form'), fetchFn);
        expect(fetchFn.mock.calls[0][1].method).toBe('PUT');
    });

    test('calls fetch with the form action URL', async () => {
        const fetchFn = jest.fn().mockResolvedValue({ ok: true });
        document.body.innerHTML = makeAntiForgeryForm('/notifications/abc/update', '<input name="toggleRead" value="true" />');
        await submitUpdateNotification(document.querySelector('form'), fetchFn);
        expect(fetchFn.mock.calls[0][0]).toBe('/notifications/abc/update');
    });

    test('sends the toggleRead value in the request body', async () => {
        const fetchFn = jest.fn().mockResolvedValue({ ok: true });
        document.body.innerHTML = makeAntiForgeryForm('/notifications/abc/update', '<input name="toggleRead" value="true" />');
        await submitUpdateNotification(document.querySelector('form'), fetchFn);
        expect(fetchFn.mock.calls[0][1].body.toString()).toContain('toggleRead=true');
    });

    test('sends the CSRF token in the RequestVerificationToken header', async () => {
        const fetchFn = jest.fn().mockResolvedValue({ ok: true });
        document.body.innerHTML = makeAntiForgeryForm('/notifications/abc/update', '<input name="toggleRead" value="true" />');
        await submitUpdateNotification(document.querySelector('form'), fetchFn);
        expect(fetchFn.mock.calls[0][1].headers['RequestVerificationToken']).toBe('test-token');
    });
});

// ── submitDeleteNotification ──────────────────────────────────────────────────

describe('submitDeleteNotification', () => {
    test('calls fetch with DELETE method', async () => {
        const fetchFn = jest.fn().mockResolvedValue({ ok: true });
        document.body.innerHTML = makeAntiForgeryForm('/notifications/abc');
        await submitDeleteNotification(document.querySelector('form'), fetchFn);
        expect(fetchFn.mock.calls[0][1].method).toBe('DELETE');
    });

    test('calls fetch with the form action URL', async () => {
        const fetchFn = jest.fn().mockResolvedValue({ ok: true });
        document.body.innerHTML = makeAntiForgeryForm('/notifications/abc');
        await submitDeleteNotification(document.querySelector('form'), fetchFn);
        expect(fetchFn.mock.calls[0][0]).toBe('/notifications/abc');
    });

    test('sends the CSRF token in the RequestVerificationToken header', async () => {
        const fetchFn = jest.fn().mockResolvedValue({ ok: true });
        document.body.innerHTML = makeAntiForgeryForm('/notifications/abc');
        await submitDeleteNotification(document.querySelector('form'), fetchFn);
        expect(fetchFn.mock.calls[0][1].headers['RequestVerificationToken']).toBe('test-token');
    });

    test('returns the fetch response', async () => {
        const fakeResponse = { ok: false, status: 404 };
        const fetchFn = jest.fn().mockResolvedValue(fakeResponse);
        document.body.innerHTML = makeAntiForgeryForm('/notifications/abc');
        const result = await submitDeleteNotification(document.querySelector('form'), fetchFn);
        expect(result).toBe(fakeResponse);
    });
});
