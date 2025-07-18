using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Threading.Tasks;

namespace ieHRMS.Core.Interface
{
    public interface IUtilityRepository
    {
        Task<SelectList> GetSelectListAsync(DataTable table, string idColumn, string nameColumn, string defaultText, string defaultValue);

        Task<DataTable> GetSearchListAsync(DataTable table, string val, string[] label, string separator);

        Task<List<ExpandoObject>> GetGridList(DataTable dt);

        Task<Dictionary<string, string>> GetGridDict(DataTable dt, string modalId);

        Task<Dictionary<string, string>> GetRadioCheckDict(DataTable table, string inputType, string propertyId, string propertyName);

        Task<List<T>> ConvertDataTableToModelList<T>(DataTable table) where T : new();

        Task<DataTable> ConvertHtmlTableToDataTable(
            string html,
            Dictionary<string, string> headerMapping,
            Dictionary<string, object> additionalCols = null,
            List<string> excludeCols = null,
            List<string> dateFields = null);

        Task<DataTable> ConvertListToDataTable<T>(List<T> items, string[] redundantColumns = null) where T : new();

        /// <summary>
        /// Uploads a file to a tenant-specific location
        /// </summary>
        /// <param name="tenantId">Tenant's unique identifier</param>
        /// <param name="file">File object</param>
        /// <param name="baseUploadPath">Base path (e.g. wwwroot/uploads)</param>
        /// <param name="extensions">Allowed file extensions</param>
        /// <param name="maxSizeMB">Maximum allowed size in MB</param>
        /// <param name="fileName">Desired file name (without extension)</param>
        Task<string> UploadFileAsync(string tenantId,IFormFile file,string baseUploadPath,string[] extensions,long maxSizeMB,string fileName);

        /// <summary>
        /// Sends an email based on tenant SMTP configuration
        /// </summary>
        Task<string> SendEmailAsync(DataTable smtpTable, string to, string cc, string bcc, string subject, string body); 
    }
}
