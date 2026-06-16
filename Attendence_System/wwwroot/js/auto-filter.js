document.addEventListener('DOMContentLoaded', function () {
    const autoFilterForms = document.querySelectorAll('.js-auto-filter');
    
    // استعادة التركيز إذا كان محفوظاً في الجلسة
    const focusedInputId = sessionStorage.getItem('autoFilter_focusedInputId');
    const focusedInputPosition = sessionStorage.getItem('autoFilter_cursorPosition');

    if (focusedInputId) {
        const inputToFocus = document.getElementById(focusedInputId);
        if (inputToFocus) {
            // إعادة التركيز
            inputToFocus.focus();
            
            // إعادة المؤشر لنفس المكان
            if (focusedInputPosition) {
                const pos = parseInt(focusedInputPosition, 10);
                setTimeout(() => {
                    inputToFocus.setSelectionRange(pos, pos);
                }, 0);
            }
        }
        // مسح البيانات بعد استخدامها
        sessionStorage.removeItem('autoFilter_focusedInputId');
        sessionStorage.removeItem('autoFilter_cursorPosition');
    }

    autoFilterForms.forEach(form => {
        let typingTimer;
        const doneTypingInterval = 500; // نصف ثانية للانتظار قبل البحث

        // دالة حفظ التركيز قبل الإرسال
        function saveFocusAndSubmit() {
            const activeElement = document.activeElement;
            if (activeElement && activeElement.tagName === 'INPUT' && activeElement.id) {
                sessionStorage.setItem('autoFilter_focusedInputId', activeElement.id);
                sessionStorage.setItem('autoFilter_cursorPosition', activeElement.selectionStart);
            }
            form.submit();
        }

        // التعامل مع حقول الإدخال النصية (البحث)
        const textInputs = form.querySelectorAll('input[type="text"], input[type="search"]');
        textInputs.forEach(input => {
            input.addEventListener('keyup', function (e) {
                // تجاهل أزرار التوجيه لتجنب التحديث العشوائي
                if (e.key === 'ArrowUp' || e.key === 'ArrowDown' || e.key === 'ArrowLeft' || e.key === 'ArrowRight' || e.key === 'Tab') {
                    return;
                }

                clearTimeout(typingTimer);
                typingTimer = setTimeout(function() {
                    // تصفير رقم الصفحة إذا كان موجوداً
                    const pageInput = form.querySelector('input[name="page"], input[name="pageIndex"]');
                    if (pageInput) pageInput.value = '1';
                    
                    saveFocusAndSubmit();
                }, doneTypingInterval);
            });

            input.addEventListener('keydown', function () {
                clearTimeout(typingTimer);
            });
        });

        // التعامل مع القوائم المنسدلة (التصفية)
        const selects = form.querySelectorAll('select');
        selects.forEach(select => {
            select.addEventListener('change', function () {
                // تصفير رقم الصفحة إذا كان موجوداً
                const pageInput = form.querySelector('input[name="page"], input[name="pageIndex"]');
                if (pageInput) pageInput.value = '1';
                
                saveFocusAndSubmit();
            });
        });
    });
});
