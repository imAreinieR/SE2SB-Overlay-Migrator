let displayDuration = 5000;

window.addEventListener('onWidgetLoad', function (obj) {
    const f = obj.detail.fieldData;
    displayDuration = (f.displayDuration || 5) * 1000;
});

window.addEventListener('onEventReceived', function (obj) {
    if (!obj.detail.event) return;

    if (typeof obj.detail.event.itemId !== "undefined") {
        obj.detail.listener = "redemption-latest";
    }

    const listener = obj.detail.listener.split("-")[0];
    const event = obj.detail.event;

    if (listener === 'follower') {
        showAlert('follower', event.name, 'New Follower');
    } else if (listener === 'subscriber') {
        if (event.gifted) {
            showAlert('sub', event.name, 'Gifted a Sub!');
        } else {
            const months = event.amount > 1 ? `${event.amount} Month Sub` : 'New Subscriber';
            showAlert('sub', event.name, months);
        }
    } else if (listener === 'cheer') {
        showAlert('cheer', event.name, `${event.amount.toLocaleString()} Bits`);
    } else if (listener === 'raid') {
        showAlert('raid', event.name, `Raiding with ${event.amount.toLocaleString()} viewers!`);
    }
});

function showAlert(type, username, detail) {
    const container = document.querySelector('.main-container');

    const el = document.createElement('div');
    el.className = 'event-container';
    el.innerHTML = `
        <div class="event-icon event-${type}"></div>
        <div class="event-text">
            <div class="event-username">${username}</div>
            <div class="event-detail">${detail}</div>
        </div>
    `;

    container.prepend(el);

    setTimeout(() => {
        el.classList.add('hiding');
        el.addEventListener('animationend', () => el.remove(), { once: true });
    }, displayDuration);
}
