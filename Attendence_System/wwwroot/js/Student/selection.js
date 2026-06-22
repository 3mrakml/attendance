// student-selection.js

document.addEventListener('DOMContentLoaded', function () {
    function updatePrintButtonState() {
        const btnPrintSelected = document.getElementById('btnPrintSelected');
        const selectedCountSpan = document.getElementById('selectedCount');
        const checkedCount = document.querySelectorAll('.student-checkbox:checked').length;
        if (selectedCountSpan) selectedCountSpan.textContent = checkedCount;
        if (checkedCount > 0) {
            if (btnPrintSelected) btnPrintSelected.removeAttribute('disabled');
        } else {
            if (btnPrintSelected) btnPrintSelected.setAttribute('disabled', 'disabled');
        }
    }

    document.addEventListener('change', function(e) {
        if (e.target.matches('#selectAllCheckbox')) {
            const studentCheckboxes = document.querySelectorAll('.student-checkbox');
            studentCheckboxes.forEach(cb => {
                cb.checked = e.target.checked;
            });
            updatePrintButtonState();
        } else if (e.target.matches('.student-checkbox')) {
            const studentCheckboxes = document.querySelectorAll('.student-checkbox');
            const allChecked = document.querySelectorAll('.student-checkbox:checked').length === studentCheckboxes.length && studentCheckboxes.length > 0;
            const selectAllCheckbox = document.getElementById('selectAllCheckbox');
            if (selectAllCheckbox) selectAllCheckbox.checked = allChecked;
            updatePrintButtonState();
        }
    });

    document.addEventListener('click', function(e) {
        const btnToggleSelection = e.target.closest('#btnToggleSelection');
        if (btnToggleSelection) {
            const btnPrintSelected = document.getElementById('btnPrintSelected');
            const selectionCols = document.querySelectorAll('.selection-col');
            const isHidden = btnPrintSelected && btnPrintSelected.classList.contains('hidden');
            
            if (isHidden) {
                if (btnPrintSelected) btnPrintSelected.classList.remove('hidden');
                selectionCols.forEach(col => col.classList.remove('hidden'));
                btnToggleSelection.classList.add('bg-primary/10', 'text-primary', 'border-primary/20');
            } else {
                if (btnPrintSelected) btnPrintSelected.classList.add('hidden');
                selectionCols.forEach(col => col.classList.add('hidden'));
                btnToggleSelection.classList.remove('bg-primary/10', 'text-primary', 'border-primary/20');

                const selectAllCheckbox = document.getElementById('selectAllCheckbox');
                if (selectAllCheckbox) selectAllCheckbox.checked = false;
                document.querySelectorAll('.student-checkbox').forEach(cb => cb.checked = false);
                updatePrintButtonState();
            }
        }
    });
});
