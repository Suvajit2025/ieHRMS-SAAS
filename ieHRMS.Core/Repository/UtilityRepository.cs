using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using ieHRMS.Core.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Serilog;

namespace ieHRMS.Core.Repository
{
    public class UtilityRepository:IUtilityRepository
    {
        
        public async Task<SelectList> GetSelectListAsync(DataTable table, string idColumn, string nameColumn, string defaultText, string defaultValue)
        {
            var items = table.AsEnumerable()
                             .Select(row => new SelectListItem
                             {
                                 Value = row[idColumn].ToString(),
                                 Text = row[nameColumn].ToString()
                             }).ToList();

            items.Insert(0, new SelectListItem { Value = defaultValue, Text = defaultText });
            return await Task.FromResult(new SelectList(items, "Value", "Text"));
        }

        public async Task<DataTable> GetSearchListAsync(DataTable table, string val, string[] label, string separator)
        {
            DataTable newTable = new DataTable();
            newTable.Columns.Add("Value", typeof(Int32));
            newTable.Columns.Add("Label", typeof(string));

            foreach (DataColumn column in table.Columns)
                newTable.Columns.Add(column.ColumnName, column.DataType);

            foreach (DataRow row in table.Rows)
            {
                var newRow = newTable.NewRow();
                newRow["Value"] = row[val];
                newRow["Label"] = string.Join(separator, label.Select(l => row[l].ToString()));

                foreach (DataColumn column in table.Columns)
                    newRow[column.ColumnName] = row[column.ColumnName];

                newTable.Rows.Add(newRow);
            }

            return await Task.FromResult(newTable);
        }

        public async Task<List<ExpandoObject>> GetGridList(DataTable dt)
        {
            return await Task.FromResult(ConvertDataTableToExpandoList(dt));
        }

        public async Task<Dictionary<string, string>> GetGridDict(DataTable dt, string modalId)
        {
            var listForGrid = ConvertDataTableToExpandoList(dt);
            return new Dictionary<string, string>
            {
                { "list", JsonConvert.SerializeObject(listForGrid) },
                { "id", JsonConvert.SerializeObject(modalId) }
            };
        }

        private List<ExpandoObject> ConvertDataTableToExpandoList(DataTable dt)
        {
            var list = new List<ExpandoObject>();
            foreach (DataRow row in dt.Rows)
            {
                dynamic expando = new ExpandoObject();
                var dict = (IDictionary<string, object>)expando;
                foreach (DataColumn column in dt.Columns)
                    dict[column.ColumnName] = row[column] != DBNull.Value ? row[column] : null;
                list.Add(expando);
            }

            if (dt.Rows.Count == 0)
            {
                dynamic expando = new ExpandoObject();
                var dict = (IDictionary<string, object>)expando;
                foreach (DataColumn column in dt.Columns)
                    dict[column.ColumnName] = null;
                list.Add(expando);
            }

            return list;
        }

        public async Task<Dictionary<string, string>> GetRadioCheckDict(DataTable table, string inputType, string propertyId, string propertyName)
        {
            var itemList = table.AsEnumerable().Select(row =>
                new Dictionary<string, object>
                {
                    { propertyId, Convert.ToInt32(row[propertyId]) },
                    { propertyName, row[propertyName].ToString() }
                }).ToList();

            string script = inputType == "checkbox" ? "<script>function updateSelectedItems(){/*...*/}</script>" : "";

            return new Dictionary<string, string>
            {
                { "list", JsonConvert.SerializeObject(itemList) },
                { "script", JsonConvert.SerializeObject(script) }
            };
        }

        public async Task<List<T>> ConvertDataTableToModelList<T>(DataTable table) where T : new()
        {
            List<T> modelList = new List<T>();
            var properties = typeof(T).GetProperties();

            foreach (DataRow row in table.Rows)
            {
                T model = new T();
                foreach (var prop in properties)
                {
                    if (table.Columns.Contains(prop.Name) && row[prop.Name] != DBNull.Value)
                    {
                        if (prop.PropertyType == typeof(List<string>))
                        {
                            var str = row[prop.Name].ToString();
                            prop.SetValue(model, str.Split(',').Select(s => s.Trim()).ToList());
                        }
                        else
                        {
                            prop.SetValue(model, Convert.ChangeType(row[prop.Name], prop.PropertyType));
                        }
                    }
                }
                modelList.Add(model);
            }

            return await Task.FromResult(modelList);
        }

        public async Task<DataTable> ConvertHtmlTableToDataTable(string html, Dictionary<string, string> headerMapping, Dictionary<string, object> additionalCols = null, List<string> excludeCols = null, List<string> dateFields = null)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            DataTable table = new DataTable();

            foreach (var pair in headerMapping)
            {
                if (excludeCols == null || !excludeCols.Contains(pair.Key))
                    table.Columns.Add(pair.Value);
            }

            additionalCols?.Keys.ToList().ForEach(col => table.Columns.Add(col));
            var rows = doc.DocumentNode.SelectNodes("//tr[td]");
            var headers = doc.DocumentNode.SelectNodes("//th")?.Select(h => h.InnerText.Trim()).ToList() ?? new();

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("td");
                object[] values = new object[table.Columns.Count];
                for (int i = 0; i < cells.Count && i < headers.Count; i++)
                {
                    var key = headers[i];
                    if (excludeCols == null || !excludeCols.Contains(key))
                    {
                        if (key == "Document")
                        {
                            var a = cells[i].SelectSingleNode(".//a");
                            values[table.Columns.IndexOf("DocumentLink")] = a?.GetAttributeValue("href", "");
                        }
                        else
                        {
                            string colName = headerMapping[key];
                            values[table.Columns.IndexOf(colName)] = cells[i].InnerText.Trim();
                        }
                    }
                }
                additionalCols?.ToList().ForEach(ac => values[table.Columns.IndexOf(ac.Key)] = ac.Value);
                table.Rows.Add(values);
            }

            if (dateFields != null)
            {
                foreach (DataRow row in table.Rows)
                    foreach (var field in dateFields)
                        if (table.Columns.Contains(field))
                            row[field] = ConvertToSmalldatetime(row[field]?.ToString());
            }

            return await Task.FromResult(table);
        }

        private string ConvertToSmalldatetime(string date)
        {
            string[] formats = { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "yyyy/MM/dd", "dd-MM-yyyy", "MM-dd-yyyy", "yyyyMMdd", "ddMMyyyy", "MMddyyyy" };
            if (DateTime.TryParseExact(date, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
            {
                return new DateTime(parsed.Year, parsed.Month, parsed.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second).ToString("yyyy-MM-dd HH:mm:ss");
            }
            return DBNull.Value.ToString();
        }

        public async Task<DataTable> ConvertListToDataTable<T>(List<T> items, string[] redundantColumns = null) where T : new()
        {
            DataTable table = new DataTable(typeof(T).Name);
            PropertyInfo[] props = typeof(T).GetProperties();

            foreach (var prop in props)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);

            foreach (T item in items)
                table.Rows.Add(props.Select(p => p.GetValue(item)).ToArray());

            redundantColumns?.ToList().ForEach(rc => { if (table.Columns.Contains(rc)) table.Columns.Remove(rc); });
            return await Task.FromResult(table);
        }

        public async Task<string> UploadFileAsync(string tenantId, IFormFile file, string baseUploadPath, string[] extensions, long maxSizeMB, string fileName)
        {
            try
            {
                if (file == null || file.Length == 0) return "File is empty.";
                if (file.Length > maxSizeMB * 1024 * 1024) return "File size exceeds the allowed limit.";

                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!extensions.Contains(extension)) return "Invalid file type.";

                // folder structure: tenantId/baseUploadPath
                string tenantFolderPath = Path.Combine("wwwroot", tenantId, baseUploadPath);
                if (!Directory.Exists(tenantFolderPath)) Directory.CreateDirectory(tenantFolderPath);

                string finalFileName = fileName + extension;
                string fullPath = Path.Combine(tenantFolderPath, finalFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                    await file.CopyToAsync(stream);

                string relativePath = Path.Combine(tenantId, baseUploadPath, finalFileName).Replace("\\", "/");
                return "/" + relativePath;
            }
            catch (Exception ex)
            {
                return "Error uploading file: " + ex.Message;
            }
        }


        public async Task<string> SendEmailAsync(DataTable smtpTable, string to, string cc, string bcc, string subject, string body)
        {
            try
            {
                if (smtpTable == null || smtpTable.Rows.Count == 0)
                    throw new Exception("SMTP settings table is empty.");

                DataRow row = smtpTable.Rows[0];

                // Extract SMTP config
                string smtpServer = row["SmtpServer"].ToString();
                int port = Convert.ToInt32(row["Port"]);
                bool enableSSL = Convert.ToBoolean(row["EnableSSL"]);
                string username = row["Username"].ToString();
                string password = row["Password"].ToString();
                string fromEmail = row["FromEmail"].ToString();

                // Create mail message
                MailMessage mail = new MailMessage
                {
                    From = new MailAddress(fromEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                // Bulk TO support (comma or semicolon separated)
                foreach (var address in to.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    mail.To.Add(address.Trim());
                }

                // Optional: CC
                if (!string.IsNullOrWhiteSpace(cc))
                {
                    foreach (var address in cc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        mail.CC.Add(address.Trim());
                    }
                }

                // Optional: BCC
                if (!string.IsNullOrWhiteSpace(bcc))
                {
                    foreach (var address in bcc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        mail.Bcc.Add(address.Trim());
                    }
                }

                // Configure SMTP
                SmtpClient smtp = new SmtpClient(smtpServer, port)
                {
                    EnableSsl = enableSSL,
                    Credentials = new NetworkCredential(username, password)
                };

                await smtp.SendMailAsync(mail);
                return "Email sent successfully.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SendEmailAsync failed");
                return $"Error sending email: {ex.Message}";
            }
        }


    }
}
