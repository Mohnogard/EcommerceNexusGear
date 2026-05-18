/* NexusGear Main JS */

// ── Sticky nav scroll class ──────────────────────────────────
window.addEventListener('scroll', () => {
  const nav = document.querySelector('.nexus-nav');
  if (nav) nav.classList.toggle('scrolled', window.scrollY > 30);
});

// ── SweetAlert2 Toast ────────────────────────────────────────
let _NxToast = null;
function showToast(message, type = 'success') {
  if (!window.Swal) return;
  if (!_NxToast) {
    _NxToast = Swal.mixin({
      toast: true,
      position: 'top-end',
      showConfirmButton: false,
      timer: 3200,
      timerProgressBar: true,
    });
  }
  _NxToast.fire({
    icon: type,
    title: message,
    background: '#150020',
    color: '#f1e8ff',
    customClass: { popup: 'nexus-swal-toast', title: 'nexus-swal-toast-title' }
  });
}

// ── Cart count refresh ───────────────────────────────────────
function refreshCartCount() {
  fetch('/Cart/Count')
    .then(r => r.json())
    .then(data => {
      const el = document.getElementById('cartCount');
      if (el) el.textContent = data.count || 0;
    })
    .catch(() => {});
}

// ── Add to cart ──────────────────────────────────────────────
function addToCart(productId, qty = 1, btn = null) {
  const originalHtml = btn ? btn.innerHTML : null;
  if (btn) { btn.disabled = true; btn.innerHTML = '<span class="nx-spinner"></span>'; }
  const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

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
      if (btn) {
        btn.disabled = false;
        btn.innerHTML = originalHtml || '<i class="fas fa-cart-plus"></i> Add to Cart';
      }
    });
}

// ── Wishlist toggle ──────────────────────────────────────────
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
        showToast(data.added ? 'Added to wishlist!' : 'Removed from wishlist', data.added ? 'success' : 'info');
      } else if (data.requiresLogin) {
        window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
      }
    })
    .catch(() => { window.location.href = '/Account/Login'; });
}

// ── Cart page: quantity update ───────────────────────────────
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
          showToast(data.message || 'Stock limit reached', 'warning');
          updateQty(cartItemId, input);
        }
        return;
      }
      const itemTotalEl = document.getElementById(`itemTotal_${cartItemId}`);
      if (itemTotalEl) itemTotalEl.textContent = '$' + parseFloat(data.itemTotal).toFixed(2);
      updateSummary(data.subtotal, data.shipping, data.total);
      const el = document.getElementById('cartCount');
      if (el) el.textContent = data.cartCount;
      if (qty === 0) { location.reload(); return; }
      showToast('Cart updated', 'success');
    })
    .catch(() => showToast('Update failed', 'error'));
}

// ── Cart page: remove item (with SweetAlert2 confirm) ────────
function removeFromCart(cartItemId) {
  if (!window.Swal) return;
  Swal.fire({
    title: 'Remove item?',
    text: 'This item will be removed from your cart.',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Yes, remove',
    cancelButtonText: 'Keep it',
    background: '#150020',
    color: '#f1e8ff',
    confirmButtonColor: '#ef4444',
    cancelButtonColor: '#7c22d4',
    iconColor: '#f59e0b',
  }).then((result) => {
    if (!result.isConfirmed) return;
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
          if (row) { row.style.transition = 'opacity 0.3s'; row.style.opacity = '0'; setTimeout(() => row.remove(), 320); }
          updateSummary(data.subtotal, data.shipping, data.total);
          const el = document.getElementById('cartCount');
          if (el) el.textContent = data.cartCount;
          showToast('Item removed from cart', 'success');
          if (data.cartCount === 0) setTimeout(() => location.reload(), 800);
        }
      })
      .catch(() => showToast('Remove failed', 'error'));
  });
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

// ── Product detail: thumbnail gallery ───────────────────────
document.addEventListener('DOMContentLoaded', () => {
  // Init cart count
  refreshCartCount();

  // Thumbnail gallery
  const mainImg = document.getElementById('mainProductImg');
  document.querySelectorAll('.thumbnail').forEach(thumb => {
    thumb.addEventListener('click', () => {
      if (mainImg) {
        mainImg.style.opacity = '0';
        setTimeout(() => {
          mainImg.src = thumb.dataset.src;
          mainImg.style.opacity = '1';
        }, 150);
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

  // Qty +/- buttons in CART (not product detail)
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

  // Testimonials slider
  (function () {
    const track = document.getElementById('tCarouselTrack');
    if (!track) return;
    const groups = Array.from(track.querySelectorAll('.tpage-group'));
    if (groups.length <= 1) return;

    let curr = 0;
    let timer;
    const dots = Array.from(document.querySelectorAll('.tcarousel-dot'));

    const go = (i) => {
      curr = ((i % groups.length) + groups.length) % groups.length;
      track.style.transform = `translateX(-${curr * 100}%)`;
      dots.forEach((d, di) => d.classList.toggle('active', di === curr));
    };

    const startTimer = () => { timer = setInterval(() => go(curr + 1), 5000); };
    const resetTimer = () => { clearInterval(timer); startTimer(); };

    document.getElementById('tPrev')?.addEventListener('click', () => { go(curr - 1); resetTimer(); });
    document.getElementById('tNext')?.addEventListener('click', () => { go(curr + 1); resetTimer(); });
    dots.forEach((d, i) => d.addEventListener('click', () => { go(i); resetTimer(); }));

    startTimer();
  })();
});
