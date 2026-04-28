// ── Pure helpers ─────────────────────────────────────────────────────────────

/**
 * Returns an error message if the file exceeds maxSizeBytes, otherwise null.
 * @param {File|null} file
 * @param {number} maxSizeBytes
 * @returns {string|null}
 */
function validateProfileImageSize(file, maxSizeBytes) {
    if (!file) return null;
    return file.size > maxSizeBytes
        ? 'File size exceeds 2MB. Please choose a smaller image.'
        : null;
}

/**
 * Returns a validation error message for the bio value, or an empty string if valid.
 * Trims the value first to catch purely-whitespace inputs.
 * @param {string} bioValue
 * @param {number} [maxLength=250]
 * @returns {string}
 */
function validateBio(bioValue, maxLength = 250) {
    if (bioValue.trim().length > maxLength) {
        return `Bio is too long: cannot be over ${maxLength} characters.`;
    }
    return '';
}

/**
 * Returns the counter label string, e.g. "42/250".
 * @param {number} length
 * @param {number} [maxLength=250]
 * @returns {string}
 */
function getBioCounterText(length, maxLength = 250) {
    return `${length}/${maxLength}`;
}

/**
 * Returns true when the length has reached or passed the warning threshold.
 * @param {number} length
 * @param {number} [warningThreshold=240]
 * @returns {boolean}
 */
function isBioApproachingLimit(length, warningThreshold = 240) {
    return length >= warningThreshold;
}

// ── DOM mutators ─────────────────────────────────────────────────────────────

/**
 * Updates the character counter element to reflect the current bioArea length,
 * switching to a danger colour when the warning threshold is reached.
 * @param {HTMLElement} bioArea
 * @param {HTMLElement} counter
 * @param {number} [maxLength=250]
 * @param {number} [warningThreshold=240]
 */
function applyBioCounter(bioArea, counter, maxLength = 250, warningThreshold = 240) {
    if (!bioArea || !counter) return;
    const length = bioArea.value.length;
    counter.textContent = getBioCounterText(length, maxLength);
    if (isBioApproachingLimit(length, warningThreshold)) {
        counter.classList.remove('text-dark');
        counter.classList.add('text-danger');
    } else {
        counter.classList.add('text-dark');
        counter.classList.remove('text-danger');
    }
}

/**
 * Shows an error message in the given display element.
 * @param {HTMLElement|null} errorDisplay
 * @param {string} message
 */
function showBioError(errorDisplay, message) {
    if (!errorDisplay) return;
    errorDisplay.textContent = message;
    errorDisplay.style.display = 'block';
}

/**
 * Hides the error display element.
 * @param {HTMLElement|null} errorDisplay
 */
function hideBioError(errorDisplay) {
    if (!errorDisplay) return;
    errorDisplay.textContent = '';
    errorDisplay.style.display = 'none';
}

// ── DOM wiring ────────────────────────────────────────────────────────────────

/**
 * Wires up all Account Settings page interactivity.
 * @param {{ alertFn?: (msg: string) => void }} [opts]
 */
function initSettings({ alertFn } = {}) {
    const _alert = alertFn || ((msg) => alert(msg));

    // Profile image upload — client-side file size guard
    const uploadForm = document.getElementById('uploadForm');
    if (uploadForm) {
        uploadForm.addEventListener('submit', function (e) {
            const fileInput = document.getElementById('fileInput');
            if (fileInput && fileInput.files.length > 0) {
                const error = validateProfileImageSize(fileInput.files[0], 2 * 1024 * 1024);
                if (error) {
                    e.preventDefault();
                    _alert(error);
                }
            }
        });
    }

    // Bio character counter
    const bioArea = document.getElementById('bioInput');
    const counter = document.getElementById('charCount');
    if (bioArea && counter) {
        applyBioCounter(bioArea, counter);
        bioArea.addEventListener('input', () => applyBioCounter(bioArea, counter));
    }

    // Bio form — client-side length validation
    const bioForm = document.getElementById('bioForm');
    const errorDisplay = document.getElementById('bioErrorMsg');
    if (bioForm && bioArea) {
        bioForm.addEventListener('submit', function (event) {
            const errorMsg = validateBio(bioArea.value);
            if (errorMsg) {
                event.preventDefault();
                showBioError(errorDisplay, errorMsg);
            } else {
                hideBioError(errorDisplay);
            }
        });
    }
}

// ── Browser entry point ───────────────────────────────────────────────────────

if (typeof document !== 'undefined') {
    document.addEventListener('DOMContentLoaded', () => initSettings());
}

// ── Jest / Node exports ───────────────────────────────────────────────────────

if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        validateProfileImageSize,
        validateBio,
        getBioCounterText,
        isBioApproachingLimit,
        applyBioCounter,
        showBioError,
        hideBioError,
        initSettings,
    };
}
