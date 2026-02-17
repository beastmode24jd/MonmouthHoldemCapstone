// Write your JavaScript code.

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