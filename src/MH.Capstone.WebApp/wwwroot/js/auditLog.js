// CSP-213, handles rendering audit logs.

// Wait for HTML to load before attaching scripts
document.addEventListener('DOMContentLoaded', function() {
    
    // 1. Audit Details Modal Handler
    document.addEventListener('click', function (e) {
        const button = e.target.closest('.audit-details-btn');
        
        if (button) {
            const contentDiv = button.parentElement.querySelector('.audit-details-content');
            const details = contentDiv ? contentDiv.textContent.trim() : "Error loading details.";
            
            const descElement = document.getElementById('auditDetailsDiv');
            if (descElement) {
                descElement.innerText = details;
            }

            const modalElement = document.getElementById('auditDetailsModal');
            if (modalElement && typeof bootstrap !== 'undefined') {
                // Safely create a fresh modal instance
                const modal = new bootstrap.Modal(modalElement);
                modal.show();
            } else {
                console.error("Bootstrap modal element not found.");
            }
        }
    });

    // 2. Attach listeners to both input fields
    setupAutoSuggest('adminSearchInput', 'adminSuggestions');
    setupAutoSuggest('userSearchInput', 'userSuggestions');

    function setupAutoSuggest(inputId, datalistId) {
        const input = document.getElementById(inputId);
        const datalist = document.getElementById(datalistId);

        if (!input || !datalist) return;

        let debounceTimer = null;

        input.addEventListener('input', function (e) {
            const term = e.target.value;
            
            if (debounceTimer) clearTimeout(debounceTimer);
            if (term.length < 2) return; 

            debounceTimer = setTimeout(async () => {
                try {
                    const response = await fetch(`/Admin/SearchUserNames?term=${encodeURIComponent(term)}`);
                    if (response.ok) {
                        const users = await response.json();
                        
                        datalist.innerHTML = '';
                        users.forEach(user => {
                            const option = document.createElement('option');
                            option.value = user.displayName || user; 
                            datalist.appendChild(option);
                        });
                    }
                } catch (error) {
                    console.error("Auto-suggest fetch failed:", error);
                }
            }, 250); 
        });
    }
});