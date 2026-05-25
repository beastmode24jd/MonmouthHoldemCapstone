// wwwroot/js/manageAccounts.js

let activeFormId = '';

/**
 * On Manage.cshtml, there are: promotion, demotion, and account locking/opening forms.
 * This opens the password verification modal and stores which form triggered it.
 * @param {string} formId - The ID of the form to submit after verification.
 */
function showPasswordModal(formId) {
    const form = document.getElementById(formId);

    // 1. Clear password and details fields from any previous attempts (Declared ONCE here)
    const passInput = document.getElementById('modalAdminPassword');
    if (passInput) {
        passInput.value = '';
    }
    
    const detailsInput = document.getElementById('modalAuditDetails');
    if (detailsInput) {
        detailsInput.value = ''; 
    }
    
    // 2. Form specific validation
    if (formId === 'lockForm' || formId === 'unlockForm') {
        const emailInput = form.querySelector('.selected-email');
        const searchInput = form.querySelector('.user-search');
        
        if (!emailInput || !emailInput.value) {
            alert("Please select a user from the dropdown list before proceeding.");
            if (searchInput) searchInput.focus();
            return; 
        }
    }

    activeFormId = formId;

    // 3. Initialize and show Bootstrap Modal
    const modalElement = document.getElementById('adminPasswordModal');
    if (modalElement && typeof bootstrap !== 'undefined') {
        const modal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
        modal.show();
    }
}

document.addEventListener('DOMContentLoaded', function () {
    // 4. Handle the Confirm Button click
    const confirmBtn = document.getElementById('confirmAuthBtn');
    if (confirmBtn) {
        confirmBtn.addEventListener('click', function () {
            
            // Notice we use DIFFERENT variable names here so they don't clash!
            const passValue = document.getElementById('modalAdminPassword').value;
            const detailsValue = document.getElementById('modalAuditDetails').value; 
            const form = document.getElementById(activeFormId);

            if (!passValue) {
                alert("Administrator password is required to confirm this action.");
                return;
            }

            if (form) {
                const hiddenPasswordField = form.querySelector('.modal-password-target');
                const hiddenDetailsField = form.querySelector('.modal-details-target'); 

                if (hiddenPasswordField) {
                    hiddenPasswordField.value = passValue;
                    
                    if (hiddenDetailsField) { 
                        hiddenDetailsField.value = detailsValue;
                    }
                    
                    form.submit();
                }
            }
        });
    }

    // 5. Setup Search Inputs
    document.querySelectorAll('.user-search').forEach(input => {
        
        // Prevent the Enter key from submitting the form prematurely
        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault(); 
            }
        });

        let debounceTimer = null;

        input.addEventListener('input', function () {
            const term = this.value;
            const findLocked = this.dataset.findLocked;
            const datalist = document.getElementById(this.getAttribute('list'));
            const hiddenEmailInput = this.closest('form').querySelector('.selected-email');

            if (debounceTimer) {
                clearTimeout(debounceTimer);
            }

            if (term.length < 2) return;

            debounceTimer = setTimeout(async () => {
                try {
                    const response = await fetch(`/Admin/SearchUsers?term=${term}&findLocked=${findLocked}`);
                    if (response.ok) {
                        const users = await response.json();

                        datalist.innerHTML = '';
                        users.forEach(user => {
                            let option = document.createElement('option');
                            // Fallbacks implemented just in case of casing differences
                            option.value = user.displayName || user.DisplayName;
                            option.dataset.email = user.email || user.Email;
                            datalist.appendChild(option);
                        });

                        // Ensure hidden input only has a value if the text exactly matches a result
                        const match = users.find(u => (u.displayName || u.DisplayName).toLowerCase() === term.toLowerCase());
                        if (match) {
                            hiddenEmailInput.value = match.email || match.Email;
                        } else {
                            hiddenEmailInput.value = ''; // Clear it if they type something else
                        }
                    }
                } catch (error) {
                    console.error("Failed to fetch users:", error);
                }
            }, 250);
        });
    });
});