// wwwroot/js/manageAccounts.js

let activeFormId = '';

/**
 * On Manage.cshtml, there are: promotion, demotion, and account locking/opening forms.
 * This opens the password verification modal and stores which form triggered it.
 * @param {string} formId - The ID of the form to submit after verification.
 */

// showPasswordModal is global, so Manage.cshtml can call it.
function showPasswordModal(formId) {
    const form = document.getElementById(formId);
    
    if (formId === 'lockForm' || formId === 'unlockForm') {
        const emailInput = form.querySelector('.selected-email');
        const searchInput = form.querySelector('.user-search');
        
        // If no email is stored in the hidden field, the user hasn't selected a valid result
        if (!emailInput || !emailInput.value) {
            alert("Please select a user from the dropdown list before proceeding.");
            if (searchInput) searchInput.focus();
            return; // EXIT: Do not show the modal
        }
    }

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

document.querySelectorAll('.user-search').forEach(input => {
    input.addEventListener('input', async function() {
        const term = this.value;
        const findLocked = this.dataset.findLocked;
        const datalist = document.getElementById(this.getAttribute('list'));
        const hiddenEmailInput = this.closest('form').querySelector('.selected-email');

        if (term.length < 2) return;

        const response = await fetch(`/Admin/SearchUsers?term=${term}&findLocked=${findLocked}`);
        const users = await response.json();

        datalist.innerHTML = '';
        users.forEach(user => {
            let option = document.createElement('option');
            option.value = user.displayName;
            option.dataset.email = user.email;
            datalist.appendChild(option);
        });

        // If the input exactly matches a display name in our list, set the hidden email field
        // Ensure hidden input only has a value if the text exactly matches a result
        const match = users.find(u => u.displayName.toLowerCase() === term.toLowerCase());
        if (match) {
            hiddenEmailInput.value = match.email;
        } else {
            hiddenEmailInput.value = ''; // Clear it if they type something else
        }
    });
});