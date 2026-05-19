const { escapeHtml, formatTime, appendMessage } = require('../../MH.Capstone.WebApp/wwwroot/js/club-chat');

// ── DOM helpers ───────────────────────────────────────────────────────────────

function makeContainer() {
    const div = document.createElement('div');
    div.id = 'messageList';
    document.body.appendChild(div);
    return div;
}

const CURRENT_USER = 'user-abc';

const ownMsg = {
    authorId: CURRENT_USER,
    authorDisplayName: 'Alex',
    content: 'Hello there!',
    sentAtUtc: '2025-05-19T12:00:00Z',
};

const otherMsg = {
    authorId: 'user-xyz',
    authorDisplayName: 'Lily',
    content: 'Hi back!',
    sentAtUtc: '2025-05-19T12:01:00Z',
};

beforeEach(() => {
    document.body.innerHTML = '';
});

// ── escapeHtml ────────────────────────────────────────────────────────────────

describe('escapeHtml', () => {
    test('returns empty string for null', () => {
        expect(escapeHtml(null)).toBe('');
    });

    test('escapes < and >', () => {
        expect(escapeHtml('<script>')).toBe('&lt;script&gt;');
    });

    test('escapes &', () => {
        expect(escapeHtml('a&b')).toBe('a&amp;b');
    });

    test('does not encode double quotes (only <, >, & are encoded by textContent)', () => {
        expect(escapeHtml('"hi"')).toBe('"hi"');
    });

    test('passes through safe text unchanged', () => {
        expect(escapeHtml('Hello World')).toBe('Hello World');
    });

    test('handles undefined by treating it as a string', () => {
        expect(typeof escapeHtml(undefined)).toBe('string');
    });
});

// ── formatTime ────────────────────────────────────────────────────────────────

describe('formatTime', () => {
    test('returns a non-empty string for a valid ISO date', () => {
        const result = formatTime('2025-05-19T12:00:00Z');
        expect(result).toBeTruthy();
        expect(typeof result).toBe('string');
    });

    test('includes a time component with colon-separated digits', () => {
        const result = formatTime('2025-01-15T14:30:00Z');
        expect(result).toMatch(/\d{1,2}:\d{2}/);
    });

    test('includes a month abbreviation (three letters)', () => {
        const result = formatTime('2025-06-01T00:00:00Z');
        expect(result).toMatch(/[A-Za-z]{3}/);
    });

    test('includes AM or PM', () => {
        const result = formatTime('2025-03-10T08:00:00Z');
        expect(result).toMatch(/AM|PM/i);
    });
});

// ── appendMessage ─────────────────────────────────────────────────────────────

describe('appendMessage', () => {
    test('appends exactly one wrapper div to the container', () => {
        const container = makeContainer();
        appendMessage(container, CURRENT_USER, ownMsg);
        expect(container.children.length).toBe(1);
    });

    test('own message wrapper has justify-content-end class', () => {
        const container = makeContainer();
        appendMessage(container, CURRENT_USER, ownMsg);
        expect(container.firstElementChild.className).toContain('justify-content-end');
    });

    test('other-user message wrapper has justify-content-start class', () => {
        const container = makeContainer();
        appendMessage(container, CURRENT_USER, otherMsg);
        expect(container.firstElementChild.className).toContain('justify-content-start');
    });

    test('own message bubble has bg-primary class', () => {
        const container = makeContainer();
        appendMessage(container, CURRENT_USER, ownMsg);
        // container > wrapper > bubble; use firstElementChild twice to reach the bubble.
        const bubble = container.firstElementChild.firstElementChild;
        expect(bubble.className).toContain('bg-primary');
    });

    test('other-user message bubble has bg-light class', () => {
        const container = makeContainer();
        appendMessage(container, CURRENT_USER, otherMsg);
        const bubble = container.firstElementChild.firstElementChild;
        expect(bubble.className).toContain('bg-light');
    });

    test('other-user message shows author display name', () => {
        const container = makeContainer();
        appendMessage(container, CURRENT_USER, otherMsg);
        expect(container.innerHTML).toContain('Lily');
    });

    test('own message does not render an author name header', () => {
        const container = makeContainer();
        appendMessage(container, CURRENT_USER, ownMsg);
        expect(container.querySelector('.fw-semibold')).toBeNull();
    });

    test('message content is rendered in the bubble', () => {
        const container = makeContainer();
        appendMessage(container, CURRENT_USER, ownMsg);
        expect(container.innerHTML).toContain('Hello there!');
    });

    test('removes the empty-state element when it is present', () => {
        const container = makeContainer();
        const emptyState = document.createElement('div');
        emptyState.id = 'emptyMessagesState';
        document.body.appendChild(emptyState);

        appendMessage(container, CURRENT_USER, ownMsg);

        expect(document.getElementById('emptyMessagesState')).toBeNull();
    });

    test('does not throw when no empty-state element exists', () => {
        const container = makeContainer();
        expect(() => appendMessage(container, CURRENT_USER, ownMsg)).not.toThrow();
    });

    test('XSS in content is escaped — no raw script tag in DOM', () => {
        const container = makeContainer();
        appendMessage(container, CURRENT_USER, { ...ownMsg, content: '<script>alert(1)</script>' });
        expect(container.innerHTML).not.toContain('<script>');
        expect(container.innerHTML).toContain('&lt;script&gt;');
    });

    test('XSS in author display name is escaped', () => {
        const container = makeContainer();
        appendMessage(container, CURRENT_USER, { ...otherMsg, authorDisplayName: '<b>Evil</b>' });
        expect(container.innerHTML).not.toContain('<b>');
        expect(container.innerHTML).toContain('&lt;b&gt;');
    });

    test('two calls both append a wrapper', () => {
        const container = makeContainer();
        appendMessage(container, CURRENT_USER, ownMsg);
        appendMessage(container, CURRENT_USER, otherMsg);
        expect(container.children.length).toBe(2);
    });

    test('timestamp div has text-white-50 class for own message', () => {
        const container = makeContainer();
        appendMessage(container, CURRENT_USER, ownMsg);
        const timeDiv = container.querySelector('.text-white-50');
        expect(timeDiv).not.toBeNull();
    });

    test('timestamp div has text-muted class for other-user message', () => {
        const container = makeContainer();
        appendMessage(container, CURRENT_USER, otherMsg);
        const timeDiv = container.querySelector('.text-muted');
        expect(timeDiv).not.toBeNull();
    });
});
