// student-modals.js

$(document).ready(function () {
    $(document).on('click', '.js-edit-student', function () {
        var btn = $(this);

        $('#EditStudentId').val(btn.attr('data-id'));
        $('#EditFullName').val(btn.attr('data-name'));
        $('#EditPhoneNumber').val(btn.attr('data-phone'));
        $('#EditAge').val(btn.attr('data-age'));

        var dobStr = btn.attr('data-dob');
        $('#EditDateOfBirth').val(dobStr);
        $('#EditDobError').addClass('hidden');
        if (dobStr) {
            var parts = dobStr.split('-');
            if (parts.length === 3) {
                $('#EditDobYear').val(parts[0]);
                $('#EditDobMonth').val(parseInt(parts[1], 10));
                $('#EditDobDay').val(parseInt(parts[2], 10));
            }
        } else {
            $('#EditDobYear').val('');
            $('#EditDobMonth').val('');
            $('#EditDobDay').val('');
        }

        $('#EditGradeId').val(btn.attr('data-grade-id'));

        document.getElementById('editStudentModal').classList.remove('hidden');
    });

    // Excel file input: show file name + enable submit button
    $('#excelFileInput').on('change', function () {
        var fileName = this.files[0] ? this.files[0].name : 'xlsx فقط';
        $('#fileNameDisplay').text(fileName);
        $('#importSubmitBtn').prop('disabled', this.files.length === 0);
    });

    // التاريخ المرجعي العام من إعدادات النظام متاح عبر globalRefDate
    // وهو معرّف في Index.cshtml

    function autoSelectGrade(ageVal, gradeId) {
        if (ageVal === '' || isNaN(ageVal) || !gradeId) return;
        var age = parseInt(ageVal);
        var foundMatch = false;

        $(gradeId + ' option').each(function () {
            if (!$(this).val()) return true; // تخطي الخيار الافتراضي "-- اختر الصف --"

            var minAttr = $(this).attr('data-minage');
            var maxAttr = $(this).attr('data-maxage');

            // إذا كان كلا الحدين فارغين، لا نختار هذا الصف تلقائياً لأن النطاق غير محدد
            if (!minAttr && !maxAttr) return true;

            var min = minAttr ? parseInt(minAttr) : 0;
            var max = maxAttr ? parseInt(maxAttr) : 1000;

            if (age >= min && age <= max) {
                $(gradeId).val($(this).val());
                foundMatch = true;
                return false; // إنهاء اللوب عند إيجاد تطابق
            }
        });

        // إذا لم نجد أي صف يطابق هذا السن الجديد، نقوم بتفريغ الاختيار
        if (!foundMatch) {
            $(gradeId).val('');
        }
    }

    function calcAndSetAge(dobId, ageId, gradeId) {
        var dob = $(dobId).val();
        if (!dob) return;
        var refDate = (window.globalRefDate && window.globalRefDate.trim() !== '') ? new Date(window.globalRefDate) : new Date();
        var birth = new Date(dob);
        var age = refDate.getFullYear() - birth.getFullYear();
        var m = refDate.getMonth() - birth.getMonth();
        if (m < 0 || (m === 0 && refDate.getDate() < birth.getDate())) age--;
        if (age >= 0 && age <= 120) {
            $(ageId).val(age).prop('readonly', true).addClass('bg-slate-50 text-slate-500 cursor-not-allowed');
            autoSelectGrade(age, gradeId);
        }
    }

    function validateAndSetDob(prefix, hiddenId, errorId, ageId, gradeId) {
        var d = $('#' + prefix + 'Day').val();
        var m = $('#' + prefix + 'Month').val();
        var y = $('#' + prefix + 'Year').val();

        $(errorId).addClass('hidden');

        if (d && m && y) {
            var dateObj = new Date(y, m - 1, d);
            // التحقق من أن التاريخ حقيقي (وليس مثل 31 فبراير)
            if (dateObj.getFullYear() == y && dateObj.getMonth() == m - 1 && dateObj.getDate() == d) {
                var formattedStr = y + '-' + m.toString().padStart(2, '0') + '-' + d.toString().padStart(2, '0');
                $(hiddenId).val(formattedStr);
                calcAndSetAge(hiddenId, ageId, gradeId);

                // إزالة خطأ الفورم إن وجد لتمكين الحفظ
                $(this).closest('form').find('button[type="submit"]').prop('disabled', false);
            } else {
                $(errorId).removeClass('hidden');
                $(hiddenId).val('');
                $(ageId).prop('readonly', false).removeClass('bg-slate-50 text-slate-500 cursor-not-allowed');
                $(this).closest('form').find('button[type="submit"]').prop('disabled', true);
            }
        } else {
            $(hiddenId).val('');
            $(ageId).prop('readonly', false).removeClass('bg-slate-50 text-slate-500 cursor-not-allowed');
        }
    }

    $('#DobDay, #DobMonth, #DobYear').on('change keyup', function () {
        validateAndSetDob.call(this, 'Dob', '#DateOfBirth', '#DobError', '#Age', '#GradeId');
    });

    $('#EditDobDay, #EditDobMonth, #EditDobYear').on('change keyup', function () {
        validateAndSetDob.call(this, 'EditDob', '#EditDateOfBirth', '#EditDobError', '#EditAge', '#EditGradeId');
    });

    $('#Age').on('change keyup', function () {
        autoSelectGrade($(this).val(), '#GradeId');
    });

    $('#EditAge').on('change keyup', function () {
        autoSelectGrade($(this).val(), '#EditGradeId');
    });
});
