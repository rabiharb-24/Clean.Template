using System.ComponentModel;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using OfficeOpenXml;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace Application.Common.Extensions;

public static class ApplicationExtensions
{
    public static string GetDescription(this Enum enumValue)
    {
        FieldInfo? field = enumValue.GetType().GetField(enumValue.ToString());
        if (field is null)
        {
            return string.Empty;
        }

        if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
        {
            return attribute.Description;
        }

        return string.Empty;
    }

    public static ExcelPackage GenerateExcel<T>(
       this IEnumerable<T> data,
       IEnumerable<ExcelColumnDto> columns,
       string title)
       where T : class
    {
        ExcelPackage.LicenseContext = LicenseContext.Commercial;
        ExcelPackage package = new();
        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(title);

        List<ExcelColumnDto> columnsList = columns.ToList();
        IList<T> dataList = data as IList<T> ?? data.ToList();

        // --- Cache columns for faster access ---
        Dictionary<string, PropertyInfo?> propertyCache = new();
        foreach (ExcelColumnDto column in columnsList)
        {
            propertyCache[column.Header] = GetNestedPropertyInfo(typeof(T), column.Header);
        }

        // --- Write header row ---
        for (int i = 0; i < columnsList.Count; i++)
        {
            ExcelRange cell = worksheet.Cells[1, i + 1];
            cell.Value = columnsList[i].DisplayName;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            worksheet.Column(i + 1).Width = columnsList[i].Width > 0 ? columnsList[i].Width : 20;
        }

        // --- Write data rows ---
        for (int rowIndex = 0; rowIndex < dataList.Count; rowIndex++)
        {
            T row = dataList[rowIndex];
            for (int colIndex = 0; colIndex < columnsList.Count; colIndex++)
            {
                string columnKey = columnsList[colIndex].Header;
                object? value = GetNestedPropertyValue(row, columnKey);
                worksheet.Cells[rowIndex + 2, colIndex + 1].Value = value?.ToString() ?? string.Empty;
            }
        }

        // --- Apply table border ---
        ExcelRange tableRange = worksheet.Cells[1, 1, dataList.Count + 1, columnsList.Count];
        tableRange.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
        tableRange.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
        tableRange.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
        tableRange.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

        return package;
    }

    /// <summary>
    /// Replace tokens in email body with values in dictionary.
    /// </summary>
    /// <param name="message">Message including tokens.</param>
    /// <param name="tokens">Dictionary of token keys with replacements.</param>
    /// <returns>Replaced tokens body string.</returns>
    public static string ReplaceEmailTokens(this string message, Dictionary<string, string> tokens)
    {
        if (tokens is null)
        {
            return message;
        }

        StringBuilder content = new(message);
        foreach (KeyValuePair<string, string> k in tokens)
        {
            content = content.Replace($"[[{k.Key}]]", k.Value);
        }

        return content.ToString();
    }

    public static Result ToApplicationResult(this IdentityResult result)
    {
        IEnumerable<Error> applicationErrors = result.Errors.Select(x =>
        {
            return new Error(x.Description);
        });

        return new Result(result.Succeeded, applicationErrors, result.Succeeded ? StatusCodes.Status400BadRequest : StatusCodes.Status200OK);
    }

    #region Private Methods
    private static PropertyInfo? GetNestedPropertyInfo(Type type, string propertyPath)
    {
        string[] parts = propertyPath.Split('.');
        PropertyInfo? property = null;

        foreach (string part in parts)
        {
            property = type.GetProperty(part);
            if (property is null)
            {
                return null;
            }

            type = property.PropertyType;
        }

        return property;
    }

    private static object? GetNestedPropertyValue(object? obj, string propertyPath)
    {
        if (obj is null)
        {
            return null;
        }

        string[] parts = propertyPath.Split('.');
        object? current = obj;

        foreach (string part in parts)
        {
            if (current is null)
            {
                return null;
            }

            PropertyInfo? prop = current.GetType().GetProperty(part);
            if (prop is null)
            {
                return null;
            }

            current = prop.GetValue(current);
        }

        return current;
    }
    #endregion
}
