using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Attendence_System.Services
{
    public class ExcelService : IExcelService
    {
        public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Data", Dictionary<string, Func<T, object>> columnMappings = null)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            // Configure for RTL
            worksheet.RightToLeft = true;

            var dataList = data.ToList();
            if (dataList.Count == 0)
            {
                worksheet.Cell(1, 1).Value = "لا توجد بيانات";
                using var emptyStream = new MemoryStream();
                workbook.SaveAs(emptyStream);
                return emptyStream.ToArray();
            }

            // Determine Headers and Value extractors
            var headers = new List<string>();
            var valueExtractors = new List<Func<T, object>>();

            if (columnMappings != null && columnMappings.Any())
            {
                foreach (var mapping in columnMappings)
                {
                    headers.Add(mapping.Key);
                    valueExtractors.Add(mapping.Value);
                }
            }
            else
            {
                // Fallback to Reflection
                var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    headers.Add(prop.Name);
                    valueExtractors.Add(item => prop.GetValue(item));
                }
            }

            // Write Headers
            for (int i = 0; i < headers.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Write Data
            for (int r = 0; r < dataList.Count; r++)
            {
                var item = dataList[r];
                for (int c = 0; c < headers.Count; c++)
                {
                    var cell = worksheet.Cell(r + 2, c + 1);
                    var value = valueExtractors[c](item);
                    
                    if (value == null)
                    {
                        cell.Value = "";
                    }
                    else if (value is DateTime dt)
                    {
                        cell.Value = dt.ToString("yyyy-MM-dd HH:mm tt");
                    }
                    else if (value.GetType().IsPrimitive || value is string || value is decimal)
                    {
                        cell.Value = value.ToString();
                    }
                    else
                    {
                        // Fallback for complex objects
                        cell.Value = value.ToString();
                    }

                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
