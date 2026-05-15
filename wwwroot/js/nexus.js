/* NexusGear Main JS */

// Sticky nav scroll class
window.addEventListener('scroll', () => {
  const nav = document.querySelector('.nexus-nav');
  if (nav) nav.classList.toggle('scrolled', window.scrollY > 30);
});

// Toast notifications
function showToast(message, type = 'success') {
  const container = document.getElementById('toastContainer');
  if (!container) return;
  const id = 'toast-' + Date.now();
  const icon = type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle';
  const html = `
    <div id="${id}" class="nexus-toast ${type}" role="alert">
      <i class="fas ${icon} nexus-toast-icon"></i>
      <span class="nexus-toast-msg">${message}</span>
      <button class="nexus-toast-close" onclick="this.closest('.nexus-toast').remove()"><i class="fas fa-times"></i></button>
    </div>`;
  container.insertAdjacentHTML('beforeend', html);
  setTimeout(() => { const el = document.getElementById(id); if (el) el.remove(); }, 3500);
}

// Cart count refresh
function refreshCartCount() {
  fetch('/Cart/Count')
    .then(r => r.json())
    .then(data => {
      const el = document.getElementById('cartCount');
      if (el) el.textContent = data.count || 0;
    })
    .catch(() => {});
}

// Add to cart
function addToCart(productId, qty = 1, btn = null) {
  if (btn) { btn.disabled = true; btn.innerHTML = '<span class="nx-spinner"></span>'; }
  const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value
    || document.querySelector('meta[name="csrf-token"]')?.content || '';

  fetch('/Cart/Add', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': token },
    body: `productId=${productId}&quantity=${qty}`
  })
    .then(r => r.json())
    .then(data => {
      if (data.requiresLogin) {
        window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
        return;
      }
      if (data.success) {
        const el = document.getElementById('cartCount');
        if (el) el.textContent = data.cartCount;
        showToast('Added to cart!', 'success');
      } else {
        showToast(data.message || 'Could not add to cart.', 'error');
      }
    })
    .catch(() => showToast('Something went wrong.', 'error'))
    .finally(() => {
      if (btn) { btn.disabled = false; btn.innerHTML = '<i class="fas fa-cart-plus"></i> Add to Cart'; }
    });
}

// Wishlist toggle
function toggleWishlist(productId, btn) {
  const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
  fetch('/Dashboard/ToggleWishlist', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': token },
    body: `productId=${productId}`
  })
    .then(r => r.json())
    .then(data => {
      if (data.success) {
        btn.classList.toggle('active', data.added);
        const icon = btn.querySelector('i');
        if (icon) icon.className = data.added ? 'fas fa-heart' : 'far fa-heart';
        showToast(data.added ? 'Added to wishlist' : 'Removed from wishlist', 'success');
      } else if (data.requiresLogin) {
        window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
      }
    })
    .catch(() => { window.location.href = '/Account/Login'; });
}

// Cart page: quantity update
function updateQty(cartItemId, input) {
  const qty = parseInt(input.value);
  if (isNaN(qty) || qty < 0) { input.value = 1; return; }
  const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
  fetch('/Cart/Update', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': token },
    body: `cartItemId=${cartItemId}&quantity=${qty}`
  })
    .then(r => r.json())
    .then(data => {
      if (!data.success) {
        if (data.maxQty != null) {
          input.value = data.maxQty;
          showToast(data.message || 'Stock limit reached', 'error');
          updateQty(cartItemId, input);
        }
        return;
      }
      // Update item total
      const itemTotalEl = document.getElementById(`itemTotal_${cartItemId}`);
      if (itemTotalEl) itemTotalEl.textContent = '$' + parseFloat(data.itemTotal).toFixed(2);
      // Update summary
      updateSummary(data.subtotal, data.shipping, data.total);
      const el = document.getElementById('cartCount');
      if (el) el.textContent = data.cartCount;
      if (qty === 0) location.reload();
    })
    .catch(() => showToast('Update failed', 'error'));
}

function removeFromCart(cartItemId) {
  const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
  fetch('/Cart/Remove', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': token },
    body: `cartItemId=${cartItemId}`
  })
    .then(r => r.json())
    .then(data => {
      if (data.success) {
        const row = document.getElementById(`cartRow_${cartItemId}`);
        if (row) { row.style.opacity = '0'; setTimeout(() => row.remove(), 300); }
        updateSummary(data.subtotal, data.shipping, data.total);
        const el = document.getElementById('cartCount');
        if (el) el.textContent = data.cartCount;
        if (data.cartCount === 0) setTimeout(() => location.reload(), 400);
      }
    })
    .catch(() => showToast('Remove failed', 'error'));
}

function updateSummary(subtotal, shipping, total) {
  const fmt = v => '$' + parseFloat(v).toFixed(2);
  const sub = document.getElementById('summarySubtotal');
  const ship = document.getElementById('summaryShipping');
  const tot = document.getElementById('summaryTotal');
  if (sub) sub.textContent = fmt(subtotal);
  if (ship) ship.textContent = shipping == 0 ? 'FREE' : fmt(shipping);
  if (tot) tot.textContent = fmt(total);
}

// Product detail: thumbnail gallery
document.addEventListener('DOMContentLoaded', () => {
  // Init cart count
  refreshCartCount();

  // Thumbnail gallery
  const mainImg = document.getElementById('mainProductImg');
  document.querySelectorAll('.thumbnail').forEach(thumb => {
    thumb.addEventListener('click', () => {
      if (mainImg) {
        mainImg.src = thumb.dataset.src;
        mainImg.classList.add('fade-in');
        setTimeout(() => mainImg.classList.remove('fade-in'), 300);
      }
      document.querySelectorAll('.thumbnail').forEach(t => t.classList.remove('active'));
      thumb.classList.add('active');
    });
  });

  // Payment method selector (checkout page)
  document.querySelectorAll('.payment-method-option').forEach(opt => {
    opt.addEventListener('click', () => {
      document.querySelectorAll('.payment-method-option').forEach(o => o.classList.remove('selected'));
      opt.classList.add('selected');
      const radio = opt.querySelector('input[type="radio"]');
      if (radio) radio.checked = true;
      const method = radio?.value;
      const stripePanel = document.getElementById('stripe-panel');
      const codPanel = document.getElementById('cod-panel');
      if (stripePanel) stripePanel.style.display = method === 'card' ? 'block' : 'none';
      if (codPanel) codPanel.style.display = method === 'cod' ? 'block' : 'none';
    });
  });

  // Admin sidebar mobile toggle
  const sidebarToggle = document.getElementById('sidebarToggle');
  if (sidebarToggle) {
    sidebarToggle.addEventListener('click', () => {
      document.getElementById('adminSidebar')?.classList.toggle('open');
    });
  }

  // Qty +/- buttons in cart
  document.querySelectorAll('.qty-plus').forEach(btn => {
    btn.addEventListener('click', () => {
      const input = btn.closest('.qty-controls')?.querySelector('.qty-input');
      if (input) {
        input.value = parseInt(input.value || 0) + 1;
        updateQty(btn.dataset.itemId, input);
      }
    });
  });

  document.querySelectorAll('.qty-minus').forEach(btn => {
    btn.addEventListener('click', () => {
      const input = btn.closest('.qty-controls')?.querySelector('.qty-input');
      if (input) {
        const newVal = Math.max(0, parseInt(input.value || 1) - 1);
        input.value = newVal;
        updateQty(btn.dataset.itemId, input);
      }
    });
  });
});
