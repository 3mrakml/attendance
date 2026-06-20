// student-import-alerts.js

// ── Import Error Helpers ───────────────────────────────────────
// عند submit فورم إضافة طالب: احفظ الاسم ليتم حذفه من localStorage بعد التحميل
function onAddStudentSubmit() {
    if (window._fixingLi) {
        var nameEl = window._fixingLi.querySelector('span.font-semibold');
        if (nameEl) {
            sessionStorage.setItem('importFixedLabel', nameEl.textContent.trim());
        }
        window._fixingLi = null;
    }
}

// عند تحميل الصفحة: هل في طالب تم حله للتوفيقِ؟
(function removeFixedFromStorage() {
    var fixedLabel = sessionStorage.getItem('importFixedLabel');
    if (!fixedLabel) return;
    sessionStorage.removeItem('importFixedLabel');
    var raw = localStorage.getItem('importResult');
    if (!raw) return;
    try {
        var data = JSON.parse(raw);
        if (data && data.errors) {
            data.errors = data.errors.filter(function (e) {
                return (e.Label || '').trim() !== fixedLabel;
            });
            if (data.errors.length === 0 && data.added === 0) {
                localStorage.removeItem('importResult');
            } else {
                localStorage.setItem('importResult', JSON.stringify(data));
            }
        }
    } catch (e) { }
})();

// متغيّر عام لتتبع العنصر الجاري معالجته
window._fixingLi = null;

function fixImportError(btn, name, phone, age, gradeId, dob) {
    // حفظ مرجع الصف المراد معالجته (لا نمسحه الآن)
    window._fixingLi = btn.closest('li');
    if (window._fixingLi) window._fixingLi.style.opacity = '0.5';

    // تعبئة فورم إضافة طالب
    document.getElementById('FullName').value = name || '';
    document.getElementById('PhoneNumber').value = phone || '';
    document.getElementById('Age').value = age || '';

    // اختيار الصف
    if (gradeId) {
        const gradeSelect = document.getElementById('GradeId');
        if (gradeSelect) gradeSelect.value = gradeId;
    }

    // تاريخ الميلاد
    if (dob) {
        const parts = dob.split('-');
        if (parts.length === 3) {
            const dobYEl = document.getElementById('DobYear');
            const dobMEl = document.getElementById('DobMonth');
            const dobDEl = document.getElementById('DobDay');
            if (dobYEl) dobYEl.value = parts[0];
            if (dobMEl) dobMEl.value = parseInt(parts[1], 10);
            if (dobDEl) dobDEl.value = parseInt(parts[2], 10);
            document.getElementById('DateOfBirth').value = dob;
        }
    }

    // فتح مودال الإضافة
    document.getElementById('addStudentModal').classList.remove('hidden');
}

document.addEventListener('DOMContentLoaded', function () {
    // عند إغلاق مودال الإضافة بدون حل: أعد الصف لوضعه الطبيعي
    const addStudentModal = document.getElementById('addStudentModal');
    if (addStudentModal) {
        addStudentModal.addEventListener('click', function (e) {
            // أي زر إغلاق داخل المودال (backdrop أو زر X)
            if (e.target === this || e.target.closest('button[onclick*="classList.add\'hidden\'"]')) {
                if (window._fixingLi) {
                    window._fixingLi.style.opacity = '1';
                    window._fixingLi = null;
                }
            }
        });
    }
});

function checkImportListEmpty() {
    // حفظ القائمة المتبقية في localStorage
    const list = document.getElementById('importErrorsList');
    if (!list) return;
    const remainingLis = list.querySelectorAll('li');
    if (remainingLis.length === 0) {
        localStorage.removeItem('importResult');
        const box = document.getElementById('importAlertBox');
        if (box) box.remove();
    } else {
        // حدّث localStorage ليعكس ما تبقى
        try {
            var stored = JSON.parse(localStorage.getItem('importResult') || '{}');
            var remaining = [];
            remainingLis.forEach(function (li) {
                var nameEl = li.querySelector('span.font-semibold');
                var reasonEl = li.querySelector('span.text-slate-600');
                if (nameEl) {
                    remaining.push({
                        Label: nameEl.textContent.trim(),
                        Reason: reasonEl ? reasonEl.textContent.trim() : '',
                        // نحتفظ بالبيانات الكاملة من dataset
                        FullName: li.dataset.fname || '',
                        Phone: li.dataset.phone || '',
                        Age: li.dataset.age || null,
                        GradeId: li.dataset.gradeid || null,
                        DateOfBirth: li.dataset.dob || null,
                    });
                }
            });
            stored.errors = remaining;
            localStorage.setItem('importResult', JSON.stringify(stored));
        } catch (e) { }
    }
}

// ── رسم alert الاستيراد من localStorage ───────────────────────
function renderImportAlert() {
    var raw = localStorage.getItem('importResult');
    if (!raw) return;
    var data;
    try { data = JSON.parse(raw); } catch (e) { return; }
    if (!data || (data.added === 0 && (!data.errors || data.errors.length === 0))) {
        localStorage.removeItem('importResult');
        return;
    }

    var errors = data.errors || [];
    var errorsHtml = '';
    if (errors.length > 0) {
        var lis = errors.map(function (err) {
            var fname = (err.FullName || '').replace(/'/g, "\\'");
            var phone = (err.Phone || '').replace(/'/g, "\\'");
            var age = err.Age || '';
            var gid = err.GradeId || '';
            var dob = err.DateOfBirth || '';
            var fixBtn = fname ? `<button type="button"
                onclick="fixImportError(this,'${fname}','${phone}','${age}','${gid}','${dob}')"
                class="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg text-xs font-semibold bg-primary/10 text-primary hover:bg-primary/20 transition-colors">
                <i data-lucide="pencil" class="w-3 h-3"></i> حل المشكلة
            </button>` : '';
            return `<li class="flex items-center justify-between gap-2 bg-white border border-amber-100 rounded-xl px-4 py-2"
                       data-fname="${err.FullName || ''}" data-phone="${err.Phone || ''}" data-age="${age}" data-gradeid="${gid}" data-dob="${dob}">
                <div class="flex items-center gap-2 flex-1 min-w-0">
                    <span class="w-1.5 h-1.5 rounded-full bg-amber-400 flex-shrink-0"></span>
                    <span class="font-semibold truncate">${escHtml(err.Label || '')}</span>
                    <span class="text-slate-400">—</span>
                    <span class="text-slate-600 truncate">${escHtml(err.Reason || '')}</span>
                </div>
                <div class="flex items-center gap-2 flex-shrink-0">
                    ${fixBtn}
                    <button type="button" onclick="this.closest('li').remove(); checkImportListEmpty();"
                        class="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg text-xs font-semibold bg-slate-100 text-slate-500 hover:bg-slate-200 transition-colors">
                        <i data-lucide="x" class="w-3 h-3"></i> تجاهل
                    </button>
                </div>
            </li>`;
        }).join('');

        errorsHtml = `<div class="border-t border-emerald-200 pt-3">
            <p class="font-semibold text-amber-700 flex items-center gap-2 mb-2">
                <i data-lucide="triangle-alert" class="w-4 h-4"></i>
                تم تخطي ${errors.length} طالب:
            </p>
            <ul id="importErrorsList" class="space-y-2 text-sm">${lis}</ul>
        </div>`;
    }

    var html = `<div id="importAlertBox" class="bg-emerald-50 border border-emerald-200 rounded-2xl p-4 space-y-3 relative">
        <button onclick="localStorage.removeItem('importResult'); document.getElementById('importAlertBox').remove();"
            class="absolute top-4 left-4 text-emerald-500 hover:text-emerald-700 transition-colors">
            <i data-lucide="x" class="w-5 h-5"></i>
        </button>
        <div class="flex items-center gap-3">
            <i data-lucide="circle-check" class="w-5 h-5 text-emerald-600 flex-shrink-0"></i>
            <span class="font-bold text-emerald-800">تم إضافة ${data.added} طالب بنجاح.</span>
        </div>
        ${errorsHtml}
    </div>`;

    var container = document.getElementById('importAlertContainer');
    if (container) {
        container.innerHTML = html;
        if (window.lucide) lucide.createIcons();
    }
}

function escHtml(str) {
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

// استدعاء renderImportAlert بعد تحميل lucide
document.addEventListener('DOMContentLoaded', function () {
    renderImportAlert();
});
