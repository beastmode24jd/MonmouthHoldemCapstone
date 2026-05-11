// CSP-180: Site-wide SignalR connection.
// Owns the single hub connection used across the whole app, surfaces
// LiveNotification events as toasts (so rank-change toasts appear no matter
// which page the user is on), and exposes the connection on window.liveHub
// for page-specific scripts (e.g. leaderboard-live.js) to subscribe to
// LeaderboardSnapshot / LeaderboardUpdated without opening a second socket.
(function () {
    'use strict';

    if (typeof signalR === 'undefined') return;

    var connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/leaderboard')
        .withAutomaticReconnect()
        .build();

    function escapeHtml(s) {
        var div = document.createElement('div');
        div.textContent = String(s == null ? '' : s);
        return div.innerHTML;
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

    connection.on('LiveNotification', showToast);

    connection.start().catch(function (err) {
        console.error('SignalR connect failed', err);
    });

    // Expose the connection so page-specific scripts can subscribe to events
    // (LeaderboardSnapshot, LeaderboardUpdated) without re-opening the socket.
    window.liveHub = {
        connection: connection,
        reconnect: function () {
            return connection.stop().then(function () {
                return connection.start();
            }).catch(function (err) {
                console.error('reconnect failed', err);
            });
        }
    };
})();
