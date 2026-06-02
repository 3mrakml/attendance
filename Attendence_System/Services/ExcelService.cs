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
            var headerRow = worksheet.Row(1);
            headerRow.Height = 25; // Taller header
            
            for (int i = 0; i < headers.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b"); // Slate 800
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#cbd5e1"); // Slate 300
            }

            // Write Data
            for (int r = 0; r < dataList.Count; r++)
            {
                var item = dataList[r];
                var row = worksheet.Row(r + 2);
                row.Height = 20;

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
                        // Ensure numbers are treated as numbers in Excel for proper formatting/sorting if they are purely numeric types,
                        // but ToString() was used before. Let's try to pass the value directly if it's numeric, otherwise string.
                        if (value is double || value is float || value is decimal || value is int || value is long)
                        {
                            cell.Value = Convert.ToDouble(value);
                        }
                        else
                        {
                            cell.Value = value.ToString();
                        }
                    }
                    else
                    {
                        cell.Value = value.ToString();
                    }

                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#e2e8f0"); // Slate 200

                    // Alternating row colors
                    if (r % 2 == 0)
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.White;
                    }
                    else
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc"); // Slate 50
                    }
                }
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
