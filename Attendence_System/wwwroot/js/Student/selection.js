// student-selection.js

document.addEventListener('DOMContentLoaded', function () {
    // Checkbox and selection logic
    const btnToggleSelection = document.getElementById('btnToggleSelection');
    const selectionCols = document.querySelectorAll('.selection-col');
    const selectAllCheckbox = document.getElementById('selectAllCheckbox');
    const studentCheckboxes = document.querySelectorAll('.student-checkbox');
    const btnPrintSelected = document.getElementById('btnPrintSelected');
    const selectedCountSpan = document.getElementById('selectedCount');

    if (btnToggleSelection) {
        btnToggleSelection.addEventListener('click', function () {
            const isHidden = btnPrintSelected.classList.contains('hidden');
            if (isHidden) {
                btnPrintSelected.classList.remove('hidden');
                selectionCols.forEach(col => col.classList.remove('hidden'));
                this.classList.add('bg-primary/10', 'text-primary', 'border-primary/20');
            } else {
                btnPrintSelected.classList.add('hidden');
                selectionCols.forEach(col => col.classList.add('hidden'));
                this.classList.remove('bg-primary/10', 'text-primary', 'border-primary/20');

                // Uncheck everything when hiding
                if (selectAllCheckbox) selectAllCheckbox.checked = false;
                studentCheckboxes.forEach(cb => cb.checked = false);
                updatePrintButtonState();
            }
        });
    }

    function updatePrintButtonState() {
        const checkedCount = document.querySelectorAll('.student-checkbox:checked').length;
        if (selectedCountSpan) selectedCountSpan.textContent = checkedCount;
        if (checkedCount > 0) {
            btnPrintSelected.removeAttribute('disabled');
        } else {
            if (btnPrintSelected) btnPrintSelected.setAttribute('disabled', 'disabled');
        }
    }

    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function () {
            studentCheckboxes.forEach(cb => {
                cb.checked = this.checked;
            });
            updatePrintButtonState();
        });
    }

    studentCheckboxes.forEach(cb => {
        cb.addEventListener('change', function () {
            const allChecked = document.querySelectorAll('.student-checkbox:checked').length === studentCheckboxes.length;
            if (selectAllCheckbox) selectAllCheckbox.checked = allChecked;
            updatePrintButtonState();
        });
    });
});
