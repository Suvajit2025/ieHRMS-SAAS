using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ieHRMS.Core.DataAccess
{
    public class AdoDataAccess
    {
        private readonly string _defaultConnectionString;

        static AdoDataAccess()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/DataAccess_log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        public AdoDataAccess(string defaultConnectionString)
        {
            _defaultConnectionString = defaultConnectionString;
        }

        /// <summary>
        /// Returns a new SQL connection using the provided connection string or the default one.
        /// </summary>
        private SqlConnection GetConnection(string connectionString = null)
        {
            return new SqlConnection(!string.IsNullOrEmpty(connectionString) ? connectionString : _defaultConnectionString);
        }

        private void LogException(Exception ex, string methodName, string queryOrSP)
        {
            Log.Error(ex, "Error in {Method} | Query: {QueryOrSP}", methodName, queryOrSP);
        }

        /// <summary>
        /// Executes a stored procedure that returns an integer.
        /// </summary>
        public async Task<int> Int_ProcessAsync(string storedProcedure, string[] parameterNames, object[] parameterValues, string connectionString = null)
        {
            if (parameterNames.Length != parameterValues.Length)
                throw new ArgumentException("The lengths of parameter names and values must be equal.");

            using (var con = GetConnection(connectionString))
            using (var cmd = new SqlCommand(storedProcedure, con))
            {
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;

                for (int i = 0; i < parameterNames.Length; i++)
                {
                    cmd.Parameters.AddWithValue(parameterNames[i], parameterValues[i] ?? DBNull.Value);
                }

                var returnParameter = cmd.Parameters.Add("RetVal", SqlDbType.Int);
                returnParameter.Direction = ParameterDirection.ReturnValue;

                try
                {
                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                    return (int)returnParameter.Value;
                }
                catch (Exception ex)
                {
                    LogException(ex, nameof(Int_ProcessAsync), storedProcedure);
                    throw new ApplicationException("An error occurred while processing the request.", ex);
                }
            }
        }

        /// <summary>
        /// Executes a stored procedure that returns a DataSet.
        /// </summary>
        public async Task<DataSet> Ds_ProcessAsync(string storedProcedure, string[] parameterNames, object[] parameterValues, string connectionString = null)
        {
            var dataSet = new DataSet();

            using (var con = GetConnection(connectionString))
            using (var cmd = new SqlCommand(storedProcedure, con))
            {
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;

                for (int i = 0; i < parameterNames.Length; i++)
                {
                    cmd.Parameters.AddWithValue(parameterNames[i], parameterValues[i] ?? DBNull.Value);
                }

                using (var adapter = new SqlDataAdapter(cmd))
                {
                    try
                    {
                        await Task.Run(() => adapter.Fill(dataSet));
                        return dataSet;
                    }
                    catch (Exception ex)
                    {
                        LogException(ex, nameof(Ds_ProcessAsync), storedProcedure);
                        throw new ApplicationException("An error occurred while fetching the dataset.", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Executes a stored procedure that returns a DataTable.
        /// </summary>
        public async Task<DataTable> Dt_ProcessAsync(string storedProcedure, string[] parameterNames, object[] parameterValues, string connectionString = null)
        {
            if (parameterNames.Length != parameterValues.Length)
                throw new ArgumentException("The lengths of parameter names and values must be equal.");

            var dataTable = new DataTable();

            using (var con = GetConnection(connectionString))
            using (var cmd = new SqlCommand(storedProcedure, con))
            {
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;

                for (int i = 0; i < parameterNames.Length; i++)
                {
                    cmd.Parameters.AddWithValue(parameterNames[i], parameterValues[i] ?? DBNull.Value);
                }

                using (var adapter = new SqlDataAdapter(cmd))
                {
                    try
                    {
                        await Task.Run(() => adapter.Fill(dataTable));
                        return dataTable;
                    }
                    catch (Exception ex)
                    {
                        LogException(ex, nameof(Dt_ProcessAsync), storedProcedure);
                        throw new ApplicationException("An error occurred while fetching the data.", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Executes a stored procedure with a Table-Valued Parameter (TVP).
        /// </summary>
        public async Task<int> Int_ProcessWithTVPAsync(string storedProcedure, string tvpParameterName, string tableName, DataTable tvpData, string connectionString = null)
        {
            using (var con = GetConnection(connectionString))
            using (var cmd = new SqlCommand(storedProcedure, con))
            {
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;

                var tvpParam = cmd.Parameters.AddWithValue(tvpParameterName, tvpData);
                tvpParam.SqlDbType = SqlDbType.Structured;
                tvpParam.TypeName = tableName;

                var returnParameter = cmd.Parameters.Add("RetVal", SqlDbType.Int);
                returnParameter.Direction = ParameterDirection.ReturnValue;

                try
                {
                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                    return (int)returnParameter.Value;
                }
                catch (Exception ex)
                {
                    LogException(ex, nameof(Int_ProcessWithTVPAsync), storedProcedure);
                    throw new ApplicationException("An error occurred while processing the request.", ex);
                }
            }
        }

        /// <summary>
        /// Executes a stored procedure with multiple Table-Valued Parameters (TVPs).
        /// </summary>
        public async Task<int> Int_ProcessWithMultipleTVPsAsync(string storedProcedure, Dictionary<string, object> parameters, Dictionary<string, (string TableName, DataTable Data)> tvpParameters, string connectionString = null)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters), "The parameters dictionary cannot be null.");

            if (tvpParameters == null || tvpParameters.Count == 0)
                throw new ArgumentNullException(nameof(tvpParameters), "The TVP parameters dictionary cannot be null or empty.");

            using (var con = GetConnection(connectionString))
            using (var cmd = new SqlCommand(storedProcedure, con))
            {
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;

                foreach (var tvpParam in tvpParameters)
                {
                    var parameter = cmd.Parameters.Add(tvpParam.Key, SqlDbType.Structured);
                    parameter.Value = tvpParam.Value.Data ?? throw new ArgumentNullException(tvpParam.Key, "DataTable cannot be null.");
                    parameter.TypeName = tvpParam.Value.TableName;
                }

                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }

                var returnParameter = cmd.Parameters.Add("RetVal", SqlDbType.Int);
                returnParameter.Direction = ParameterDirection.ReturnValue;

                try
                {
                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                    return (int)returnParameter.Value;
                }
                catch (Exception ex)
                {
                    LogException(ex, nameof(Int_ProcessWithMultipleTVPsAsync), storedProcedure);
                    throw new ApplicationException("An error occurred while processing multiple TVPs.", ex);
                }
            }
        }
    }
}
