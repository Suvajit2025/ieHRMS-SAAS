 
using ieHRMS.Core.DataAccess;
using ieHRMS.Core.Interface;
using Serilog; 
using System.Data; 

namespace ieHRMS.Core.Repository
{
    public class DataServiceRepository :IDataService
    {
        private readonly AdoDataAccess _DTO;
        private readonly string _defaultConnectionString;
        // Configure Serilog in a static constructor
        static DataServiceRepository()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/DataService_log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
        public DataServiceRepository(AdoDataAccess DTO, string defaultConnectionString)
        {
            _DTO = DTO;
            _defaultConnectionString = defaultConnectionString;
        }
        private string GetConnectionString(string providedConnectionString)
        {
            return !string.IsNullOrEmpty(providedConnectionString) ? providedConnectionString : _defaultConnectionString;
        }
        public async Task<int> AddAsync(string storedProcedure, Dictionary<string, object> parameters, string connectionString = null)
        {
            //throw new NotImplementedException();
            try
            {
                var parameterNames = parameters.Keys.ToArray();
                var parameterValues = parameters.Values.ToArray();

                return await _DTO.Int_ProcessAsync(storedProcedure, parameterNames, parameterValues, GetConnectionString(connectionString));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error: " + ex.Message, storedProcedure);
                throw new ApplicationException("An error occurred while adding the record.", ex);
            }
        }

        public async Task<object> AddTableAsync(string storedProcedure, Dictionary<string, object> parameters, string connectionString = null)
        {
            //throw new NotImplementedException();
            try
            {
                var parameterNames = parameters.Keys.ToArray();
                var parameterValues = parameters.Values.ToArray();

                return await _DTO.Dt_ProcessAsync(storedProcedure, parameterNames, parameterValues, GetConnectionString(connectionString));
            }
            catch (Exception ex) {
                Log.Error(ex, "Error: " + ex.Message, storedProcedure);
                throw new ApplicationException("An error occurred while adding the record.", ex); 
            }

        }
        public async Task<int> UpdateAsync(string storedProcedure, Dictionary<string, object> parameters, string connectionString = null)
        {
            //throw new NotImplementedException();
            try
            {
                var parameterNames = parameters.Keys.ToArray();
                var parameterValues = parameters.Values.ToArray();

                return await _DTO.Int_ProcessAsync(storedProcedure, parameterNames, parameterValues, GetConnectionString(connectionString));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error: " + ex.Message, storedProcedure);
                throw new ApplicationException("An error occurred while Updating the record.", ex);
            }
        }

        public async Task<object> UpdateTableAsync(string storedProcedure, Dictionary<string, object> parameters, string connectionString = null)
        {
            //throw new NotImplementedException();
            try
            {
                var parameterNames = parameters.Keys.ToArray();
                var parameterValues = parameters.Values.ToArray();

                return await _DTO.Dt_ProcessAsync(storedProcedure, parameterNames, parameterValues, GetConnectionString(connectionString));
            }
            catch (Exception ex) {
                Log.Error(ex, "Error: " + ex.Message, storedProcedure);
                throw new ApplicationException("An error occurred while Updating the record.", ex);
            }
        }
        public async Task<int> DeleteAsync(string storedProcedure, Dictionary<string, object> parameters, string connectionString = null)
        {
            //throw new NotImplementedException();
            try
            {
                var parameterNames = parameters.Keys.ToArray();
                var parameterValues = parameters.Values.ToArray();

                return await _DTO.Int_ProcessAsync(storedProcedure, parameterNames, parameterValues, GetConnectionString(connectionString));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error: " + ex.Message, storedProcedure);
                throw new ApplicationException("An error occurred while Deleting the record.", ex);
            }
        }

        public async Task<DataTable> GetDataAsync(string storedProcedure, Dictionary<string, object> parameters, string connectionString = null)
        {
            try
            {
                var parameterNames = parameters.Keys.ToArray();
                var parameterValues = parameters.Values.ToArray();

                var result = await _DTO.Dt_ProcessAsync(storedProcedure, parameterNames, parameterValues, GetConnectionString(connectionString));

                // Convert the DataTable to a List of Dictionaries
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error: " + ex.Message, storedProcedure);
                throw new ApplicationException("An error occurred while retrieving records.", ex);
            }
        }
        public async Task<DataSet> GetAllDatasetAsync(string storedProcedure, Dictionary<string, object> parameters, string connectionString = null)
        {
            //throw new NotImplementedException();
            try
            {
                var parameterNames = parameters.Keys.ToArray();
                var parameterValues = parameters.Values.ToArray();

                var result = await _DTO.Ds_ProcessAsync(storedProcedure, parameterNames, parameterValues, GetConnectionString(connectionString));

                // Return the DataSet
                return result;


            }
            catch (Exception ex) {
                Log.Error(ex, "Error: " + ex.Message, storedProcedure);
                throw new ApplicationException("An error occurred while retrieving the dataset.", ex);
            }
        }
        public async Task<int> AddWithTVPAsync(string storedProcedure, string tvpParameterName, string tableName, DataTable tvpData, string connectionString = null)
        {
            try
            {
                return await _DTO.Int_ProcessWithTVPAsync(storedProcedure, tvpParameterName, tableName, tvpData, GetConnectionString(connectionString));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error: " + ex.Message, storedProcedure);
                throw new ApplicationException("An error occurred while adding records via the TVP.", ex);
            }
        }
        public async Task<int> Int_ProcessWithMultipleTVPsAsync(string storedProcedure, Dictionary<string, object> parameters, Dictionary<string, (string TableName, DataTable Data)> tvpParameters, string connectionString = null)
        {
            try 
            {
                var result = await _DTO.Int_ProcessWithMultipleTVPsAsync(storedProcedure, parameters, tvpParameters, GetConnectionString(connectionString));
                return result;
            } 
            catch (Exception ex)
            {
                Log.Error(ex, "Error: " + ex.Message, storedProcedure);
                throw new ApplicationException("An error occurred while adding records via the TVP.", ex);
            }
            

        }

    }
}
