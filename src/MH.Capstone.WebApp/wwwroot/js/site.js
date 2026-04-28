// The communal JS file...

/* Adding global script for detecting the user's IANA timezone.
        Saves it to a cookie for the server to use, populates 'deviceTimezone'
        input field if it exists on the page.
*/

function initUserTimezone() {
    const userTimezone = Intl.DateTimeFormat().resolvedOptions().timeZone;

    // Save to cookie (Expires in 1 year, accessible site-wide)
    document.cookie = "UserTimeZone=" + userTimezone + ";path=/;max-age=31536000;SameSite=Lax";

    // Auto-fill hidden input for the Sighting Upload form
    const tzInput = document.getElementById('deviceTimezone');
    if (tzInput) {
        tzInput.value = userTimezone;
    }
}

// For my sanity, we are adding a visibility toggle function for passwords
function togglePasswordVisibility(inputId, iconId) {
    const passwordInput = document.getElementById(inputId);
    const toggleIcon = document.getElementById(iconId);

    if (!passwordInput || !toggleIcon) return; // Exit, neither argument is present

    if (passwordInput && toggleIcon) {
        if (passwordInput.type === 'password') {
                passwordInput.type = 'text';
                toggleIcon.classList.replace('bi-eye', 'bi-eye-slash');
        } else {
            passwordInput.type = 'password';
            toggleIcon.classList.replace('bi-eye-slash', 'bi-eye');
        }
    }
}

function registerPasswordToggles() {
    // Returns an empty list if none are found
    const toggleButtons = document.querySelectorAll('[data-password-toggle]');

    toggleButtons.forEach(btn => {
        btn.addEventListener('click', function() {
            // Pull the IDs directly from the HTML attributes
            const inputId = this.getAttribute('data-target-input');
            const iconId = this.getAttribute('data-target-icon');
            togglePasswordVisibility(inputId, iconId);
        });
    });
}


document.addEventListener("DOMContentLoaded", function() {
    registerAllNumericInputs();  // Reworked from original global registration
    registerPasswordToggles();  // New global registration
    initUserTimezone();         // Gets timezone cookie from the user, for page display
});

// Thanks, ChatGPT, for the help with this function!
function registerAllNumericInputs() {
    const inputs = document.querySelectorAll("[data-js-numericOnly]");
    //console.log(inputs);

    inputs.forEach(input => {
        ensureNumericInput(input);
    });
}

// Thanks, ChatGPT, for the help with this function!
function ensureNumericInput(inputElm) {
    if (!inputElm) return;

    inputElm.addEventListener("input",
        function() {
            // Remove any character that is not a digit or decimal point
            let value = this.value.replace(/[^0-9.-]/g, "");

            // Allow only one decimal point
            const parts = value.split(".");
            if (parts.length > 2) {
                value = parts[0] + "." + parts.slice(1).join("");
            }

            this.value = value;
        }
    );
}

