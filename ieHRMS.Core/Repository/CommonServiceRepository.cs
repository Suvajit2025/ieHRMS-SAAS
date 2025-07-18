 
using HtmlAgilityPack;
using Newtonsoft.Json; 
using System.Data;
using System.Dynamic;
using System.Globalization; 
using System.Net.Mail;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http; 
using ieHRMS.Core.Interface;

namespace ieHRMS.Core.Repository
{ 
    public class DropdownItem
    {
        public Int32 Id { get; set; }
        public string Name { get; set; }
    }
    public class CommonServiceRepository:ICommonService
    {
         
        // ----------------it is for select list which mostly used for dropdown--------------------------//
        public async Task<SelectList> GetSelectListAsync(DataTable table, string idColumn, string nameColumn, string defaultText, string defaultValue)
        {
            // Convert DataTable rows to a list of DropdownItem
            var items = await Task.Run(() =>
            {
                return table.AsEnumerable().Select(row => new DropdownItem
                {
                    Id = Convert.ToInt32(row[idColumn]),  // Convert to int
                    Name = row.Field<string>(nameColumn)
                }).ToList();
            });

            // Create a list of SelectListItem with the default value at the top
            var itemsWithDefault = new List<SelectListItem>
            {
                new SelectListItem { Value = defaultValue, Text = defaultText }  // Default item with empty Value
            };

            // Add the remaining items from the original list
            itemsWithDefault.AddRange(items.Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = i.Name
            }));

            // Create the SelectList with the combined items
            return new SelectList(itemsWithDefault, "Value", "Text");
        }


        // ----------------it is for search box--------------------------//
        public async Task<DataTable> GetSearchListAsync(DataTable table, string val, string[] label, string separator)
        {
            // Create a new DataTable with the same structure as the original one plus the "Value" and "Label" columns
            DataTable newTable = new DataTable(table.TableName);
            newTable.Columns.Add("Value", typeof(Int32)); // Assuming "Value" column is of type Int32
            newTable.Columns.Add("Label", typeof(string)); // Assuming "Label" column is of type string

            // Add the rest of the columns from the original table
            foreach (DataColumn column in table.Columns)
            {
                newTable.Columns.Add(column.ColumnName, column.DataType);
            }

            // Iterate through each row in the original DataTable
            if (table.Rows.Count > 0)
            {
                foreach (DataRow row in table.Rows)
                {
                    DataRow newRow = newTable.NewRow();

                    // Store the value from the specified column 'val' into the "Value" column of the new row
                    newRow["Value"] = row[val];

                    // Create a concatenated label string from the specified columns using the provided separator
                    string concatenatedLabel = string.Join(separator, label.Select(lbl => row[lbl].ToString()));
                    newRow["Label"] = concatenatedLabel;

                    // Copy all the other columns from the original row to the new row
                    foreach (DataColumn column in table.Columns)
                    {
                        newRow[column.ColumnName] = row[column.ColumnName];
                    }

                    // Add the populated row to the new table
                    newTable.Rows.Add(newRow);
                }
            }


            return await Task.FromResult(newTable); // Return the new DataTable
        }


        // ----------------it is for grid dictionary(do no use)--------------------------//
        public async Task<Dictionary<string, string>> GetGridDict(DataTable dt, string modalId)
        {
            try
            {


                List<ExpandoObject> listForGrid = new List<ExpandoObject>();


                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        dynamic expando = new ExpandoObject();
                        var expandoDict = (IDictionary<string, object>)expando;

                        foreach (DataColumn column in dt.Columns)
                        {
                            expandoDict[column.ColumnName] = row[column] != DBNull.Value ? row[column] : null;
                        }

                        listForGrid.Add(expando);
                    }
                }
                else
                {
                    // Create an empty expando object with column names
                    dynamic expando = new ExpandoObject();
                    var expandoDict = (IDictionary<string, object>)expando;

                    foreach (DataColumn column in dt.Columns)
                    {
                        expandoDict[column.ColumnName] = null;
                    }

                    listForGrid.Add(expando);
                }
                Dictionary<string, string> gridDict = new Dictionary<string, string>
        {
            { "list", JsonConvert.SerializeObject(listForGrid) },
            { "id", JsonConvert.SerializeObject(modalId) }
        };
                return gridDict;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        // ----------------it is for grid list--------------------------//
        public async Task<List<ExpandoObject>> GetGridList(DataTable dt)
        {
            try
            {


                List<ExpandoObject> listForGrid = new List<ExpandoObject>();


                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        dynamic expando = new ExpandoObject();
                        var expandoDict = (IDictionary<string, object>)expando;

                        foreach (DataColumn column in dt.Columns)
                        {
                            expandoDict[column.ColumnName] = row[column] != DBNull.Value ? row[column] : null;
                        }

                        listForGrid.Add(expando);
                    }
                }
                else
                {
                    // Create an empty expando object with column names
                    dynamic expando = new ExpandoObject();
                    var expandoDict = (IDictionary<string, object>)expando;

                    foreach (DataColumn column in dt.Columns)
                    {
                        expandoDict[column.ColumnName] = null;
                    }

                    listForGrid.Add(expando);
                }

                return listForGrid;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }






        // ----------------it is for radio and checkbox--------------------------//

        public async Task<Dictionary<string, string>> GetRadioCheckDict(DataTable table, string inputType, string propertyId, string propertyName)
        {
            try
            {


                var itemList = new List<Dictionary<string, object>>();
                string validationScript = "";

                if (inputType == "checkbox")
                {
                    // Include the checkbox validation script here
                    validationScript = @"
                <script>
                    function updateSelectedItems(checkboxClass, selectedTextId, selectedCountId, selectAllCheckboxClass, minSelections) {
                        var selectedItems = [];
                        var count = 0;

                        // Collect all selected items
                        $('.' + checkboxClass + ':checked').each(function () {
                            var itemName = $(this).next('label').text();
                            selectedItems.push(itemName);
                            count++;
                        });

                        // Update the button text and count
                        var text = $('#' + selectedTextId);
                        var countBox = $('#' + selectedCountId);

                        if (selectedItems.length > 0) {
                            text.text(selectedItems.join(', '));
                            countBox.text(count).show();
                        } else {
                            text.text('Select Items');
                            countBox.hide();
                        }

                        // Check or uncheck ""Select All"" based on individual checkboxes
                        var allChecked = $('.' + checkboxClass).length === $('.' + checkboxClass + ':checked').length;
                        $('.' + selectAllCheckboxClass).prop('checked', allChecked);

                        // Validation check
                        var validationMessage = text.closest('.dropdown').find('.CheckboxValidationMessage');
                        var dropdownButton = text.closest('.dropdown').find('.dropdown-toggle');
                        var form = text.closest('form');

                        function applyValidationStyles(isValid) {
                            debugger;
                            if (isValid) {
                                validationMessage.hide();
                                dropdownButton.removeClass('is-invalid').addClass('is-valid').css('border-color', '#44cf9c'); // Reset border color
                            } else {
                                validationMessage.show();
                                dropdownButton.removeClass('is-valid').addClass('is-invalid').css('border-color', 'red'); // Add red border color
                            }
                        }

                        if (minSelections === 'all') {
                            applyValidationStyles(allChecked);
                            if (!allChecked) {
                                validationMessage.text('All checkboxes must be selected.');
                            }
                        } else {
                            applyValidationStyles(count >= minSelections);
                            if (count < minSelections) {
                                validationMessage.text('Please select at least ' + minSelections + ' boxes.');
                            }
                        }
                    }

                    // Call the setupFormValidation function with the appropriate parameters
                    function setupFormValidation(checkboxClass, selectedTextId, selectedCountId, selectAllCheckboxClass, minSelections) {
                        $(document).ready(function () {
                            function setupCheckboxSelection() {
                                // Handle change event on ""Select All"" checkbox
                                $('.' + selectAllCheckboxClass).change(function () {
                                    var isChecked = $(this).is(':checked');
                                    $('.' + checkboxClass).prop('checked', isChecked);
                                    updateSelectedItems(checkboxClass, selectedTextId, selectedCountId, selectAllCheckboxClass, minSelections);
                                });

                                // Handle change event on individual checkboxes
                                $('.' + checkboxClass).change(function () {
                                    updateSelectedItems(checkboxClass, selectedTextId, selectedCountId, selectAllCheckboxClass, minSelections);
                                });
                            }

                            setupCheckboxSelection();

                            // Handle form submission for both button types
                            $('form').submit(function (event) {
                                var count = $('.' + checkboxClass + ':checked').length;
                                if (minSelections === 'all' && count !== $('.' + checkboxClass).length) {
                                    updateSelectedItems(checkboxClass, selectedTextId, selectedCountId, selectAllCheckboxClass, minSelections);
                                    event.preventDefault(); // Prevent form submission
                                } else if (count < minSelections) {
                                    updateSelectedItems(checkboxClass, selectedTextId, selectedCountId, selectAllCheckboxClass, minSelections);
                                    event.preventDefault(); // Prevent form submission
                                } else {
                                    // Form will be submitted
                                }
                            });
                        });
                    }
                </script>";
                }




                foreach (DataRow row in table.Rows)
                {
                    var item = new Dictionary<string, object>
                            {
                              { propertyId, Convert.ToInt32(row[propertyId]) },
                              { propertyName, row[propertyName].ToString() }
                        };

                    itemList.Add(item);
                }


                Dictionary<string, string> radioCheck = new Dictionary<string, string>
                {
                    { "list", JsonConvert.SerializeObject(itemList) },
                    { "script", JsonConvert.SerializeObject(validationScript) }
                };

                return radioCheck; // Return response
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }




        // ----------------it is used for convert datable to any list with model class as generic parameter--------------------------//

        public async Task<List<T>> ConvertDataTableToModelList<T>(DataTable table) where T : new()
        {
            List<T> modelList = new List<T>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            await Task.Run(() =>
            {
                foreach (DataRow row in table.Rows)
                {
                    T model = new T();
                    foreach (var property in properties)
                    {
                        if (table.Columns.Contains(property.Name) && row[property.Name] != DBNull.Value)
                        {
                            if (property.PropertyType == typeof(List<string>))
                            {
                                // Convert the comma-separated string to a List<string>
                                var commaSeparatedString = row[property.Name].ToString();
                                var list = commaSeparatedString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                                .Select(s => s.Trim())
                                                                .ToList();
                                property.SetValue(model, list);
                            }
                            else
                            {
                                property.SetValue(model, Convert.ChangeType(row[property.Name], property.PropertyType));
                            }
                        }
                    }
                    modelList.Add(model);
                }
            });

            return modelList;
        }

        //--Html Table To Data Table

        public async Task<DataTable> ConvertHtmlTableToDataTable(string htmlCode, Dictionary<string, string> headerMapping, Dictionary<string, object> additionalColumns = null, List<string> columnsToExclude = null, List<string> datetimeFields = null)
        {
            string htmlCode2 = htmlCode;
            Dictionary<string, string> headerMapping2 = headerMapping;
            List<string> columnsToExclude2 = columnsToExclude;
            Dictionary<string, object> additionalColumns2 = additionalColumns;
            List<string> datetimeFields2 = datetimeFields;
            return await Task.Run(delegate
            {
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(htmlCode2);
                DataTable dataTable = new DataTable();
                foreach (KeyValuePair<string, string> item in headerMapping2)
                {
                    if (columnsToExclude2 == null || !columnsToExclude2.Contains(item.Key))
                    {
                        dataTable.Columns.Add(item.Value);
                    }
                }

                if (additionalColumns2 != null)
                {
                    foreach (KeyValuePair<string, object> item2 in additionalColumns2)
                    {
                        dataTable.Columns.Add(item2.Key);
                    }
                }

                HtmlNodeCollection htmlNodeCollection = htmlDocument.DocumentNode.SelectNodes("//tr[td]");
                if (htmlNodeCollection != null)
                {
                    List<string> list = htmlDocument.DocumentNode.SelectNodes("//th")?.Select((HtmlNode h) => h.InnerText.Trim()).ToList() ?? new List<string>();
                    foreach (HtmlNode item3 in (IEnumerable<HtmlNode>)htmlNodeCollection)
                    {
                        HtmlNodeCollection htmlNodeCollection2 = item3.SelectNodes("td");
                        object[] array = new object[dataTable.Columns.Count];
                        int num = 0;
                        for (int i = 0; i < htmlNodeCollection2.Count && i < list.Count; i++)
                        {
                            string text = list[i];
                            if (columnsToExclude2 == null || !columnsToExclude2.Contains(text))
                            {
                                if (text == "Document")
                                {
                                    HtmlNode htmlNode = htmlNodeCollection2[i].SelectSingleNode(".//a");
                                    if (htmlNode != null)
                                    {
                                        int num2 = dataTable.Columns.IndexOf("DocumentLink");
                                        if (num2 >= 0)
                                        {
                                            array[num2] = htmlNode.GetAttributeValue("href", string.Empty);
                                        }
                                    }
                                }
                                else
                                {
                                    string columnName = headerMapping2[text];
                                    int num3 = dataTable.Columns.IndexOf(columnName);
                                    if (num3 >= 0)
                                    {
                                        array[num3] = htmlNodeCollection2[i].InnerText.Trim();
                                    }
                                }
                            }
                        }

                        if (additionalColumns2 != null)
                        {
                            foreach (KeyValuePair<string, object> item4 in additionalColumns2)
                            {
                                if (dataTable.Columns.Contains(item4.Key))
                                {
                                    array[dataTable.Columns.IndexOf(item4.Key)] = item4.Value;
                                }
                            }
                        }

                        dataTable.Rows.Add(array);
                    }
                }

                if (datetimeFields2 != null)
                {
                    foreach (DataRow row in dataTable.Rows)
                    {
                        foreach (string item5 in datetimeFields2)
                        {
                            if (dataTable.Columns.Contains(item5))
                            {
                                string date = row[item5]?.ToString();
                                row[item5] = ConvertToSmalldatetime(date);
                            }
                        }
                    }
                }

                return dataTable;
            });
        }


        // Method to convert string to SQL Server smalldatetime format
        private string ConvertToSmalldatetime(string date)
        {
            // Define the potential date formats to try
            string[] formats = {
                "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", // Common formats
                "yyyy/MM/dd", "dd-MM-yyyy", "MM-dd-yyyy", // Other variations
                "yyyyMMdd", "ddMMyyyy", "MMddyyyy" // Compact formats
            };

            // Attempt to parse the date using the defined formats
            if (DateTime.TryParseExact(date, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                // Combine the parsed date with the current time (or set time to midnight if preferred)
                DateTime combinedDateTime = new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day,
                                                          DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

                // Return in SQL Server datetime format
                return combinedDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }

            // Return DBNull if parsing fails or handle as needed
            return DBNull.Value.ToString();
        }

        //Datatable To List
        public async Task<DataTable> ConvertListToDataTable<T>(List<T> items, string[] redundantColumns = null) where T : new()
        {
            DataTable dataTable = new DataTable(typeof(T).Name);
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in Props)
            {
                var type = (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                            ? Nullable.GetUnderlyingType(prop.PropertyType)
                            : prop.PropertyType);
                dataTable.Columns.Add(prop.Name, type);
            }

            foreach (T item in items)
            {
                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    values[i] = Props[i].GetValue(item, null);
                }
                dataTable.Rows.Add(values);
            }

            if (redundantColumns != null)
            {
                foreach (string rc in redundantColumns)
                {
                    if (dataTable.Columns.Contains(rc))
                    {
                        dataTable.Columns.Remove(rc);
                    }
                }
            }
            return await Task.FromResult(dataTable);
        }

        //Send mail
        public async Task<string> SendEmailAsync(string SMTPServer,bool EnableSSL,string Username,string Password,int SMTP_Port,string mFromEmail,string mToEmail,string mSubject,string mBody,string? mCcEmail = null,string? mBccEmail = null)
        {
            try
            {
                using (MailMessage mm = new MailMessage())
                {
                    mm.From = new MailAddress(mFromEmail);

                    // Add To recipients (support multiple using , or ;)
                    foreach (var to in SplitEmails(mToEmail))
                        mm.To.Add(to);

                    // Add CC recipients
                    if (!string.IsNullOrWhiteSpace(mCcEmail))
                    {
                        foreach (var cc in SplitEmails(mCcEmail))
                            mm.CC.Add(cc);
                    }

                    // Add BCC recipients
                    if (!string.IsNullOrWhiteSpace(mBccEmail))
                    {
                        foreach (var bcc in SplitEmails(mBccEmail))
                            mm.Bcc.Add(bcc);
                    }

                    mm.Subject = mSubject;
                    mm.Body = mBody;
                    mm.IsBodyHtml = true;

                    using (SmtpClient smtp = new SmtpClient(SMTPServer, SMTP_Port))
                    {
                        smtp.EnableSsl = EnableSSL;
                        smtp.Credentials = new NetworkCredential(Username, Password);
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtp.Timeout = 20000;

                        await smtp.SendMailAsync(mm);
                    }
                }
                return "Success";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private List<string> SplitEmails(string emails)
        {
            return emails.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(e => e.Trim())
                         .Where(e => !string.IsNullOrEmpty(e))
                         .ToList();
        }


        //File Upload
        public async Task<string> UploadFileAsync(IFormFile file, string fullUploadPath, string[] allowedExtensions, long maxSizeMB, string uniqueFileName)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return "File is empty.";

                if (file.Length > maxSizeMB * 1024 * 1024)
                    return "File size exceeds the allowed limit.";

                string fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                    return "Invalid file type.";

                // 🔹 Ensure the directory exists
                if (!Directory.Exists(fullUploadPath))
                    Directory.CreateDirectory(fullUploadPath);

                // 🔹 Generate unique file name
                string finalFileName = $"{uniqueFileName}{fileExtension}";
                string filePath = Path.Combine(fullUploadPath, finalFileName);

                // 🔹 Save file asynchronously
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 🔹 Convert the absolute file path to a relative path
                string relativePath = filePath.Replace(Directory.GetCurrentDirectory(), "").Replace("\\", "/").Replace("wwwroot/", "");
                return $"/{relativePath}"; // ✅ Return dynamically generated relative path
            }
            catch (Exception ex)
            {
                return "Error uploading file: " + ex.Message;
            }
        }


    }
}
