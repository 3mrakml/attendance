// Prevent Double Submission Globally
document.addEventListener('submit', function (e) {
    var form = e.target;
    
    // Prevent multiple submissions
    if (form.dataset.submitting) {
        e.preventDefault();
        return;
    }
    
    // Check HTML5 validation
    if (form.checkValidity && !form.checkValidity()) {
        return;
    }
    
    // Check jQuery validation if exists
    if (window.jQuery && typeof $(form).valid === 'function' && !$(form).valid()) {
        return;
    }
    
    form.dataset.submitting = 'true';
    
    var submitBtn = form.querySelector('button[type="submit"], input[type="submit"]');
    if (submitBtn) {
        // Add a tiny delay before disabling to ensure form data is captured by the browser
        setTimeout(function() {
            submitBtn.disabled = true;
            if (submitBtn.tagName === 'BUTTON') {
                const originalText = submitBtn.innerHTML;
                submitBtn.dataset.originalText = originalText;
                submitBtn.innerHTML = '<svg class="animate-spin -ml-1 mr-2 h-4 w-4 inline-block text-current opacity-75" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg> جاري المعالجة...';
            }
        }, 10);
    }
});
