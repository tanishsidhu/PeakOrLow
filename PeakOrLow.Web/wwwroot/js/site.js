(function () {
  var form = document.getElementById('waitlist-form');
  if (!form) return;

  form.addEventListener('submit', async function (e) {
    e.preventDefault();

    var emailInput = document.getElementById('waitlist-email');
    var successMsg = document.getElementById('waitlist-success');
    var errorMsg   = document.getElementById('waitlist-error');
    var btn        = form.querySelector('.submit-btn');

    successMsg.style.display = 'none';
    errorMsg.style.display   = 'none';
    errorMsg.textContent     = '';

    var email = emailInput.value.trim();
    if (!email) return;

    btn.disabled = true;
    btn.textContent = 'Sending…';

    try {
      var res = await fetch('/api/waitlist', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: email })
      });

      var data = await res.json();

      if (data.success) {
        successMsg.style.display = 'block';
        form.style.display = 'none';
      } else {
        errorMsg.textContent = data.error || 'Something went wrong. Please try again.';
        errorMsg.style.display = 'block';
        btn.disabled = false;
        btn.textContent = 'Notify Me →';
      }
    } catch (err) {
      errorMsg.textContent = 'Something went wrong. Please try again.';
      errorMsg.style.display = 'block';
      btn.disabled = false;
      btn.textContent = 'Notify Me →';
    }
  });
})();
