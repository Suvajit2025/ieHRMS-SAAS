using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ieHRMS.Core.Interface
{
    public interface IDataService
    {
        Task<int> AddAsync(string storedProcedure, Dictionary<string, object> parameters, string connectionString = null);
        Task<object> AddTableAsync(string storedProcedure, Dictionary<string, object> parameters, string connectionString = null);
        Task<int> UpdateAsync(string storedProcedure, Dictionary<string, object> parameters, string connectionString = null);
        Task<object> UpdateTableAsync(string storedProcedure, Dictionary<string, object> parameters, string connectionString = null);
        Task<int> DeleteAsync(string storedProcedure, Dictionary<string, object> parameters, string connectionString = null);
        Task<DataTable> GetDataAsync(string storedProcedure, Dictionary<string, object> parameters, string connectionString = null);
        Task<DataSet> GetAllDatasetAsync(string storedProcedure, Dictionary<string, object> parameters, string connectionString = null);
        Task<int> AddWithTVPAsync(string storedProcedure, string tvpParameterName, string tableName, DataTable tvpData, string connectionString = null);
        Task<int> Int_ProcessWithMultipleTVPsAsync(string storedProcedure, Dictionary<string, object> parameters, Dictionary<string, (string TableName, DataTable Data)> tvpParameters, string connectionString = null);

    }
}
