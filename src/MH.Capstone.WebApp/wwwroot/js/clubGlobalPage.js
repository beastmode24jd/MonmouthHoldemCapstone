function showClubModal() {
    // Clear text field from any previous entries
    const nameInput = document.getElementById('modalClubName');
    if (nameInput)
    {
        nameInput.value = '';
    }

    const descriptionInput = document.getElementById('descInput');
    if (descriptionInput)
    {
        descriptionInput.value = '';
    }

    // Reset the character counter display when the modal opens
    const counter = document.getElementById('charCount');
    if (counter) counter.textContent = '0/250';

    // Initialize and show Manage.cshtml Bootstrap Modal
    const modalElement = document.getElementById('newClubModal');
    const modal = new bootstrap.Modal(modalElement);
    modal.show();
}

(function () {
    const STORAGE_KEY = 'clubsFilter';
    const currentUserId = document.getElementById('currentUserId')?.dataset.userId ?? '';

    const filterAllBtn = document.getElementById('filterAll');
    const filterMineBtn = document.getElementById('filterMine');
    const emptyStateMine = document.getElementById('emptyStateMine');
    const emptyStateAll = document.getElementById('emptyStateAll');
    const countLabel = document.getElementById('clubCountLabel');

    function applyFilter(filter) {
        sessionStorage.setItem(STORAGE_KEY, filter);

        const cards = document.querySelectorAll('.club-card-wrapper');
        let visibleCount = 0;

        if (filter === 'mine') {
            // ALWAYS update button colors first
            if (filterMineBtn) filterMineBtn.className = 'btn bg-lightGreen active';
            if (filterAllBtn) filterAllBtn.className = 'btn bg-darkGreen';

            // Filter the cards
            cards.forEach(function (card) {
                if (card.dataset.userId === currentUserId) {
                    card.style.display = '';
                    visibleCount++;
                } else {
                    card.style.display = 'none';
                }
            });

            // Handle Empty States and Labels
            if (emptyStateAll) emptyStateAll.style.display = 'none';
            if (emptyStateMine) emptyStateMine.style.display = (visibleCount === 0) ? '' : 'none';
            
            if (countLabel) {
                countLabel.textContent = visibleCount === 0 
                    ? '0 clubs' 
                    : visibleCount + ' ' + (visibleCount === 1 ? 'club' : 'clubs');
            }
        } else {
            // Update the button colors first
            if (filterAllBtn) filterAllBtn.className = 'btn bg-lightGreen active';
            if (filterMineBtn) filterMineBtn.className = 'btn bg-darkGreen';

            // Filter the cards
            cards.forEach(function (card) {
                if (card.dataset.isPublic === 'true') {
                    card.style.display = '';
                    visibleCount++;
                } else {
                    card.style.display = 'none';
                }
            });

            // 3. Handle Empty States and Labels
            if (emptyStateAll) emptyStateAll.style.display = (visibleCount === 0) ? '' : 'none';
            if (emptyStateMine) emptyStateMine.style.display = 'none';
            
            if (countLabel) {
                countLabel.textContent = visibleCount + ' ' + (visibleCount === 1 ? 'club' : 'clubs');
            }
        }
    }

    if (filterAllBtn) filterAllBtn.addEventListener('click', function () { applyFilter('all'); });
    if (filterMineBtn) filterMineBtn.addEventListener('click', function () { applyFilter('mine'); });

    const saved = sessionStorage.getItem(STORAGE_KEY) || 'all';
    applyFilter(saved);
})();