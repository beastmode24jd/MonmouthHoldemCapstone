const {
    validateProfileImageSize,
    validateBio,
    getBioCounterText,
    isBioApproachingLimit,
    applyBioCounter,
    showBioError,
    hideBioError,
    initSettings,
} = require('../../MH.Capstone.WebApp/wwwroot/js/settings');

// ── DOM builders ──────────────────────────────────────────────────────────────

function makeSettingsPage({ bioValue = '', fileInputAttached = true } = {}) {
    document.body.innerHTML = `
        <form id="uploadForm" action="/dashboard/upload">
            <input id="fileInput" type="file" />
            <button type="submit">Upload</button>
        </form>

        <form id="bioForm" action="/dashboard/bio">
            <textarea id="bioInput">${bioValue}</textarea>
            <span id="charCount" class="form-text">0/250</span>
            <div id="bioErrorMsg" style="display:none;"></div>
            <button type="submit">Update Bio</button>
        </form>
    `;
}

function getBioElements() {
    return {
        bioArea: document.getElementById('bioInput'),
        counter: document.getElementById('charCount'),
        errorDisplay: document.getElementById('bioErrorMsg'),
        bioForm: document.getElementById('bioForm'),
    };
}

beforeEach(() => {
    document.body.innerHTML = '';
    jest.restoreAllMocks();
});

// ── validateProfileImageSize ──────────────────────────────────────────────────

describe('validateProfileImageSize', () => {
    const MAX = 2 * 1024 * 1024; // 2 MB

    test('returns null when file is null', () => {
        expect(validateProfileImageSize(null, MAX)).toBeNull();
    });

    test('returns null when file is exactly at the limit', () => {
        const file = { size: MAX };
        expect(validateProfileImageSize(file, MAX)).toBeNull();
    });

    test('returns null when file is under the limit', () => {
        const file = { size: MAX - 1 };
        expect(validateProfileImageSize(file, MAX)).toBeNull();
    });

    test('returns an error message when file exceeds the limit', () => {
        const file = { size: MAX + 1 };
        expect(validateProfileImageSize(file, MAX)).toBe(
            'File size exceeds 2MB. Please choose a smaller image.'
        );
    });

    test('respects a custom maxSizeBytes argument', () => {
        const file = { size: 500 };
        expect(validateProfileImageSize(file, 499)).not.toBeNull();
        expect(validateProfileImageSize(file, 500)).toBeNull();
    });
});

// ── validateBio ───────────────────────────────────────────────────────────────

describe('validateBio', () => {
    test('returns empty string for an empty bio', () => {
        expect(validateBio('')).toBe('');
    });

    test('returns empty string when bio is exactly 250 characters', () => {
        expect(validateBio('a'.repeat(250))).toBe('');
    });

    test('returns empty string when bio is under 250 characters', () => {
        expect(validateBio('hello')).toBe('');
    });

    test('returns an error message when bio exceeds 250 characters', () => {
        expect(validateBio('a'.repeat(251))).toContain('too long');
    });

    test('trims whitespace before checking length', () => {
        // 250 spaces trim to 0 — should be valid
        expect(validateBio(' '.repeat(250))).toBe('');
    });

    test('respects a custom maxLength argument', () => {
        expect(validateBio('hello', 3)).not.toBe('');
        expect(validateBio('hi', 3)).toBe('');
    });
});

// ── getBioCounterText ─────────────────────────────────────────────────────────

describe('getBioCounterText', () => {
    test('returns "0/250" for length 0 with default max', () => {
        expect(getBioCounterText(0)).toBe('0/250');
    });

    test('returns the correct label for a non-zero length', () => {
        expect(getBioCounterText(42)).toBe('42/250');
    });

    test('respects a custom maxLength argument', () => {
        expect(getBioCounterText(10, 100)).toBe('10/100');
    });
});

// ── isBioApproachingLimit ─────────────────────────────────────────────────────

describe('isBioApproachingLimit', () => {
    test('returns false when well under the threshold', () => {
        expect(isBioApproachingLimit(0)).toBe(false);
    });

    test('returns false one below the threshold', () => {
        expect(isBioApproachingLimit(239)).toBe(false);
    });

    test('returns true at the threshold', () => {
        expect(isBioApproachingLimit(240)).toBe(true);
    });

    test('returns true above the threshold', () => {
        expect(isBioApproachingLimit(250)).toBe(true);
    });

    test('respects a custom warningThreshold argument', () => {
        expect(isBioApproachingLimit(5, 10)).toBe(false);
        expect(isBioApproachingLimit(10, 10)).toBe(true);
    });
});

// ── applyBioCounter ───────────────────────────────────────────────────────────

describe('applyBioCounter', () => {
    test('does not throw when bioArea is null', () => {
        expect(() => applyBioCounter(null, document.createElement('span'))).not.toThrow();
    });

    test('does not throw when counter is null', () => {
        expect(() => applyBioCounter(document.createElement('textarea'), null)).not.toThrow();
    });

    test('sets counter text to "0/250" for an empty textarea', () => {
        makeSettingsPage();
        const { bioArea, counter } = getBioElements();
        applyBioCounter(bioArea, counter);
        expect(counter.textContent).toBe('0/250');
    });

    test('updates counter text to reflect the current value length', () => {
        makeSettingsPage({ bioValue: 'hello' });
        const { bioArea, counter } = getBioElements();
        applyBioCounter(bioArea, counter);
        expect(counter.textContent).toBe('5/250');
    });

    test('adds text-danger class when at warning threshold (240)', () => {
        makeSettingsPage({ bioValue: 'a'.repeat(240) });
        const { bioArea, counter } = getBioElements();
        applyBioCounter(bioArea, counter);
        expect(counter.classList.contains('text-danger')).toBe(true);
    });

    test('removes text-danger class when below warning threshold', () => {
        makeSettingsPage({ bioValue: 'a'.repeat(239) });
        const { bioArea, counter } = getBioElements();
        counter.classList.add('text-danger');
        applyBioCounter(bioArea, counter);
        expect(counter.classList.contains('text-danger')).toBe(false);
    });

    test('adds text-dark class when below warning threshold', () => {
        makeSettingsPage({ bioValue: 'hi' });
        const { bioArea, counter } = getBioElements();
        applyBioCounter(bioArea, counter);
        expect(counter.classList.contains('text-dark')).toBe(true);
    });
});

// ── showBioError / hideBioError ───────────────────────────────────────────────

describe('showBioError', () => {
    test('does not throw when errorDisplay is null', () => {
        expect(() => showBioError(null, 'oops')).not.toThrow();
    });

    test('sets the text content of the error display', () => {
        const el = document.createElement('div');
        showBioError(el, 'Too long!');
        expect(el.textContent).toBe('Too long!');
    });

    test('sets display to "block"', () => {
        const el = document.createElement('div');
        el.style.display = 'none';
        showBioError(el, 'Error');
        expect(el.style.display).toBe('block');
    });
});

describe('hideBioError', () => {
    test('does not throw when errorDisplay is null', () => {
        expect(() => hideBioError(null)).not.toThrow();
    });

    test('clears the text content', () => {
        const el = document.createElement('div');
        el.textContent = 'some error';
        hideBioError(el);
        expect(el.textContent).toBe('');
    });

    test('sets display to "none"', () => {
        const el = document.createElement('div');
        el.style.display = 'block';
        hideBioError(el);
        expect(el.style.display).toBe('none');
    });
});

// ── initSettings ─────────────────────────────────────────────────────────────

describe('initSettings — bio character counter', () => {
    test('sets counter text to the current bio length on init', () => {
        makeSettingsPage({ bioValue: 'hello' });
        initSettings();
        expect(document.getElementById('charCount').textContent).toBe('5/250');
    });

    test('updates counter text on input event', () => {
        makeSettingsPage({ bioValue: '' });
        initSettings();
        const bioArea = document.getElementById('bioInput');
        bioArea.value = 'typed text';
        bioArea.dispatchEvent(new Event('input'));
        expect(document.getElementById('charCount').textContent).toBe('10/250');
    });
});

describe('initSettings — bio form validation', () => {
    test('allows submission when bio is valid', () => {
        makeSettingsPage({ bioValue: 'Short bio' });
        initSettings();
        const bioForm = document.getElementById('bioForm');
        const event = new Event('submit', { cancelable: true });
        bioForm.dispatchEvent(event);
        expect(event.defaultPrevented).toBe(false);
    });

    test('prevents submission and shows error when bio is too long', () => {
        makeSettingsPage({ bioValue: 'a'.repeat(251) });
        initSettings();
        const bioForm = document.getElementById('bioForm');
        const event = new Event('submit', { cancelable: true });
        bioForm.dispatchEvent(event);
        expect(event.defaultPrevented).toBe(true);
        expect(document.getElementById('bioErrorMsg').style.display).toBe('block');
    });

    test('hides the error display when a previously-invalid bio becomes valid', () => {
        makeSettingsPage({ bioValue: 'a'.repeat(251) });
        initSettings();
        const bioArea = document.getElementById('bioInput');
        const bioForm = document.getElementById('bioForm');
        const errorDisplay = document.getElementById('bioErrorMsg');

        // First submit — invalid
        bioForm.dispatchEvent(new Event('submit', { cancelable: true }));
        expect(errorDisplay.style.display).toBe('block');

        // Fix the value and resubmit
        bioArea.value = 'valid bio';
        bioForm.dispatchEvent(new Event('submit', { cancelable: true }));
        expect(errorDisplay.style.display).toBe('none');
    });
});

describe('initSettings — profile image upload validation', () => {
    test('calls alertFn and prevents submission when file exceeds 2 MB', () => {
        makeSettingsPage();
        const alertFn = jest.fn();
        initSettings({ alertFn });

        const fileInput = document.getElementById('fileInput');
        Object.defineProperty(fileInput, 'files', {
            value: [{ size: 3 * 1024 * 1024 }],
            configurable: true,
        });

        const uploadForm = document.getElementById('uploadForm');
        const event = new Event('submit', { cancelable: true });
        uploadForm.dispatchEvent(event);

        expect(alertFn).toHaveBeenCalledTimes(1);
        expect(event.defaultPrevented).toBe(true);
    });

    test('does not alert when file is within the 2 MB limit', () => {
        makeSettingsPage();
        const alertFn = jest.fn();
        initSettings({ alertFn });

        const fileInput = document.getElementById('fileInput');
        Object.defineProperty(fileInput, 'files', {
            value: [{ size: 1 * 1024 * 1024 }],
            configurable: true,
        });

        const uploadForm = document.getElementById('uploadForm');
        uploadForm.dispatchEvent(new Event('submit', { cancelable: true }));

        expect(alertFn).not.toHaveBeenCalled();
    });

    test('does not alert when no file is selected', () => {
        makeSettingsPage();
        const alertFn = jest.fn();
        initSettings({ alertFn });

        const uploadForm = document.getElementById('uploadForm');
        uploadForm.dispatchEvent(new Event('submit', { cancelable: true }));

        expect(alertFn).not.toHaveBeenCalled();
    });
});

describe('initSettings — graceful no-op when elements are absent', () => {
    test('does not throw when the settings forms are not on the page', () => {
        document.body.innerHTML = '<p>some other page</p>';
        expect(() => initSettings()).not.toThrow();
    });
});
