// CSP-213, handles rendering audit logs.

document.addEventListener('click', function (e) {
    const button = e.target.closest('.audit-details-btn');
    
    if (button) {
        const contentDiv = button.parentElement.querySelector('.audit-details-content');
        
        // Use .textContent instead of .innerText so it can read hidden elements!
        const details = contentDiv ? contentDiv.textContent.trim() : "Error loading details.";
        
        showAuditDetailsModal(details);
    }
});

function showAuditDetailsModal(details) {
    const descElement = document.getElementById('auditDetailsDiv');
    if (descElement) {
        descElement.innerText = details;
    }

    const modalElement = document.getElementById('auditDetailsModal');
    if (modalElement && typeof bootstrap !== 'undefined') {
        const modal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
        modal.show();
    } else {
        console.error("Bootstrap or Modal element not found.");
    }
}

// Attach listeners to both input fields
setupAutoSuggest('adminSearchInput', 'adminSuggestions');
setupAutoSuggest('userSearchInput', 'userSuggestions');

function setupAutoSuggest(inputId, datalistId) {
    const input = document.getElementById(inputId);
    const datalist = document.getElementById(datalistId);

    if (!input || !datalist) return;

    input.addEventListener('input', async function (e) {
        const term = e.target.value;
        
        // Wait until they've typed at least 2 characters to save database calls
        if (term.length < 2) return; 

        try {
            const response = await fetch(`/Admin/SearchUserNames?term=${encodeURIComponent(term)}`);
            if (response.ok) {
                const users = await response.json(); // Changed variable name to users for clarity
                
                datalist.innerHTML = '';
                
                users.forEach(user => {
                    const option = document.createElement('option');
                    
                    // Update this line to read the property!
                    // If your endpoint returns just strings, use `user`. If it returns objects, use `user.displayName`.
                    // To be safe, we can handle both:
                    option.value = user.displayName || user; 
                    
                    datalist.appendChild(option);
                });
            }
        } catch (error) {
            console.error("Auto-suggest fetch failed:", error);
        }
    });
}