
let currentActiveReportId = null;

document.addEventListener('click', function (e) {
    if (e.target && e.target.classList.contains('details-btn')) {
        const id = e.target.getAttribute('data-id');
        const desc = e.target.getAttribute('data-description');
        const resolved = e.target.getAttribute('data-resolved') === 'true';
        showDetailsModal(id, desc, resolved);
    }
});

function showDetailsModal(reportId, description, isResolved) {
    currentActiveReportId = reportId;
    
    // Set description text
    const descElement = document.getElementById('modalDescription');
    if (descElement) {
        descElement.innerText = description || "No description provided.";
    }

    // Set checkbox status in modal
    const checkbox = document.getElementById('modalIsResolved');
    if (checkbox) {
        checkbox.checked = isResolved;
    }

    // Initialize and show the modal
    const modalElement = document.getElementById('reportDetailsModal');
    if (modalElement && typeof bootstrap !== 'undefined') {
        // Reuse existing instance or create a new one
        const modal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
        modal.show();
    } else {
        console.error("Bootstrap or Modal element not found.");
    }
}

// Handle "Confirm" button in Modal
document.getElementById('confirmResolveBtn').addEventListener('click', async function() {
    if (currentActiveReportId) {
        // Get the status from the modal's checkbox
        const isChecked = document.getElementById('modalIsResolved').checked;
        
        // Grab the details from the new textarea
        const details = document.getElementById('modalAuditDetails').value;
        
        // Pass the details to the updateResolution function
        await updateResolution(currentActiveReportId, isChecked, details);
        location.reload(); 
    }
});

// Handle direct checkbox clicks in the table
document.querySelectorAll('.resolution-toggle').forEach(checkbox => {
    checkbox.addEventListener('change', async function() {
        const id = this.getAttribute('data-id');
        const isChecked = this.checked; // Capture the actual state

        // Direct toggle bypasses the modal, so we pass an empty string for details
        await updateResolution(id, isChecked, "");
    });
});

// Shared AJAX function
async function updateResolution(reportId, isResolved, details) {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    if (!tokenElement) return;

    const token = tokenElement.value;

    try {
        // Now 'details' is defined in the function scope and won't throw an error!
        const encodedDetails = encodeURIComponent(details || "");
        
        // Append the encoded details to the fetch URL
        const response = await fetch(`/Admin/UpdateResolution/${reportId}?status=${isResolved}&details=${encodedDetails}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            alert("Failed to update report status.");
            location.reload(); 
        }
    } catch (error) {
        console.error("Error:", error);
    }
}