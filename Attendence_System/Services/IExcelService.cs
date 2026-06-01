using System;
using System.Collections.Generic;

namespace Attendence_System.Services
{
    public interface IExcelService
    {
        /// <summary>
        /// Exports a list of objects to an Excel byte array.
        /// If columnMappings is provided, it dictates the columns and values.
        /// Otherwise, it uses Reflection to export all public properties.
        /// </summary>
        byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Data", Dictionary<string, Func<T, object>> columnMappings = null);
    }
}
