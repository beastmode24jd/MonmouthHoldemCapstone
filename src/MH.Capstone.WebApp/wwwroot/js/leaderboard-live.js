// CSP-180: Real-time leaderboard client.
// Connects to /hubs/leaderboard, applies snapshot + per-entry updates to the
// existing rendered table, and surfaces toast notifications. Exposes
// window.leaderboardLive.reconnect() so acceptance tests can exercise the
// reconnect-snapshot scenario.
(function () {
    'use strict';

    if (typeof signalR === 'undefined') {
        showLiveStatusBanner('Live updates unavailable (SignalR client failed to load).');
        return;
    }

    var tbody = document.querySelector('table.table tbody');
    if (!tbody) return;

    var connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/leaderboard')
        .withAutomaticReconnect()
        .build();

    function escapeHtml(s) {
        var div = document.createElement('div');
        div.textContent = String(s == null ? '' : s);
        return div.innerHTML;
    }

    function renderSnapshot(entries) {
        if (!Array.isArray(entries)) return;
        tbody.innerHTML = '';
        entries.forEach(function (entry) {
            var tr = document.createElement('tr');
            tr.id = 'user-' + entry.userId;
            tr.innerHTML =
                '<td>' + entry.rank + '</td>' +
                '<td>' + escapeHtml(entry.displayName) + '</td>' +
                '<td>' + entry.points + '</td>';
            tbody.appendChild(tr);
        });
    }

    function applyEntryUpdate(update) {
        if (!update || !update.userId) return;
        var row = document.getElementById('user-' + update.userId);
        if (!row) return;
        var cells = row.querySelectorAll('td');
        if (cells.length >= 3) {
            cells[0].textContent = update.rank;
            cells[2].textContent = update.points;
        }
    }

    function showToast(notification) {
        var existing = document.getElementById('liveNotificationToast');
        if (existing) existing.remove();

        var toast = document.createElement('div');
        toast.id = 'liveNotificationToast';
        toast.className = 'toast position-fixed top-0 end-0 m-3 show bg-info text-white';
        toast.setAttribute('role', 'alert');
        var title = (notification && notification.title) || 'Update';
        var msg = (notification && notification.message) || '';
        toast.innerHTML =
            '<div class="toast-body">' +
                '<strong>' + escapeHtml(title) + '</strong>' +
                (msg ? ': ' + escapeHtml(msg) : '') +
            '</div>';
        document.body.appendChild(toast);
        setTimeout(function () {
            if (toast.parentNode) toast.parentNode.removeChild(toast);
        }, 4000);
    }

    function showLiveStatusBanner(text) {
        var existing = document.getElementById('liveStatusBanner');
        if (existing) { existing.textContent = text; return; }
        var banner = document.createElement('div');
        banner.id = 'liveStatusBanner';
        banner.className = 'alert alert-warning';
        banner.setAttribute('role', 'alert');
        banner.textContent = text;
        var container = document.querySelector('.container');
        if (container) container.insertBefore(banner, container.firstChild);
    }

    connection.on('LeaderboardSnapshot', renderSnapshot);
    connection.on('LeaderboardUpdated', applyEntryUpdate);
    connection.on('LiveNotification', showToast);

    connection.onreconnected(function () {
        var banner = document.getElementById('liveStatusBanner');
        if (banner) banner.remove();
    });

    connection.start().catch(function (err) {
        console.error('SignalR connect failed', err);
        showLiveStatusBanner('Live updates unavailable.');
    });

    // Expose a reconnect helper so acceptance tests (CSP-180 scenario 3) can
    // exercise the reconnect-snapshot path.
    window.leaderboardLive = {
        reconnect: function () {
            return connection.stop().then(function () {
                return connection.start();
            }).catch(function (err) {
                console.error('reconnect failed', err);
                showLiveStatusBanner('Live updates unavailable.');
            });
        }
    };
})();
