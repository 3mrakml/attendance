$(document).ready(function () {
    $(document).on('click', '.js-delete', function () {
        var btn = $(this);
        var id = btn.data('id');
        var type = btn.data('type');
        var deleteUrl;

        switch (type) {
            case 'student':
                deleteUrl = `/Student/Delete/${id}`;
                break;
            case 'course':
                deleteUrl = `/Course/DeleteCourse/${id}`;
                break;
            case 'lecture':
                deleteUrl = `/Course/DeleteLecture/${id}`;
                break;
            case 'grade':
                deleteUrl = `/Grade/Delete/${id}`;
                break;
            default:
                console.error('نوع العنصر غير معروف.');
                return;
        }

        const swal = Swal.mixin({
            customClass: {
                confirmButton: 'bg-rose-600 hover:bg-rose-700 text-white font-bold py-2.5 px-6 rounded-xl mx-2 shadow-sm transition-colors',
                cancelButton: 'bg-slate-100 hover:bg-slate-200 text-slate-700 font-bold py-2.5 px-6 rounded-xl mx-2 transition-colors',
                popup: 'rounded-2xl shadow-xl border border-slate-100'
            },
            buttonsStyling: false
        });

        swal.fire({
            title: `هل أنت متأكد أنك تريد حذف هذا ${getTypeName(type)}؟`,
            text: "لن تتمكن من التراجع عن هذا الإجراء!",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'نعم، احذفه!',
            cancelButtonText: 'لا، إلغاء!',
            reverseButtons: true
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    url: deleteUrl,
                    method: 'DELETE',
                    headers: {
                        'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                    },
                    success: function (response) {
                        if (response && response.success === false) {
                            swal.fire(
                                'عذراً!',
                                response.message || 'لا يمكن الحذف بسبب وجود بيانات مرتبطة.',
                                'error'
                            );
                            return;
                        }
                        
                        swal.fire(
                            'تم الحذف!',
                            `تم حذف ${getTypeName(type)} بنجاح.`,
                            'success'
                        ).then(() => {
                            btn.closest('tr, .js-delete-parent').fadeOut(300, function () {
                                $(this).remove();
                            });
                            // fallback: remove closest card-like div if not in a table
                            if (!btn.closest('tr').length) {
                                btn.closest('div[data-deletable]').fadeOut(300, function () {
                                    $(this).remove();
                                });
                            }
                        });
                    },
                    error: function () {
                        swal.fire(
                            'خطأ!',
                            'حدث خطأ أثناء عملية الحذف.',
                            'error'
                        );
                    }
                });
            }
        });

        function getTypeName(type) {
            switch (type) {
                case 'student': return 'الطالب';
                case 'course':  return 'المادة';
                case 'lecture': return 'المحاضرة';
                case 'grade':   return 'الصف';
                default:        return '';
            }
        }
    });
});
