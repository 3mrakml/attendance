using Attendence_System.Data;
using Attendence_System.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Attendence_System.Services
{
    public class ImportService : IImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISystemSettingService _settingService;

        // الأسماء المقبولة لكل عمود (عربي وإنجليزي)
        private static readonly string[] NameColumns = { "الاسم", "الاسم بالكامل", "الاسم الكامل", "name", "fullname", "full name" };
        private static readonly string[] PhoneColumns = { "الهاتف", "رقم الهاتف", "موبايل", "phone", "mobile", "phonenumber" };
        private static readonly string[] AgeColumns = { "السن", "العمر", "age" };
        private static readonly string[] GradeColumns = { "الصف", "الفرقة", "المستوى", "الصف/الفرقة", "grade", "class" };
        private static readonly string[] DobColumns = { "تاريخ الميلاد", "ميلاد", "dob", "birthdate", "date of birth", "dateofbirth" };

        public ImportService(ApplicationDbContext context, ISystemSettingService settingService)
        {
            _context = context;
            _settingService = settingService;
        }

        public async Task<ImportStudentResult> ImportStudentsFromExcelAsync(IFormFile file, string tenantId)
        {
            var result = new ImportStudentResult();

            // جلب الصفوف الموجودة مسبقاً لهذا الـ Tenant
            var grades = await _context.Grades.ToListAsync();

            // جلب بيانات الطلاب الأساسية لتفادي التكرار التام
            var existingStudents = await _context.Students
                .Select(s => new { s.FullName, s.PhoneNumber, s.GradeId, s.Age, s.DateOfBirth })
                .ToListAsync();

            var studentsToAdd = new List<Student>();
            
            // قاموس لتتبع التسلسل لكل صف دراسي
            var gradeSequenceTracker = new Dictionary<int, int>();

            // نسخ الملف إلى MemoryStream لأن ClosedXML يحتاج Seek
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            using var workbook = new XLWorkbook(ms);
            var ws = workbook.Worksheets.First();

            // قراءة رأس الجدول (الصف الأول)
            var headerRow = ws.Row(1);
            int nameCol = -1, phoneCol = -1, ageCol = -1, gradeCol = -1, dobCol = -1;

            foreach (var cell in headerRow.CellsUsed())
            {
                var header = GetCellString(cell).ToLower().Trim();
                int col = cell.Address.ColumnNumber;

                if (NameColumns.Contains(header)) nameCol = col;
                else if (PhoneColumns.Contains(header)) phoneCol = col;
                else if (AgeColumns.Contains(header)) ageCol = col;
                else if (GradeColumns.Contains(header)) gradeCol = col;
                else if (DobColumns.Contains(header)) dobCol = col;
            }

            // لو مفيش عمود اسم — نفترض العمود الأول هو الاسم
            if (nameCol == -1)
            {
                var firstUsedCell = headerRow.CellsUsed().FirstOrDefault();
                if (firstUsedCell != null)
                    nameCol = firstUsedCell.Address.ColumnNumber;
                else
                {
                    result.Errors.Add(new ImportStudentError
                    {
                        Label = "الملف",
                        Reason = "الملف فارغ أو لا يحتوي على بيانات."
                    });
                    return result;
                }
            }

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            for (int row = 2; row <= lastRow; row++)
            {
                // تخطي الصفوف الفارغة كلياً
                if (ws.Row(row).IsEmpty()) continue;

                var name = nameCol > 0 ? GetCellString(ws.Cell(row, nameCol)) : "";
                var label = string.IsNullOrWhiteSpace(name) ? $"الصف رقم {row}" : name;

                // التحقق: الاسم مطلوب
                if (string.IsNullOrWhiteSpace(name))
                {
                    result.Errors.Add(new ImportStudentError { Label = label, Reason = "الاسم فارغ" });
                    continue;
                }

                // رقم الهاتف
                string? phone = phoneCol > 0 ? GetCellString(ws.Cell(row, phoneCol)) : null;
                if (string.IsNullOrWhiteSpace(phone)) phone = null;
                // السن
                int age = 0;
                DateOnly? dob = null;

                // تاريخ الميلاد (أولوية على السن اليدوي)
                if (dobCol > 0)
                {
                    var dobStr = GetCellString(ws.Cell(row, dobCol));
                    if (!string.IsNullOrWhiteSpace(dobStr))
                    {
                        // ClosedXML بيرجع التاريخ أحياناً كـ DateTime string
                        if (DateOnly.TryParse(dobStr, out DateOnly parsedDob))
                        {
                            dob = parsedDob;
                            // احسب السن باستخدام التاريخ المرجعي العام من الإعدادات
                            var refDateStr = await _settingService.GetSettingAsync("AgeReferenceDate", "");
                            DateOnly refDate = string.IsNullOrEmpty(refDateStr) || !DateOnly.TryParse(refDateStr, out DateOnly parsedRef)
                                ? DateOnly.FromDateTime(Attendence_System.Helpers.AppTime.Today)
                                : parsedRef;
                            int years = refDate.Year - dob.Value.Year;
                            if (refDate < dob.Value.AddYears(years)) years--;
                            age = years;
                        }
                    }
                }

                // لو مفيش تاريخ ميلاد، خذ السن اليدوي
                if (dob == null && ageCol > 0)
                {
                    var ageStr = GetCellString(ws.Cell(row, ageCol));
                    int.TryParse(ageStr, out age);
                }

                // الصف
                int gradeId = 0;
                if (gradeCol > 0)
                {
                    var gradeName = GetCellString(ws.Cell(row, gradeCol));
                    if (!string.IsNullOrWhiteSpace(gradeName))
                    {
                        var normalizedInputGrade = NormalizeArabicText(gradeName).Trim();
                        var matchedGrade = grades.FirstOrDefault(g =>
                            NormalizeArabicText(g.Name).Trim().Equals(normalizedInputGrade, StringComparison.OrdinalIgnoreCase));
                        if (matchedGrade != null)
                        {
                            gradeId = matchedGrade.GradeId;
                        }
                        else
                        {
                            // اسم الصف موجود في الملف لكن غير موجود في النظام
                            result.Errors.Add(new ImportStudentError
                            {
                                Label = name,
                                Reason = $"الصف '{gradeName}' غير موجود في النظام",
                                FullName = name,
                                Phone = phone,
                                Age = age > 0 ? age : null,
                                DateOfBirth = dob.HasValue ? dob.Value.ToString("yyyy-MM-dd") : null
                            });
                            continue;
                        }
                    }
                    else
                    {
                        // خلية الصف فارغة — ارفض إضافة الطالب
                        result.Errors.Add(new ImportStudentError
                        {
                            Label = name,
                            Reason = "خلية الصف فارغة. يرجى تحديد الصف.",
                            FullName = name,
                            Phone = phone,
                            Age = age > 0 ? age : null,
                            DateOfBirth = dob.HasValue ? dob.Value.ToString("yyyy-MM-dd") : null
                        });
                        continue;
                    }
                }
                else
                {
                    // لا يوجد عمود صف في الملف — ارفض إضافة الطالب
                    result.Errors.Add(new ImportStudentError
                    {
                        Label = name,
                        Reason = "لا يوجد عمود للصفوف في الملف المرفوع."
                    });
                    continue;
                }

                // إذا لم يتم العثور على أي صف في النظام
                if (gradeId == 0)
                {
                    result.Errors.Add(new ImportStudentError
                    {
                        Label = name,
                        Reason = "لا يوجد صف دراسي في النظام. أضف صفاً أولاً."
                    });
                    continue;
                }

                // التحقق من التكرار التام (نفس الاسم، الهاتف، الصف، العمر وتاريخ الميلاد)
                var isDuplicateDB = existingStudents.Any(s =>
                    (s.FullName ?? "").Trim().Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    s.PhoneNumber == phone &&
                    s.GradeId == gradeId &&
                    s.Age == age &&
                    s.DateOfBirth == dob);

                var isDuplicateInFile = studentsToAdd.Any(s =>
                    (s.FullName ?? "").Trim().Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    s.PhoneNumber == phone &&
                    s.GradeId == gradeId &&
                    s.Age == age &&
                    s.DateOfBirth == dob);

                if (isDuplicateDB || isDuplicateInFile)
                {
                    result.Errors.Add(new ImportStudentError
                    {
                        Label = name,
                        Reason = "طالب مكرر (تطابق تام في الاسم، الهاتف، الصف، والعمر)",
                        FullName = name,
                        Phone = phone,
                        Age = age > 0 ? age : null,
                        GradeId = gradeId,
                        GradeName = grades.FirstOrDefault(g => g.GradeId == gradeId)?.Name,
                        DateOfBirth = dob.HasValue ? dob.Value.ToString("yyyy-MM-dd") : null
                    });
                    continue;
                }

                // توليد QRToken فريد ومتسلسل حسب الصف
                string token = await GenerateSequentialQRTokenAsync(gradeId, gradeSequenceTracker, studentsToAdd);

                studentsToAdd.Add(new Student
                {
                    FullName = name,
                    PhoneNumber = phone,
                    Age = age,
                    DateOfBirth = dob,
                    GradeId = gradeId,
                    QRToken = token,
                    TenantId = tenantId
                });

                // تمت إزالة التحقق من الهاتف المكرر
            }

            // حفظ الكل في batch واحدة
            if (studentsToAdd.Any())
            {
                _context.Students.AddRange(studentsToAdd);
                await _context.SaveChangesAsync();
                result.AddedCount = studentsToAdd.Count;
            }

            return result;
        }

        /// <summary>يقرأ قيمة الخلية بشكل آمن سواء كانت نص أو رقم أو تاريخ</summary>
        private static string GetCellString(IXLCell cell)
        {
            if (cell == null || cell.IsEmpty())
                return string.Empty;

            try
            {
                return cell.CachedValue.ToString()?.Trim() ?? cell.GetString().Trim();
            }
            catch
            {
                return cell.GetString().Trim();
            }
        }

        private async Task<string> GenerateSequentialQRTokenAsync(int gradeId, Dictionary<int, int> gradeSequenceTracker, List<Student> studentsToAdd)
        {
            var grade = await _context.Grades.FindAsync(gradeId);
            string prefix = grade?.Code > 0 ? grade.Code.ToString() : gradeId.ToString();
            int expectedLength = prefix.Length + 3;

            if (!gradeSequenceTracker.ContainsKey(gradeId))
            {
                var existingTokens = await _context.Students
                    .Where(s => s.GradeId == gradeId && s.QRToken.StartsWith(prefix) && s.QRToken.Length == expectedLength)
                    .Select(s => s.QRToken)
                    .ToListAsync();
                    
                int maxSeq = 0;
                foreach (var t in existingTokens)
                {
                    if (int.TryParse(t.Substring(prefix.Length), out int seq))
                    {
                        if (seq > maxSeq) maxSeq = seq;
                    }
                }
                gradeSequenceTracker[gradeId] = maxSeq;
            }

            string newToken;
            while (true)
            {
                gradeSequenceTracker[gradeId]++;
                int nextSeq = gradeSequenceTracker[gradeId];
                newToken = $"{gradeId}{nextSeq:D3}";
                
                // التأكد من عدم التكرار (لتفادي أي مشاكل لو كان في رقم مسجل يدوياً بنفس الصيغة)
                if (!studentsToAdd.Any(s => s.QRToken == newToken) && !await _context.Students.AnyAsync(s => s.QRToken == newToken))
                {
                    break;
                }
            }

            return newToken;
        }

        /// <summary>توحيد النصوص العربية لتجاهل الفروقات في الهمزات والتاء المربوطة</summary>
        private static string NormalizeArabicText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            
            return text
                .Replace("أ", "ا")
                .Replace("إ", "ا")
                .Replace("آ", "ا")
                .Replace("ة", "ه")
                .Replace("ى", "ي");
        }


        public byte[] GenerateStudentTemplate(IEnumerable<string> gradeNames)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("الطلاب");

            // رأس الجدول
            ws.Cell(1, 1).Value = "الاسم بالكامل";
            ws.Cell(1, 2).Value = "السن";
            ws.Cell(1, 3).Value = "رقم الهاتف";
            ws.Cell(1, 4).Value = "الصف";

            // تنسيق الرأس
            var headerRow = ws.Range(1, 1, 1, 4);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#3B82F6");
            headerRow.Style.Font.FontColor = XLColor.White;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // صف مثال
            ws.Cell(2, 1).Value = "أحمد محمد علي";
            ws.Cell(2, 2).Value = 20;
            ws.Cell(2, 3).Value = "01012345678";
            ws.Cell(2, 4).Value = gradeNames.FirstOrDefault() ?? "";

            // تنسيق الأعمدة
            ws.Column(1).Width = 30;
            ws.Column(2).Width = 10;
            ws.Column(3).Width = 18;
            ws.Column(4).Width = 20;

            ws.RightToLeft = true;

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
