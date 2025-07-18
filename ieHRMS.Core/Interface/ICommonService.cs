using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ieHRMS.Core.Interface
{
    public interface ICommonService
    {
        //This is for SelectList
        Task<SelectList> GetSelectListAsync(DataTable table, string idColumn, string nameColumn, string defaultText, string defaultValue);

        //This is for Search
        Task<DataTable> GetSearchListAsync(DataTable table, string val, string[] label, string separator);

        //This is for grid
        Task<Dictionary<string, string>> GetGridDict(DataTable table, string modalId);

        Task<List<ExpandoObject>> GetGridList(DataTable table);
        //This is for radio and checkbox
        Task<Dictionary<string, string>> GetRadioCheckDict(DataTable table, string inputType, string propertyId, string propertyName);

        Task<List<T>> ConvertDataTableToModelList<T>(DataTable table) where T : new();

        Task<DataTable> ConvertHtmlTableToDataTable(string htmlCode, Dictionary<string, string> headerMapping, Dictionary<string, object> additionalColumns = null, List<string> columnsToExclude = null, List<string> datetimeFields = null);

        // Converts a List<T> to a DataTable
        Task<DataTable> ConvertListToDataTable<T>(List<T> items, string[] redundantColumns = null) where T : new();

        // Email Sending Method
        Task<string> SendEmailAsync(string SMTPServer, bool enableSSL, string username, string password, int SMTP_Port, string fromEmail, string toEmail, string subject, string body, string? ccEmail = null,string? bccEmail = null);
        Task<string> UploadFileAsync(IFormFile file, string uploadPath, string[] allowedExtensions, long maxSizeMB, string uniqueFileName);
    }
}
