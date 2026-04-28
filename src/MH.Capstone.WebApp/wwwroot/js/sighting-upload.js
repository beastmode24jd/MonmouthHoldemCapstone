// sighting-upload.js - Handles lat/long automation from photo EXIF or current location

document.addEventListener("DOMContentLoaded", function () {
    const imageInput = document.getElementById("imageUploadInput");
    const latitudeInput = document.getElementById("latitudeInput");
    const longitudeInput = document.getElementById("longitudeInput");
    const locationStatus = document.getElementById("locationStatus");
    const useCurrentLocationBtn = document.getElementById("useCurrentLocationBtn");
    const imagePreview = document.getElementById("imagePreview");

    // Show status message
    function showStatus(message, type) {
        locationStatus.textContent = message;
        locationStatus.className = `alert alert-${type}`;
        locationStatus.classList.remove("d-none");
    }

    // Hide status message
    function hideStatus() {
        locationStatus.classList.add("d-none");
    }

    // Convert EXIF GPS coordinates to decimal degrees
    function convertDMSToDecimal(dms, ref) {
        if (!dms || dms.length < 3) return null;
        
        const degrees = dms[0];
        const minutes = dms[1];
        const seconds = dms[2];
        
        let decimal = degrees + (minutes / 60) + (seconds / 3600);
        
        // South and West are negative
        if (ref === "S" || ref === "W") {
            decimal = -decimal;
        }
        
        return decimal.toFixed(5);
    }

    // Extract GPS from image EXIF data
    function extractGPSFromImage(file) {
        return new Promise((resolve, reject) => {
            EXIF.getData(file, function () {
                const lat = EXIF.getTag(this, "GPSLatitude");
                const latRef = EXIF.getTag(this, "GPSLatitudeRef");
                const lon = EXIF.getTag(this, "GPSLongitude");
                const lonRef = EXIF.getTag(this, "GPSLongitudeRef");

                if (lat && lon && latRef && lonRef) {
                    const latitude = convertDMSToDecimal(lat, latRef);
                    const longitude = convertDMSToDecimal(lon, lonRef);
                    
                    if (latitude !== null && longitude !== null) {
                        resolve({ latitude, longitude });
                    } else {
                        reject("Could not parse GPS coordinates");
                    }
                } else {
                    reject("No GPS data found in image");
                }
            });
        });
    }

    // Handle image upload
    if (imageInput) {
        imageInput.addEventListener("change", async function (e) {
            const file = e.target.files[0];
            if (!file) return;

            // Show image preview
            if (imagePreview) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    imagePreview.src = e.target.result;
                    imagePreview.classList.remove("d-none");
                };
                reader.readAsDataURL(file);
            }

            // Try to extract GPS from EXIF
            try {
                showStatus("Extracting location from image...", "info");
                const coords = await extractGPSFromImage(file);
                
                latitudeInput.value = coords.latitude;
                longitudeInput.value = coords.longitude;
                
                showStatus("Location extracted from image successfully!", "success");
            } catch (error) {
                showStatus(
                    "Could not extract location from image. You can use your current location or enter coordinates manually.",
                    "warning"
                );
            }
        });
    }

    // Handle "Use Current Location" button
    if (useCurrentLocationBtn) {
        useCurrentLocationBtn.addEventListener("click", function () {
            if (!navigator.geolocation) {
                showStatus("Geolocation is not supported by your browser.", "danger");
                return;
            }

            showStatus("Getting your current location...", "info");
            useCurrentLocationBtn.disabled = true;

            navigator.geolocation.getCurrentPosition(
                function (position) {
                    latitudeInput.value = position.coords.latitude.toFixed(5);
                    longitudeInput.value = position.coords.longitude.toFixed(5);
                    
                    showStatus("Current location retrieved successfully!", "success");
                    useCurrentLocationBtn.disabled = false;
                },
                function (error) {
                    let message = "Could not get your location. ";
                    
                    switch (error.code) {
                        case error.PERMISSION_DENIED:
                            message += "Location permission was denied. Please allow location access or enter coordinates manually.";
                            break;
                        case error.POSITION_UNAVAILABLE:
                            message += "Location information is unavailable.";
                            break;
                        case error.TIMEOUT:
                            message += "Location request timed out.";
                            break;
                        default:
                            message += "An unknown error occurred.";
                    }
                    
                    showStatus(message, "danger");
                    useCurrentLocationBtn.disabled = false;
                },
                {
                    enableHighAccuracy: true,
                    timeout: 10000,
                    maximumAge: 0
                }
            );
        });
    }

    // ── CSP-144: AI Photo Recognition ────────────────────────────────────────
    const identifyAIBtn = document.getElementById("identifyAIBtn");
    const aiSuggestionBadge = document.getElementById("aiSuggestionBadge");
    const aiSuggestionSpecies = document.getElementById("aiSuggestionSpecies");
    const aiNotIdentifiedMessage = document.getElementById("aiNotIdentifiedMessage");
    const aiErrorToast = document.getElementById("aiErrorToast");
    const aiErrorMessage = document.getElementById("aiErrorMessage");
    const descriptionInput = document.getElementById("Description");

    function hideAllAiStatuses() {
        aiSuggestionBadge.classList.add("d-none");
        aiNotIdentifiedMessage.classList.add("d-none");
        aiErrorToast.classList.add("d-none");
    }

    if (identifyAIBtn && imageInput) {
        // Enable the button only when a file has been chosen
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
                if (tokenInput) {
                    formData.append("__RequestVerificationToken", tokenInput.value);
                }

                const response = await fetch("/SightingAI/Identify", {
                    method: "POST",
                    body: formData
                });

                if (!response.ok) {
                    const errBody = await response.json().catch(function () {
                        return { message: "AI service error." };
                    });
                    aiErrorMessage.textContent = errBody.message || "AI service error.";
                    aiErrorToast.classList.remove("d-none");
                    return;
                }

                const data = await response.json();

                if (data.identified) {
                    aiSuggestionSpecies.textContent = data.species;
                    aiSuggestionBadge.classList.remove("d-none");
                    if (descriptionInput && data.description) {
                        descriptionInput.value = data.description;
                    }
                } else {
                    aiNotIdentifiedMessage.classList.remove("d-none");
                }
            } catch (err) {
                aiErrorMessage.textContent =
                    "Could not reach the AI service. Please try again or describe the photo manually.";
                aiErrorToast.classList.remove("d-none");
            } finally {
                identifyAIBtn.innerHTML = originalText;
                identifyAIBtn.disabled = !imageInput.files || imageInput.files.length === 0;
            }
        });
    }
});
