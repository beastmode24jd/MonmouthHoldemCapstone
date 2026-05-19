// CSP-218: Real-time club chatroom via SignalR.
// Opens a hub connection scoped to this club only when the user is on the
// Chatroom page. Intercepts form submit so messages are delivered instantly
// to all connected members without a page reload. Falls back to normal POST
// if SignalR is unavailable.

function escapeHtml(s) {
    var div = document.createElement('div');
    div.textContent = String(s == null ? '' : s);
    return div.innerHTML;
}

function formatTime(isoString) {
    var d = new Date(isoString);
    return d.toLocaleString('en-US', { month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' });
}

// Builds and appends a message bubble to `container`.
// Accepts the container element and currentUserId so it can be called with
// any container reference (makes unit testing straightforward).
function appendMessage(container, currentUserId, msg) {
    var emptyState = document.getElementById('emptyMessagesState');
    if (emptyState) emptyState.remove();

    var isOwn = msg.authorId === currentUserId;

    var wrapper = document.createElement('div');
    wrapper.className = 'd-flex ' + (isOwn ? 'justify-content-end' : 'justify-content-start');

    var bubble = document.createElement('div');
    bubble.className = (isOwn ? 'bg-primary text-white' : 'bg-light border') + ' rounded px-3 py-2';
    bubble.style.maxWidth = '70%';

    var html = '';
    if (!isOwn) {
        html += '<div class="fw-semibold small mb-1">' + escapeHtml(msg.authorDisplayName) + '</div>';
    }
    html += '<div>' + escapeHtml(msg.content) + '</div>';
    html += '<div class="' + (isOwn ? 'text-white-50' : 'text-muted') + ' small mt-1 text-end">'
        + escapeHtml(formatTime(msg.sentAtUtc)) + '</div>';

    bubble.innerHTML = html;
    wrapper.appendChild(bubble);
    container.appendChild(wrapper);
    container.scrollTop = container.scrollHeight;
}

(function () {
    'use strict';

    var messageList = document.getElementById('messageList');
    var form = document.getElementById('sendMessageForm');

    if (!messageList || !form) return;

    var clubId = messageList.dataset.clubId;
    var currentUserId = messageList.dataset.currentUserId;

    if (!clubId || typeof signalR === 'undefined') return;

    var connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/chat?clubId=' + clubId)
        .withAutomaticReconnect()
        .build();

    connection.on('ReceiveMessage', function (msg) {
        appendMessage(messageList, currentUserId, msg);
    });

    connection.start().catch(function (err) {
        console.error('ChatHub connect failed:', err);
    });

    window.addEventListener('beforeunload', function () {
        connection.stop();
    });

    // Intercept form submit — send via hub instead of full-page POST.
    // The form's POST action remains as a no-JS fallback.
    form.addEventListener('submit', function (e) {
        var input = document.getElementById('messageInput');
        var content = input ? input.value.trim() : '';
        if (!content) return;

        e.preventDefault();

        connection.invoke('SendMessage', clubId, content).then(function () {
            if (input) {
                input.value = '';
                var counter = document.getElementById('charCounter');
                if (counter) counter.textContent = '0 / 2000';
                counter && counter.classList.remove('text-danger');
            }
        }).catch(function (err) {
            console.error('SendMessage failed:', err);
        });
    });
})();

// ── Jest / Node exports ─────────────────────────────────────────────────────

if (typeof module !== 'undefined' && module.exports) {
    module.exports = { escapeHtml, formatTime, appendMessage };
}
