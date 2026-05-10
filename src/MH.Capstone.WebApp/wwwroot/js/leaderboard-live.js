// CSP-180: Leaderboard-page wiring for the site-wide live hub.
// Connection management and toast rendering live in live-hub.js (site-wide).
// This file only handles leaderboard-specific events: receiving the initial
// snapshot, applying per-entry updates, and exposing a reconnect helper for
// the acceptance test.
(function () {
    'use strict';

    if (!window.liveHub || !window.liveHub.connection) {
        // Live hub didn't initialize (e.g. SignalR client failed to load).
        // Render a banner so the user knows live updates are unavailable.
        showLiveStatusBanner('Live updates unavailable.');
        return;
    }

    var tbody = document.querySelector('table.table tbody');
    if (!tbody) return;

    var connection = window.liveHub.connection;

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
            cells[2].textContent = update.points;
        }
        reorderAndRerank();
    }

    // Sort rows by points DESC and renumber the rank cell for every row.
    // Required because a single point change can shift the rank of any user
    // whose points did NOT change — and the server only emits updates for
    // users whose points DID change.
    function reorderAndRerank() {
        var rows = Array.prototype.slice.call(tbody.querySelectorAll('tr'));
        rows.sort(function (a, b) {
            var ap = parseInt(a.querySelectorAll('td')[2].textContent.trim(), 10) || 0;
            var bp = parseInt(b.querySelectorAll('td')[2].textContent.trim(), 10) || 0;
            return bp - ap;
        });
        rows.forEach(function (row, i) {
            row.querySelectorAll('td')[0].textContent = (i + 1);
            tbody.appendChild(row);
        });
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

    connection.onreconnected(function () {
        var banner = document.getElementById('liveStatusBanner');
        if (banner) banner.remove();
    });

    // Acceptance test (CSP-180 scenario 3) calls this to exercise the
    // reconnect-snapshot path. Delegates to the site-wide reconnect helper.
    window.leaderboardLive = {
        reconnect: function () {
            return window.liveHub.reconnect();
        }
    };
})();
