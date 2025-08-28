using RentManagement.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RentManagement.Models;
using Dapper;
using Microsoft.AspNetCore.Connections;
using System.Data;
using System.Data.SqlClient;

namespace RentManagement.Data
{
    public class LeaseDocumentRepository : ILeaseDocumentRepository
    {
        // Basic CRUD operations
        private readonly string _connectionString;

        public LeaseDocumentRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private IDbConnection CreateConnection()
            => new SqlConnection(_connectionString);
        public async Task<int> AddLeaseDocumentAsync(LeaseDocument leaseDocument)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@LeaseId", leaseDocument.LeaseId);
            parameters.Add("@FileName", leaseDocument.FileName);
            parameters.Add("@UniqueFileName", leaseDocument.UniqueFileName);
            parameters.Add("@FilePath", leaseDocument.FilePath);
            parameters.Add("@FileSize", leaseDocument.FileSize);
            parameters.Add("@ContentType", leaseDocument.ContentType);
            parameters.Add("@UploadedBy", leaseDocument.UploadedBy);
            parameters.Add("@DocumentId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync("sp_AddLeaseDocument", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@DocumentId");
        }

        public async Task<List<LeaseDocument>> GetLeaseDocumentsByLeaseIdAsync(int leaseId)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@LeaseId", leaseId);

            var result = await connection.QueryAsync<LeaseDocument>(
                "sp_GetLeaseDocumentsByLeaseId",
                parameters,
                commandType: CommandType.StoredProcedure);

            return result.ToList();
        }

        public async Task<LeaseDocument> GetLeaseDocumentByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@DocumentId", id);

            var result = await connection.QueryFirstOrDefaultAsync<LeaseDocument>(
                "sp_GetLeaseDocumentById",
                parameters,
                commandType: CommandType.StoredProcedure);

            return result;
        }

        public async Task<bool> DeleteLeaseDocumentAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@DocumentId", id);
            parameters.Add("@Success", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            await connection.ExecuteAsync("sp_DeleteLeaseDocument", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<bool>("@Success");
        }

        public async Task<bool> UpdateLeaseDocumentAsync(LeaseDocument leaseDocument)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@DocumentId", leaseDocument.Id);
            parameters.Add("@FileName", leaseDocument.FileName);
            parameters.Add("@FilePath", leaseDocument.FilePath);
            parameters.Add("@FileSize", leaseDocument.FileSize);
            parameters.Add("@ContentType", leaseDocument.ContentType);
            parameters.Add("@Success", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            await connection.ExecuteAsync("sp_UpdateLeaseDocument", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<bool>("@Success");
        }
    }
}

