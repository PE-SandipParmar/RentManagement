using Dapper;
using DocumentFormat.OpenXml.Drawing.Charts;
using RentManagement.Models;
using System.Data;
using System.Data.SqlClient;

namespace RentManagement.Data
{
    public class LeaseRepository : ILeaseRepository
    {
        private readonly string _connectionString;

        public LeaseRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private IDbConnection CreateConnection()
            => new SqlConnection(_connectionString);

        public async Task<PagedResult<Lease>> GetLeasesAsync(int page, int pageSize, string? search)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Page", page);
            parameters.Add("@PageSize", pageSize);
            parameters.Add("@Search", search ?? string.Empty);

            using var multi = await connection.QueryMultipleAsync(
                "LeaseRead",
                parameters,
                commandType: CommandType.StoredProcedure);

            var leases = (await multi.ReadAsync<Lease>()).ToList();
            var totalCount = await multi.ReadFirstOrDefaultAsync<int>();

            return new PagedResult<Lease>
            {
                Items = leases,
                TotalItems = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public async Task<Lease?> GetLeaseByIdAsync(int id)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            return await connection.QueryFirstOrDefaultAsync<Lease>(
                "LeaseGetById",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> CreateLeaseAsync(Lease lease)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@PerquisiteType", lease.PerquisiteType);
            parameters.Add("@Status", lease.Status);
            parameters.Add("@LeaseTypeId", lease.LeaseTypeId);
            parameters.Add("@RefNo", lease.RefNo);
            parameters.Add("@EmployeeId", lease.EmployeeId);
            parameters.Add("@RefDate", lease.RefDate);
            parameters.Add("@PerquisiteApplicablePercentId", lease.PerquisiteApplicablePercentId);
            parameters.Add("@VendorId", lease.VendorId);
            parameters.Add("@MonthlyRentPayable", lease.MonthlyRentPayable);
            parameters.Add("@FromDate", lease.FromDate);
            parameters.Add("@EndDate", lease.EndDate);
            parameters.Add("@RentRecoveryElementId", lease.RentRecoveryElementId);
            parameters.Add("@RentDeposit", lease.RentDeposit);
            parameters.Add("@AdditionalRentRecovery", lease.AdditionalRentRecovery);
            parameters.Add("@BrokerageAmount", lease.BrokerageAmount);
            parameters.Add("@LicenseFeeRecoveryElementId", lease.LicenseFeeRecoveryElementId);
            parameters.Add("@StampDuty", lease.StampDuty);
            parameters.Add("@LicenseFeeAmount", lease.LicenseFeeAmount);
            parameters.Add("@PaymentTermId", lease.PaymentTermId);
            parameters.Add("@PayableOnOrBeforeId", lease.PayableOnOrBeforeId);
            parameters.Add("@Narration", lease.Narration);
            parameters.Add("@ApprovalStatus", (int)lease.ApprovalStatus);
            parameters.Add("@MakerUserId", lease.MakerUserId);
            parameters.Add("@MakerUserName", lease.MakerUserName);
            parameters.Add("@CheckerUserId", lease.CheckerUserId);
            parameters.Add("@CheckerUserName", lease.CheckerUserName);
            parameters.Add("@MakerAction", (int)lease.MakerAction);
            parameters.Add("@ApprovalDate", lease.ApprovalDate);
            parameters.Add("@RejectionReason", lease.RejectionReason);
            parameters.Add("@IsActiveRecord", lease.IsActiveRecord);
            parameters.Add("@CreatedBy", lease.CreatedBy);

            parameters.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "LeaseCreate",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Id");
        }

        public async Task<bool> UpdateLeaseAsync(Lease lease)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", lease.Id);
            parameters.Add("@PerquisiteType", lease.PerquisiteType);
            parameters.Add("@Status", lease.Status);
            parameters.Add("@LeaseTypeId", lease.LeaseTypeId);
            parameters.Add("@RefNo", lease.RefNo);
            parameters.Add("@EmployeeId", lease.EmployeeId);
            parameters.Add("@RefDate", lease.RefDate);
            parameters.Add("@PerquisiteApplicablePercentId", lease.PerquisiteApplicablePercentId);
            parameters.Add("@VendorId", lease.VendorId);
            parameters.Add("@MonthlyRentPayable", lease.MonthlyRentPayable);
            parameters.Add("@FromDate", lease.FromDate);
            parameters.Add("@EndDate", lease.EndDate);
            parameters.Add("@RentRecoveryElementId", lease.RentRecoveryElementId);
            parameters.Add("@RentDeposit", lease.RentDeposit);
            parameters.Add("@AdditionalRentRecovery", lease.AdditionalRentRecovery);
            parameters.Add("@BrokerageAmount", lease.BrokerageAmount);
            parameters.Add("@LicenseFeeRecoveryElementId", lease.LicenseFeeRecoveryElementId);
            parameters.Add("@StampDuty", lease.StampDuty);
            parameters.Add("@LicenseFeeAmount", lease.LicenseFeeAmount);
            parameters.Add("@PaymentTermId", lease.PaymentTermId);
            parameters.Add("@PayableOnOrBeforeId", lease.PayableOnOrBeforeId);
            parameters.Add("@Narration", lease.Narration);
            //parameters.Add("@ApprovalStatus", (int)lease.ApprovalStatus);
            parameters.Add("@MakerUserId", lease.MakerUserId);
            parameters.Add("@MakerUserName", lease.MakerUserName);
            //parameters.Add("@CheckerUserId", lease.CheckerUserId);
            //parameters.Add("@CheckerUserName", lease.CheckerUserName);
            //parameters.Add("@MakerAction", (int)lease.MakerAction);
            //parameters.Add("@ApprovalDate", lease.ApprovalDate);
            //parameters.Add("@RejectionReason", lease.RejectionReason);
            //parameters.Add("@IsActiveRecord", lease.IsActiveRecord);
            parameters.Add("@ModifiedBy", lease.ModifiedBy);

            var rowsAffected = await connection.ExecuteAsync(
                "LeaseUpdateForApproval",
                parameters,
                commandType: CommandType.StoredProcedure);

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteLeaseAsync(int id)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            var rowsAffected = await connection.ExecuteAsync(
                "LeaseDelete",
                parameters,
                commandType: CommandType.StoredProcedure);

            return rowsAffected > 0;
        }

        public async Task<int> GetLeasesCountAsync(string? search)
        {
            using var connection = CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Search", search ?? string.Empty);

            return await connection.ExecuteScalarAsync<int>(
                "LeaseCount",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> LeaseNoExistsAsync(string leaseNo, int? excludeId = null)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@LeaseNo", leaseNo);
            parameters.Add("@ExcludeId", excludeId);

            return await connection.ExecuteScalarAsync<bool>(
                "CheckLeaseNoExists",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<Lease?> GetLeaseByRefNoAsync(string refNo)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@RefNo", refNo);

            return await connection.QueryFirstOrDefaultAsync<Lease>(
                "LeaseGetByRefNo",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        // Approval Workflow Methods
        public async Task<IEnumerable<Lease>> GetApprovedLeasesAsync(string searchTerm, string statusFilter, int page, int pageSize)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SearchTerm", searchTerm ?? string.Empty);
            parameters.Add("@StatusFilter", statusFilter ?? string.Empty);
            parameters.Add("@Page", page);
            parameters.Add("@PageSize", pageSize);

            return await connection.QueryAsync<Lease>(
                "LeaseGetApproved",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> GetApprovedLeaseCountAsync(string searchTerm, string statusFilter)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SearchTerm", searchTerm ?? string.Empty);
            parameters.Add("@StatusFilter", statusFilter ?? string.Empty);

            return await connection.ExecuteScalarAsync<int>(
                "LeaseGetApprovedCount",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Lease>> GetPendingApprovalsAsync(string searchTerm, int page, int pageSize)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SearchTerm", searchTerm ?? string.Empty);
            parameters.Add("@Page", page);
            parameters.Add("@PageSize", pageSize);

            return await connection.QueryAsync<Lease>(
                "LeaseGetPendingApprovals",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> GetPendingApprovalCountAsync(string searchTerm)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SearchTerm", searchTerm ?? string.Empty);

            return await connection.ExecuteScalarAsync<int>(
                "LeaseGetPendingApprovalCount",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Lease>> GetRejectedLeasesAsync(string searchTerm, int page, int pageSize)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SearchTerm", searchTerm ?? string.Empty);
            parameters.Add("@Page", page);
            parameters.Add("@PageSize", pageSize);

            return await connection.QueryAsync<Lease>(
                "LeaseGetRejected",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> GetRejectedLeaseCountAsync(string searchTerm)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SearchTerm", searchTerm ?? string.Empty);

            return await connection.ExecuteScalarAsync<int>(
                "LeaseGetRejectedCount",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> AddLeaseForApprovalAsync(Lease lease, string makerUserId, string makerUserName, MakerAction action)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@PerquisiteType", lease.PerquisiteType);
            parameters.Add("@Status", lease.Status);
            parameters.Add("@LeaseTypeId", lease.LeaseTypeId);
            parameters.Add("@RefNo", lease.RefNo);
            parameters.Add("@EmployeeId", lease.EmployeeId);
            parameters.Add("@RefDate", lease.RefDate);
            parameters.Add("@PerquisiteApplicablePercentId", lease.PerquisiteApplicablePercentId);
            parameters.Add("@VendorId", lease.VendorId);
            parameters.Add("@MonthlyRentPayable", lease.MonthlyRentPayable);
            parameters.Add("@FromDate", lease.FromDate);
            parameters.Add("@EndDate", lease.EndDate);
            parameters.Add("@RentRecoveryElementId", lease.RentRecoveryElementId);
            parameters.Add("@RentDeposit", lease.RentDeposit);
            parameters.Add("@AdditionalRentRecovery", lease.AdditionalRentRecovery);
            parameters.Add("@BrokerageAmount", lease.BrokerageAmount);
            parameters.Add("@LicenseFeeRecoveryElementId", lease.LicenseFeeRecoveryElementId);
            parameters.Add("@StampDuty", lease.StampDuty);
            parameters.Add("@LicenseFeeAmount", lease.LicenseFeeAmount);
            parameters.Add("@PaymentTermId", lease.PaymentTermId);
            parameters.Add("@PayableOnOrBeforeId", lease.PayableOnOrBeforeId);
            parameters.Add("@Narration", lease.Narration);
            parameters.Add("@MakerUserId", makerUserId);
            parameters.Add("@MakerUserName", makerUserName);
            parameters.Add("@MakerAction", (int)action);
            parameters.Add("@CreatedBy", lease.CreatedBy);

            parameters.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "LeaseAddForApproval",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Id");
        }

        public async Task<bool> UpdateLeaseForApprovalAsync(Lease lease, string makerUserId, string makerUserName)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", lease.Id);
            parameters.Add("@PerquisiteType", lease.PerquisiteType);
            parameters.Add("@Status", lease.Status);
            parameters.Add("@LeaseTypeId", lease.LeaseTypeId);
            parameters.Add("@RefNo", lease.RefNo);
            parameters.Add("@EmployeeId", lease.EmployeeId);
            parameters.Add("@RefDate", lease.RefDate);
            parameters.Add("@PerquisiteApplicablePercentId", lease.PerquisiteApplicablePercentId);
            parameters.Add("@VendorId", lease.VendorId);
            parameters.Add("@MonthlyRentPayable", lease.MonthlyRentPayable);
            parameters.Add("@FromDate", lease.FromDate);
            parameters.Add("@EndDate", lease.EndDate);
            parameters.Add("@RentRecoveryElementId", lease.RentRecoveryElementId);
            parameters.Add("@RentDeposit", lease.RentDeposit);
            parameters.Add("@AdditionalRentRecovery", lease.AdditionalRentRecovery);
            parameters.Add("@BrokerageAmount", lease.BrokerageAmount);
            parameters.Add("@LicenseFeeRecoveryElementId", lease.LicenseFeeRecoveryElementId);
            parameters.Add("@StampDuty", lease.StampDuty);
            parameters.Add("@LicenseFeeAmount", lease.LicenseFeeAmount);
            parameters.Add("@PaymentTermId", lease.PaymentTermId);
            parameters.Add("@PayableOnOrBeforeId", lease.PayableOnOrBeforeId);
            parameters.Add("@Narration", lease.Narration);
            parameters.Add("@MakerUserId", makerUserId);
            parameters.Add("@MakerUserName", makerUserName);
            parameters.Add("@ModifiedBy", lease.ModifiedBy);

            var rowsAffected = await connection.ExecuteAsync(
                "LeaseUpdateForApproval",
                parameters,
                commandType: CommandType.StoredProcedure);

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteLeaseForApprovalAsync(int id, string makerUserId, string makerUserName)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@MakerUserId", makerUserId);
            parameters.Add("@MakerUserName", makerUserName);

            var rowsAffected = await connection.ExecuteAsync(
                "LeaseDeleteForApproval",
                parameters,
                commandType: CommandType.StoredProcedure);

            return rowsAffected > 0;
        }

        public async Task<bool> ApproveLeaseAsync(int id, string checkerUserId, string checkerUserName)
        {
            using var connection = CreateConnection();


            var sql = @"
                UPDATE Leases 
                SET ApprovalStatus = 2, 
                    CheckerUserId = @CheckerUserId, 
                    CheckerUserName = @CheckerUserName, 
                    ApprovalDate = GETDATE(),
                    ModifiedAt = GETDATE()
                WHERE Id = @Id AND ApprovalStatus = 1";

            var parameters = new
            {
                Id = id,
                CheckerUserId = checkerUserId,
                CheckerUserName = checkerUserName
            };

            var affectedRows = await connection.ExecuteAsync(sql, parameters);
            return affectedRows > 0;

            //var parameters = new DynamicParameters();
            //parameters.Add("@Id", id);
            //parameters.Add("@CheckerUserId", checkerUserId);
            //parameters.Add("@CheckerUserName", checkerUserName);

            //var rowsAffected = await connection.ExecuteAsync(
            //    "LeaseApprove",
            //    parameters,
            //    commandType: CommandType.StoredProcedure);

            //return rowsAffected > 0;
        }

        public async Task<bool> RejectLeaseAsync(int id, string checkerUserId, string checkerUserName, string rejectionReason)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@CheckerUserId", checkerUserId);
            parameters.Add("@CheckerUserName", checkerUserName);
            parameters.Add("@RejectionReason", rejectionReason);

            var rowsAffected = await connection.ExecuteAsync(
                "LeaseReject",
                parameters,
                commandType: CommandType.StoredProcedure);

            return rowsAffected > 0;
        }

        public async Task<bool> HasPendingChangesAsync(int id)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            return await connection.ExecuteScalarAsync<bool>(
                "LeaseHasPendingChanges",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        // Dropdown data methods
        public async Task<IEnumerable<LeaseType>> GetLeaseTypesAsync()
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<LeaseType>(
                "LeaseTypesRead",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<EmployeeName>> GetEmployeeNamesAsync()
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<EmployeeName>(
                "EmployeeNamesRead",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Owner>> GetOwnersAsync()
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<Owner>(
                "VendorRead",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<RentRecoveryElement>> GetRentRecoveryElementsAsync()
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<RentRecoveryElement>(
                "RentRecoveryElementsRead",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<LicenseFeeRecoveryElement>> GetLicenseFeeRecoveryElementsAsync()
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<LicenseFeeRecoveryElement>(
                "LicenseFeeRecoveryElementsRead",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<PaymentTerm>> GetPaymentTermsAsync()
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<PaymentTerm>(
                "PaymentTermsRead",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<PayableOnOrBeforeOption>> GetPayableOnOrBeforeOptionsAsync()
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<PayableOnOrBeforeOption>(
                "PayableOnOrBeforeOptionsRead",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<PerquisiteApplicablePercent>> GetPerquisiteApplicablePercentsAsync()
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<PerquisiteApplicablePercent>(
                "PerquisiteApplicablePercentsRead",
                commandType: CommandType.StoredProcedure);
        }
    }
}
