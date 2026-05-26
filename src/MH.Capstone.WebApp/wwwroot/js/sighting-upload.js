// sighting-upload.js — Lat/long automation (page-load auto-fill, EXIF, manual button) + AI photo recognition.
//
// CSP-147: EXIF GPS extraction on photo upload.
// CSP-193: Auto-invoke navigator.geolocation on page load and inform the user inline if denied.

// CSP-177: intercept form submission when offline and save to IndexedDB queue instead.
// navigator.onLine cannot be overridden in Chrome (non-configurable property), so
// acceptance tests set window.__FORCE_OFFLINE = true to simulate offline state.
function isOffline() {
    return (typeof window.__FORCE_OFFLINE !== 'undefined' && window.__FORCE_OFFLINE) || !navigator.onLine;
}

(function () {
    document.addEventListener("DOMContentLoaded", function () {
        const form = document.getElementById("sightingUploadForm");
        const userIdEl = document.getElementById("currentUserId");
        if (!form) return;

        form.addEventListener("submit", async function (e) {
            if (!isOffline()) return; // normal path — let form submit to server

            e.preventDefault();

            const imageInput = document.getElementById("imageUploadInput");
            const file = imageInput && imageInput.files && imageInput.files[0];

            let imageDataUrl = null;
            let imageFileName = null;
            if (file) {
                imageDataUrl = await new Promise((resolve) => {
                    const reader = new FileReader();
                    reader.onload = (ev) => resolve(ev.target.result);
                    reader.readAsDataURL(file);
                });
                imageFileName = file.name;
            }

            const clientSightingIdInput = document.getElementById("clientSightingIdInput");
            const clientSightingId = clientSightingIdInput ? clientSightingIdInput.value : null;

            const userId = userIdEl ? userIdEl.textContent.trim() : null;

            if (!userId) {
                alert("Cannot save offline sighting: user session not found. Please log in again.");
                return;
            }

            await enqueueOfflineSighting(userId, {
                speciesName: (document.getElementById("SpeciesName") || {}).value || "",
                latitude: (document.getElementById("latitudeInput") || {}).value || "0",
                longitude: (document.getElementById("longitudeInput") || {}).value || "0",
                timestamp: form.querySelector("[name='Timestamp']")?.value || new Date().toISOString(),
                timezone: form.querySelector("[name='DeviceTimezone']")?.value || "America/Los_Angeles",
                description: form.querySelector("[name='Description']")?.value || "",
                imageDataUrl,
                imageFileName,
                clientSightingId
            });

            window.location.href = "/Sighting/OfflineQueue";
        });
    });
})();

// ── Pure helpers (top-level for Jest) ───────────────────────────────────────

function convertDMSToDecimal(dms, ref) {
    if (!dms || dms.length < 3) return null;
    const decimal = dms[0] + (dms[1] / 60) + (dms[2] / 3600);
    return (ref === "S" || ref === "W" ? -decimal : decimal).toFixed(5);
}

function isCoordEmpty(input) {
    if (!input) return true;
    const v = (input.value || "").trim();
    return v === "" || Number(v) === 0;
}

function setStatus(statusEl, message, type) {
    if (!statusEl) return;
    statusEl.textContent = message;
    statusEl.className = `alert alert-${type}`;
    statusEl.classList.remove("d-none");
}

function clearStatus(statusEl) {
    if (!statusEl) return;
    statusEl.classList.add("d-none");
}

// CSP-193: shared geolocation flow used by both the page-load auto-fill and the
// "Use Current Location" button. autoMode suppresses the in-flight info banner
// and stays silent for transient errors (timeout / position unavailable) so a
// user who never asked for location isn't greeted by a red banner; an explicit
// PERMISSION_DENIED still surfaces because the user just answered the prompt.
function requestGeolocation({
    geolocation,
    latitudeInput,
    longitudeInput,
    statusEl,
    autoMode = false,
    onSettled = () => {},
}) {
    if (!geolocation) {
        if (!autoMode) setStatus(statusEl, "Geolocation is not supported by your browser.", "danger");
        onSettled();
        return;
    }

    if (autoMode && (!isCoordEmpty(latitudeInput) || !isCoordEmpty(longitudeInput))) {
        // User (or EXIF) already filled in coordinates — don't overwrite them.
        onSettled();
        return;
    }

    if (!autoMode) setStatus(statusEl, "Getting your current location...", "warning");

    geolocation.getCurrentPosition(
        function (position) {
            if (latitudeInput)  latitudeInput.value  = position.coords.latitude.toFixed(5);
            if (longitudeInput) longitudeInput.value = position.coords.longitude.toFixed(5);
            setStatus(statusEl, "Current location retrieved successfully!", "success");
            onSettled();
        },
        function (error) {
            const codes = (typeof error === "object" && error) ? error : {};
            const PERMISSION_DENIED = codes.PERMISSION_DENIED ?? 1;
            const POSITION_UNAVAILABLE = codes.POSITION_UNAVAILABLE ?? 2;
            const TIMEOUT = codes.TIMEOUT ?? 3;

            if (error && error.code === PERMISSION_DENIED) {
                setStatus(
                    statusEl,
                    "Location permission was denied. Please enter coordinates manually.",
                    "warning"
                );
            } else if (!autoMode) {
                let message = "Could not get your location. ";
                switch (error && error.code) {
                    case POSITION_UNAVAILABLE: message += "Location information is unavailable."; break;
                    case TIMEOUT:              message += "Location request timed out.";          break;
                    default:                   message += "An unknown error occurred.";
                }
                setStatus(statusEl, message, "danger");
            }
            onSettled();
        },
        { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
    );
}

// CSP-147: Extract GPS from image EXIF data. Resolves with { latitude, longitude } strings (5 dp).
function extractGPSFromImage(file) {
    return new Promise(function (resolve, reject) {
        if (typeof EXIF === "undefined") { reject("EXIF library unavailable"); return; }
        EXIF.getData(file, function () {
            const lat    = EXIF.getTag(this, "GPSLatitude");
            const latRef = EXIF.getTag(this, "GPSLatitudeRef");
            const lon    = EXIF.getTag(this, "GPSLongitude");
            const lonRef = EXIF.getTag(this, "GPSLongitudeRef");
            if (!lat || !lon || !latRef || !lonRef) { reject("No GPS data found in image"); return; }
            const latitude  = convertDMSToDecimal(lat, latRef);
            const longitude = convertDMSToDecimal(lon, lonRef);
            if (latitude === null || longitude === null) { reject("Could not parse GPS coordinates"); return; }
            resolve({ latitude, longitude });
        });
    });
}

// ── Page wiring ─────────────────────────────────────────────────────────────

function initSightingUpload() {
    const imageInput            = document.getElementById("imageUploadInput");
    const latitudeInput         = document.getElementById("latitudeInput");
    const longitudeInput        = document.getElementById("longitudeInput");
    const statusEl              = document.getElementById("locationStatus");
    const useCurrentLocationBtn = document.getElementById("useCurrentLocationBtn");
    const imagePreview          = document.getElementById("imagePreview");

    // CSP-193: kick off auto-geolocation on page load (only fills empty fields).
    requestGeolocation({
        geolocation: (typeof navigator !== "undefined") ? navigator.geolocation : null,
        latitudeInput,
        longitudeInput,
        statusEl,
        autoMode: true,
    });

    // CSP-147: EXIF extraction + image preview on photo upload.
    if (imageInput) {
        imageInput.addEventListener("change", async function (e) {
            const file = e.target.files[0];
            if (!file) return;

            if (imagePreview) {
                const reader = new FileReader();
                reader.onload = function (ev) {
                    imagePreview.src = ev.target.result;
                    imagePreview.classList.remove("d-none");
                };
                reader.readAsDataURL(file);
            }

            try {
                setStatus(statusEl, "Extracting location from image...", "info");
                const coords = await extractGPSFromImage(file);
                latitudeInput.value  = coords.latitude;
                longitudeInput.value = coords.longitude;
                setStatus(statusEl, "Location extracted from image successfully!", "success");
            } catch (_err) {
                setStatus(
                    statusEl,
                    "Could not extract location from image. You can use your current location or enter coordinates manually.",
                    "warning"
                );
            }
        });
    }

    // Manual "Use Current Location" button (unchanged behavior — full info+error banners).
    if (useCurrentLocationBtn) {
        useCurrentLocationBtn.addEventListener("click", function () {
            useCurrentLocationBtn.disabled = true;
            requestGeolocation({
                geolocation: navigator.geolocation,
                latitudeInput,
                longitudeInput,
                statusEl,
                autoMode: false,
                onSettled: function () { useCurrentLocationBtn.disabled = false; },
            });
        });
    }

    // ── CSP-144: AI Photo Recognition ────────────────────────────────────────
    const identifyAIBtn         = document.getElementById("identifyAIBtn");
    const aiSuggestionBadge     = document.getElementById("aiSuggestionBadge");
    const aiSuggestionSpecies   = document.getElementById("aiSuggestionSpecies");
    const aiNotIdentifiedMessage = document.getElementById("aiNotIdentifiedMessage");
    const aiErrorToast          = document.getElementById("aiErrorToast");
    const aiErrorMessage        = document.getElementById("aiErrorMessage");
    const descriptionInput      = document.getElementById("Description");
    const speciesNameInput      = document.getElementById("SpeciesName"); // CSP-142

    function hideAllAiStatuses() {
        aiSuggestionBadge.classList.add("d-none");
        aiNotIdentifiedMessage.classList.add("d-none");
        aiErrorToast.classList.add("d-none");
    }

    if (identifyAIBtn && imageInput) {
        imageInput.addEventListener("change", function () {
            identifyAIBtn.disabled = !imageInput.files || imageInput.files.length === 0;
        });

        identifyAIBtn.addEventListener("click", async function () {
            const file = imageInput.files && imageInput.files[0];
            if (!file) return;

            hideAllAiStatuses();

            const originalText = identifyAIBtn.innerHTML;
            identifyAIBtn.disabled = true;
            identifyAIBtn.innerHTML =
                '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Identifying...';

            try {
                const formData = new FormData();
                formData.append("image", file);

                const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
                if (tokenInput) formData.append("__RequestVerificationToken", tokenInput.value);

                const response = await fetch("/SightingAI/Identify", { method: "POST", body: formData });

                if (!response.ok) {
                    const errBody = await response.json().catch(function () { return { message: "AI service error." }; });
                    aiErrorMessage.textContent = errBody.message || "AI service error.";
                    aiErrorToast.classList.remove("d-none");
                    return;
                }

                const data = await response.json();

                if (data.identified) {
                    aiSuggestionSpecies.textContent = data.species;
                    aiSuggestionBadge.classList.remove("d-none");
                    if (descriptionInput && data.description) descriptionInput.value = data.description;
                    if (speciesNameInput && data.species && !speciesNameInput.value.trim()) {
                        speciesNameInput.value = data.species;
                    }
                } else {
                    aiNotIdentifiedMessage.classList.remove("d-none");
                }
            } catch (_err) {
                aiErrorMessage.textContent =
                    "Could not reach the AI service. Please try again or describe the photo manually.";
                aiErrorToast.classList.remove("d-none");
            } finally {
                identifyAIBtn.innerHTML = originalText;
                identifyAIBtn.disabled = !imageInput.files || imageInput.files.length === 0;
            }
        });
    }
}

// ── Wiring + Jest exports ───────────────────────────────────────────────────

if (typeof document !== "undefined") {
    document.addEventListener("DOMContentLoaded", initSightingUpload);
}

if (typeof module !== "undefined" && module.exports) {
    module.exports = {
        convertDMSToDecimal,
        isCoordEmpty,
        setStatus,
        clearStatus,
        requestGeolocation,
        extractGPSFromImage,
        initSightingUpload,
    };
}
