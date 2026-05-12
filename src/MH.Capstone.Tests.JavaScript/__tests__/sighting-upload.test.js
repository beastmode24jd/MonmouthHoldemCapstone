// CSP-193: Geolocation auto-fill + permission-denied messaging on /Sighting/Upload.
const {
    isCoordEmpty,
    requestGeolocation,
    convertDMSToDecimal,
} = require('../../MH.Capstone.WebApp/wwwroot/js/sighting-upload');

function makeInputs() {
    document.body.innerHTML = `
        <input id="latitudeInput" />
        <input id="longitudeInput" />
        <div id="locationStatus" class="alert d-none"></div>
    `;
    return {
        latitudeInput:  document.getElementById('latitudeInput'),
        longitudeInput: document.getElementById('longitudeInput'),
        statusEl:       document.getElementById('locationStatus'),
    };
}

beforeEach(() => { document.body.innerHTML = ''; });

// ── isCoordEmpty ────────────────────────────────────────────────────────────

describe('isCoordEmpty', () => {
    test('returns true for null/undefined input', () => {
        expect(isCoordEmpty(null)).toBe(true);
        expect(isCoordEmpty(undefined)).toBe(true);
    });

    test('treats empty / whitespace / "0" as empty', () => {
        const { latitudeInput } = makeInputs();
        latitudeInput.value = '';     expect(isCoordEmpty(latitudeInput)).toBe(true);
        latitudeInput.value = '   ';  expect(isCoordEmpty(latitudeInput)).toBe(true);
        latitudeInput.value = '0';    expect(isCoordEmpty(latitudeInput)).toBe(true);
        latitudeInput.value = '0.0';  expect(isCoordEmpty(latitudeInput)).toBe(true);
    });

    test('treats non-zero numeric strings as non-empty', () => {
        const { latitudeInput } = makeInputs();
        latitudeInput.value = '44.123';   expect(isCoordEmpty(latitudeInput)).toBe(false);
        latitudeInput.value = '-122.34';  expect(isCoordEmpty(latitudeInput)).toBe(false);
    });
});

// ── convertDMSToDecimal ─────────────────────────────────────────────────────

describe('convertDMSToDecimal', () => {
    test('returns null for malformed DMS', () => {
        expect(convertDMSToDecimal(null, 'N')).toBeNull();
        expect(convertDMSToDecimal([1, 2], 'N')).toBeNull();
    });

    test('negates South / West', () => {
        expect(parseFloat(convertDMSToDecimal([10, 0, 0], 'S'))).toBeCloseTo(-10, 5);
        expect(parseFloat(convertDMSToDecimal([10, 0, 0], 'W'))).toBeCloseTo(-10, 5);
    });

    test('keeps North / East positive', () => {
        expect(parseFloat(convertDMSToDecimal([45, 30, 0], 'N'))).toBeCloseTo(45.5, 5);
        expect(parseFloat(convertDMSToDecimal([45, 30, 0], 'E'))).toBeCloseTo(45.5, 5);
    });
});

// ── requestGeolocation (autoMode = true, page-load behavior) ────────────────

describe('requestGeolocation (autoMode)', () => {
    test('populates lat/long when getCurrentPosition succeeds', () => {
        const { latitudeInput, longitudeInput, statusEl } = makeInputs();
        const geolocation = {
            getCurrentPosition: (onOk) => onOk({ coords: { latitude: 44.04, longitude: -123.07 } }),
        };

        requestGeolocation({ geolocation, latitudeInput, longitudeInput, statusEl, autoMode: true });

        expect(latitudeInput.value).toBe('44.04000');
        expect(longitudeInput.value).toBe('-123.07000');
        expect(statusEl.className).toContain('alert-success');
    });

    test('shows inline warning when permission is denied', () => {
        const { latitudeInput, longitudeInput, statusEl } = makeInputs();
        const geolocation = {
            getCurrentPosition: (_ok, onErr) => onErr({
                code: 1, PERMISSION_DENIED: 1, POSITION_UNAVAILABLE: 2, TIMEOUT: 3,
            }),
        };

        requestGeolocation({ geolocation, latitudeInput, longitudeInput, statusEl, autoMode: true });

        expect(latitudeInput.value).toBe('');
        expect(longitudeInput.value).toBe('');
        expect(statusEl.classList.contains('d-none')).toBe(false);
        expect(statusEl.className).toContain('alert-warning');
        expect(statusEl.textContent.toLowerCase()).toContain('manually');
    });

    test('stays silent on transient errors (timeout / unavailable) in autoMode', () => {
        const { latitudeInput, longitudeInput, statusEl } = makeInputs();
        const geolocation = {
            getCurrentPosition: (_ok, onErr) => onErr({
                code: 3, PERMISSION_DENIED: 1, POSITION_UNAVAILABLE: 2, TIMEOUT: 3,
            }),
        };

        requestGeolocation({ geolocation, latitudeInput, longitudeInput, statusEl, autoMode: true });

        expect(statusEl.classList.contains('d-none')).toBe(true);
    });

    test('does not overwrite values the user has already typed', () => {
        const { latitudeInput, longitudeInput, statusEl } = makeInputs();
        latitudeInput.value  = '40.0';
        longitudeInput.value = '-100.0';

        let called = false;
        const geolocation = {
            getCurrentPosition: () => { called = true; },
        };

        requestGeolocation({ geolocation, latitudeInput, longitudeInput, statusEl, autoMode: true });

        expect(called).toBe(false);
        expect(latitudeInput.value).toBe('40.0');
        expect(longitudeInput.value).toBe('-100.0');
    });

    test('no-ops silently when navigator.geolocation is unavailable in autoMode', () => {
        const { latitudeInput, longitudeInput, statusEl } = makeInputs();

        requestGeolocation({ geolocation: null, latitudeInput, longitudeInput, statusEl, autoMode: true });

        expect(statusEl.classList.contains('d-none')).toBe(true);
    });
});

// ── requestGeolocation (autoMode = false, manual button) ────────────────────

describe('requestGeolocation (manual button mode)', () => {
    test('shows the in-flight info banner before resolution', () => {
        const { latitudeInput, longitudeInput, statusEl } = makeInputs();
        const geolocation = { getCurrentPosition: () => {} };

        requestGeolocation({ geolocation, latitudeInput, longitudeInput, statusEl, autoMode: false });

        expect(statusEl.className).toContain('alert-info');
        expect(statusEl.textContent.toLowerCase()).toContain('getting your current location');
    });

    test('shows a danger banner when geolocation is unavailable', () => {
        const { latitudeInput, longitudeInput, statusEl } = makeInputs();

        requestGeolocation({ geolocation: null, latitudeInput, longitudeInput, statusEl, autoMode: false });

        expect(statusEl.className).toContain('alert-danger');
    });

    test('runs onSettled callback on permission-denied to re-enable the button', () => {
        const { latitudeInput, longitudeInput, statusEl } = makeInputs();
        const geolocation = {
            getCurrentPosition: (_ok, onErr) => onErr({
                code: 1, PERMISSION_DENIED: 1, POSITION_UNAVAILABLE: 2, TIMEOUT: 3,
            }),
        };

        let settled = false;
        requestGeolocation({
            geolocation, latitudeInput, longitudeInput, statusEl,
            autoMode: false, onSettled: () => { settled = true; },
        });

        expect(settled).toBe(true);
    });
});
