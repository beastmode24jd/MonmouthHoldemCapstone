// wwwroot/js/adminModal.js
// Credit to gemini for helping me check tags and attribute calls

let activeFormId = '';

/**
 * On Manage.cshtml, there are: promotion, demotion, and deactivation forms.
 * This opens the password verification modal and stores which form triggered it.
 * @param {string} formId - The ID of the form to submit after verification.
 */

// showPasswordModal is global, so Manage.cshtml can call it.
function showPasswordModal(formId) {
    activeFormId = formId;
    
    // Clear password field from any previous attempts
    const passwordInput = document.getElementById('modalAdminPassword');
    if (passwordInput)
    {
        passwordInput.value = '';
    }

    // Initialize and show Manage.cshtml Bootstrap Modal
    const modalElement = document.getElementById('adminPasswordModal');
    const modal = new bootstrap.Modal(modalElement);
    modal.show();
}

// Attach event listener to the confirmation button, once the DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    const confirmBtn = document.getElementById('confirmAuthBtn');

    if (confirmBtn) {
        confirmBtn.addEventListener('click', function () {
            
            // Get both the form, and the password input
            const passwordValue = document.getElementById('modalAdminPassword').value;
            const form = document.getElementById(activeFormId);

            if (!passwordValue)
            {
                // User has tried to submit an empty string.
                alert("Administrator password is required to confirm this action.");
                return;
            }

            if (form)
            {
                // Find the hidden password field within the specific form
                const hiddenField = form.querySelector('.modal-password-target');

                if (hiddenField)
                {
                    hiddenField.value = passwordValue;
                    form.submit();
                } else {
                    console.error("Could not find hidden password field in form:", activeFormId);
                }
            }
        });
    }
});