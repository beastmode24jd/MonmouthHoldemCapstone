
let currentActiveReportId = null;

function showDetailsModal(reportId, description, isResolved) {
    currentActiveReportId = reportId;
    
    // Set description text
    const descElement = document.getElementById('modalDescription');
    descElement.innerText = description || "No description provided.";

    // Set checkbox status in modal
    const checkbox = document.getElementById('modalIsResolved');
    checkbox.checked = isResolved;

    const modal = new bootstrap.Modal(document.getElementById('reportDetailsModal'));
    modal.show();
    modal.show();
}

// Handle "Confirm" button in Modal
document.getElementById('confirmResolveBtn').addEventListener('click', async function() {
    if (currentActiveReportId) {
        await toggleResolution(currentActiveReportId);
        location.reload(); // Refresh to show updated status
    }
});

// Handle direct checkbox clicks in the table
document.querySelectorAll('.resolution-toggle').forEach(checkbox => {
    checkbox.addEventListener('change', async function() {
        const id = this.getAttribute('data-id');
        await toggleResolution(id);
    });
});

// Shared AJAX function
async function toggleResolution(reportId) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    try {
        const response = await fetch(`/Admin/ToggleResolution/${reportId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            alert("Failed to update report status.");
            location.reload(); // Revert UI if server fails
        }
    } catch (error) {
        console.error("Error:", error);
    }
}