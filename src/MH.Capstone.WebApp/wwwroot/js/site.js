// The communal JS file...

document.getElementById('uploadForm')?.addEventListener("submit", function()
{
    const fileInput = document.getElementById('fileInput');
    if (fileInput.files.length > 0) {
        const fileSize = fileInput.files[0].size; // Size in bytes
        const maxSize = 2 * 1024 * 1024; // 2MB

        if (fileSize > maxSize)
        {
            alert('File size exceeds 2MB. Please choose a smaller image.');
            return false; // Prevents the form from submitting
        }
    }
});

// Provides front-end error messages for invalid text inputs for User Bio
// Used Gemini for catching mismatched id labels and capitalization.
document.addEventListener("DOMContentLoaded", function () {
    const bioForm = document.getElementById('bioForm');
    const bioInput = document.getElementById('bioInput');
    const errorDisplay = document.getElementById('bioErrorMsg');

    if (bioForm && bioInput) {
        bioForm.addEventListener('submit', function (event) {

            // uses .trim to detect purely whitespace input
            const bioValue = bioInput.value.trim();
            let errorMsg = "";

            if (bioValue.length === 0) {
                errorMsg = "Bio cannot be empty, or consist of only whitespace characters."
            }
            else if (bioValue.length > 250) {
                // This case would only be available if someone modified the front-end HTML attributes.
                // Leaving it here in case anyone does that.
                errorMsg = "Bio is too long: cannot be over 250 characters."
            }
            
            if (errorMsg !== "")
            {
                // Prevent the form from submitting to the server
                event.preventDefault();

                // Display the error message
                errorDisplay.textContent = errorMsg;
                errorDisplay.style.display = 'block';
            }

        });
    }

});

// Updates the user bio character counter.
document.addEventListener("DOMContentLoaded", function ()
{
    const bioArea = document.getElementById('bioInput');
    const counter = document.getElementById('charCount');
    const bioForm = document.getElementById('bioForm');

    function updateCounter() {
        const length = bioArea.value.length;
        counter.textContent = `${length}/250`;
                
        // Change text color if approaching submission limit
        if (length >= 240) {
            counter.classList.replace('text-dark', 'text-danger');
        } else {
            counter.classList.add('text-dark');
            counter.classList.remove('text-danger');
        }
    }

    // Initialize counter on page load
    // (User text doesn't auto-clear yet, will fix later)
    updateCounter();

    // Listen for user input
    bioArea.addEventListener('input', updateCounter);
});

document.addEventListener("DOMContentLoaded", registerAllNumericInputs);

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

