/* =======================================================
   RestaurantPOS - Main JavaScript
   ======================================================= */

'use strict';

function localText(key, fallback) {
    return (window.appTexts && window.appTexts[key]) || fallback;
}

// --- Theme Management ---
function toggleTheme() {
    const html = document.documentElement;
    const current = html.getAttribute('data-bs-theme');
    const next = current === 'dark' ? 'light' : 'dark';
    html.setAttribute('data-bs-theme', next);
    localStorage.setItem('pos-theme', next);
    document.getElementById('theme-icon').className = next === 'dark' ? 'bi bi-sun-fill' : 'bi bi-moon-stars-fill';
}

// Restore saved theme
(function () {
    const saved = localStorage.getItem('pos-theme') || 'light';
    document.documentElement.setAttribute('data-bs-theme', saved);
    const icon = document.getElementById('theme-icon');
    if (icon) icon.className = saved === 'dark' ? 'bi bi-sun-fill' : 'bi bi-moon-stars-fill';
})();

// --- Language switch ---
function setLang(lang) {
    document.cookie = `.AspNetCore.Culture=c=${lang}|uic=${lang};path=/;max-age=31536000`;
    location.reload();
}

// --- Toast Notifications ---
window.showToast = function (message, type = 'info', title = '') {
    const container = document.getElementById('toast-container');
    if (!container) return;
    const id = 'toast-' + Date.now();
    const icons = { success: 'check-circle-fill', danger: 'exclamation-triangle-fill', warning: 'exclamation-circle-fill', info: 'info-circle-fill' };
    const html = `
        <div id="${id}" class="toast align-items-center text-bg-${type} border-0" role="alert" aria-live="assertive">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="bi bi-${icons[type] || 'info-circle'} me-2"></i>
                    ${title ? '<strong>' + title + '</strong> ' : ''}${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>`;
    container.insertAdjacentHTML('beforeend', html);
    const toastEl = document.getElementById(id);
    const toast = new bootstrap.Toast(toastEl, { delay: 4000 });
    toast.show();
    toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
};

// --- SignalR connection ---
let posConnection = null;

function connectSignalR(branchId) {
    posConnection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/pos')
        .withAutomaticReconnect()
        .build();

    posConnection.on('NewOrder', (data) => {
        showToast(formatAppText('newOrderMessage', data.orderNumber, data.orderType), 'info', localText('newOrderTitle', 'New Order'));
        updateNotificationBadge(1);
    });

    posConnection.on('KitchenReady', (data) => {
        showToast(formatAppText('kitchenReadyMessage', data.orderNumber), 'success', localText('kitchenReadyTitle', 'Kitchen Ready'));
        updateNotificationBadge(1);
    });

    posConnection.on('LowStock', (data) => {
        showToast(formatAppText('lowStockMessage', data.productName, data.quantity), 'warning', localText('lowStockTitle', 'Low Stock'));
    });

    posConnection.on('InvoiceFailed', (data) => {
        showToast(formatAppText('invoiceFailedMessage', data.invoiceNumber), 'danger', localText('invoiceFailedTitle', 'Invoice Failed'));
    });

    posConnection.start()
        .then(() => posConnection.invoke('JoinBranch', branchId))
        .catch(err => console.error(localText('signalrConnectionFailed', 'SignalR connection failed') + ':', err));
}

function updateNotificationBadge(add) {
    const badge = document.getElementById('notif-count');
    if (!badge) return;
    const current = parseInt(badge.textContent) || 0;
    const next = current + add;
    badge.textContent = next;
    badge.style.display = next > 0 ? '' : 'none';
}

// --- POS Keyboard Shortcuts ---
document.addEventListener('keydown', (e) => {
    if (e.altKey) {
        switch (e.key) {
            case 'F1': location.href = '/POS'; break;
            case 'F2': location.href = '/Orders'; break;
            case 'F3': location.href = '/Kitchen'; break;
            case 'F4': location.href = '/Tables'; break;
        }
    }
    // Barcode scanner: reads characters rapidly, ends with Enter
});

// --- Barcode Scanner Support ---
let barcodeBuffer = '';
let barcodeTimer = null;

document.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' && barcodeBuffer.length > 3) {
        const barcode = barcodeBuffer;
        barcodeBuffer = '';
        window.dispatchEvent(new CustomEvent('barcodeScanned', { detail: { barcode } }));
        return;
    }
    if (e.key.length === 1) {
        barcodeBuffer += e.key;
        clearTimeout(barcodeTimer);
        barcodeTimer = setTimeout(() => { barcodeBuffer = ''; }, 100);
    }
});

// --- HTMX global event handlers ---
document.addEventListener('htmx:afterRequest', (e) => {
    if (e.detail.xhr.status >= 400) {
        showToast(localText('genericRequestError', 'An error occurred. Please try again.'), 'danger');
    }
});

// --- Confirm delete helper ---
window.confirmDelete = function (url, name) {
    if (confirm(formatAppText('deleteConfirmMessage', name))) {
        const form = document.createElement('form');
        form.method = 'post';
        form.action = url;
        const csrf = document.querySelector('input[name="__RequestVerificationToken"]');
        if (csrf) form.appendChild(csrf.cloneNode());
        document.body.appendChild(form);
        form.submit();
    }
};

// --- Number formatting ---
window.formatCurrency = function (amount, currency = 'SAR') {
    return new Intl.NumberFormat(document.documentElement.lang, { style: 'currency', currency }).format(amount);
};
