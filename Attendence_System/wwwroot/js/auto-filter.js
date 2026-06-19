document.addEventListener('DOMContentLoaded', function () {

    // ══════════════════════════════════════════════════════
    //  Arabic Fuzzy Normalization (mirrors server-side C#)
    // ══════════════════════════════════════════════════════
    function normalizeArabic(text) {
        if (!text) return '';
        return text
            .replace(/[أإآٱ]/g, 'ا')   // unify alef variants
            .replace(/ة/g, 'ه')          // teh marbuta → heh
            .replace(/ى/g, 'ي')          // dotless yeh → yeh
            .replace(/[\u064B-\u065F]/g, '') // strip tashkeel diacritics
            .replace(/\s+/g, '')          // remove ALL whitespace
            .toLowerCase();
    }

    function arabicContains(haystack, needle) {
        if (!needle) return true;
        return normalizeArabic(haystack).includes(normalizeArabic(needle));
    }

    // ══════════════════════════════════════════════════════
    //  Restore focus/cursor after form submit (fallback)
    // ══════════════════════════════════════════════════════
    const focusedInputId = sessionStorage.getItem('autoFilter_focusedInputId');
    const focusedInputPosition = sessionStorage.getItem('autoFilter_cursorPosition');
    if (focusedInputId) {
        const inputToFocus = document.getElementById(focusedInputId);
        if (inputToFocus) {
            inputToFocus.focus();
            if (focusedInputPosition) {
                const pos = parseInt(focusedInputPosition, 10);
                setTimeout(() => { inputToFocus.setSelectionRange(pos, pos); }, 0);
            }
        }
        sessionStorage.removeItem('autoFilter_focusedInputId');
        sessionStorage.removeItem('autoFilter_cursorPosition');
    }

    // ══════════════════════════════════════════════════════
    //  Auto-filter Forms
    // ══════════════════════════════════════════════════════
    const autoFilterForms = document.querySelectorAll('.js-auto-filter');

    autoFilterForms.forEach(form => {
        let typingTimer;
        const doneTypingInterval = 800; // fallback debounce (ms)

        function saveFocusAndSubmit() {
            const activeElement = document.activeElement;
            if (activeElement && activeElement.tagName === 'INPUT' && activeElement.id) {
                sessionStorage.setItem('autoFilter_focusedInputId', activeElement.id);
                sessionStorage.setItem('autoFilter_cursorPosition', activeElement.selectionStart);
            }
            form.submit();
        }

        // ── Resolve live-filter target table ────────────
        const liveTargetSelector = form.dataset.liveTarget;
        const liveTable = liveTargetSelector ? document.querySelector(liveTargetSelector) : null;
        const liveRows  = liveTable ? Array.from(liveTable.querySelectorAll('tbody tr[data-search]')) : null;
        const emptyRow  = liveTable ? liveTable.querySelector('tbody tr.js-empty-row') : null;

        function applyLiveFilter(value) {
            if (!liveRows) return;
            const q = value.trim();
            let visible = 0;
            liveRows.forEach(row => {
                const match = arabicContains(row.dataset.search, q);
                row.style.display = match ? '' : 'none';
                if (match) visible++;
            });
            if (emptyRow) {
                emptyRow.style.display = (visible === 0 && q !== '') ? '' : 'none';
            }
        }

        // ── Text Inputs ──────────────────────────────────
        const textInputs = form.querySelectorAll('input[type="text"], input[type="search"]');
        textInputs.forEach(input => {
            // Apply on page load for preserved search values
            if (input.value) applyLiveFilter(input.value);

            input.addEventListener('input', function () {
                if (liveRows) {
                    // ✨ FULLY LIVE — instant client-side row filtering
                    applyLiveFilter(this.value);
                } else {
                    // Fallback: debounced server submit
                    clearTimeout(typingTimer);
                    typingTimer = setTimeout(() => {
                        const pageInput = form.querySelector('input[name="page"], input[name="pageIndex"]');
                        if (pageInput) pageInput.value = '1';
                        saveFocusAndSubmit();
                    }, doneTypingInterval);
                }
            });

            input.addEventListener('keydown', function (e) {
                if (!liveRows) clearTimeout(typingTimer);
                // Enter key submits form to persist filter in URL
                if (e.key === 'Enter') {
                    e.preventDefault();
                    const pageInput = form.querySelector('input[name="page"], input[name="pageIndex"]');
                    if (pageInput) pageInput.value = '1';
                    saveFocusAndSubmit();
                }
                // Ignore arrow keys
                if (['ArrowUp','ArrowDown','ArrowLeft','ArrowRight','Tab'].includes(e.key)) return;
            });
        });

        // ── Selects — always server-submit immediately ───
        const selects = form.querySelectorAll('select');
        selects.forEach(select => {
            select.addEventListener('change', function () {
                const pageInput = form.querySelector('input[name="page"], input[name="pageIndex"]');
                if (pageInput) pageInput.value = '1';
                saveFocusAndSubmit();
            });
        });
    });
});
