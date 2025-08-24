using Dapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using RentManagement.Models;
using System;
using System.Data;
using System.Data.SqlClient;

namespace RentManagement.Data
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        public EmployeeRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private IDbConnection CreateConnection()
            => new SqlConnection(_connectionString);

        #region Original Methods

        public async Task<PagedResult<Employee>> GetEmployeesAsync(int page, int pageSize, string search)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Page", page);
            parameters.Add("@PageSize", pageSize);
            parameters.Add("@Search", search ?? string.Empty);

            using var multi = await connection.QueryMultipleAsync(
                "EmployeeRead",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var employees = (await multi.ReadAsync<Employee>()).ToList();
            var totalCount = await multi.ReadFirstOrDefaultAsync<int>();

            return new PagedResult<Employee>
            {
                Items = employees,
                TotalItems = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            return await connection.QueryFirstOrDefaultAsync<Employee>(
                "GetEmployeeById",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Email", email);
            parameters.Add("@excludeId", excludeId);

            return await connection.ExecuteScalarAsync<bool>(
                "CheckEmployeesEmailExists",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> CreateEmployeeAsync(Employee employee)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Code", employee.Code);
            parameters.Add("@Name", employee.Name);
            parameters.Add("@DateOfBirth", employee.DateOfBirth);
            parameters.Add("@Gender", employee.Gender);
            parameters.Add("@Email", employee.Email);
            parameters.Add("@Aadhar", employee.Aadhar);
            parameters.Add("@Pan", employee.Pan);
            parameters.Add("@DepartmentId", employee.DepartmentId);
            parameters.Add("@DesignationId", employee.DesignationId);
            parameters.Add("@DateOfJoining", employee.DateOfJoining);
            parameters.Add("@EligibleForLease", employee.EligibleForLease);
            parameters.Add("@TotalSalary", employee.TotalSalary);
            parameters.Add("@BasicSalary", employee.BasicSalary);
            parameters.Add("@HRA", employee.HRA);
            parameters.Add("@HouseRentAllowance", employee.HouseRentAllowance);
            parameters.Add("@TravelAllowance", employee.TravelAllowance);
            parameters.Add("@MedicalAllowance", employee.MedicalAllowance);
            parameters.Add("@OtherAllowance", employee.OtherAllowance);
            parameters.Add("@GrossSalaryAfterDeductions", employee.GrossSalaryAfterDeductions);
            parameters.Add("@ProvidentFund", employee.PF);
            parameters.Add("@ProfessionalTax", employee.ProfessionalTax);
            parameters.Add("@ESIC", employee.ESI);
            parameters.Add("@IsActive", employee.IsActive);
            parameters.Add("@CreatedAt", employee.CreatedAt);
            parameters.Add("@CreatedBy", employee.CreatedBy);

            // Approval workflow parameters
            parameters.Add("@ApprovalStatus", (int)employee.ApprovalStatus);
            parameters.Add("@MakerUserId", employee.MakerUserId);
            parameters.Add("@MakerUserName", employee.MakerUserName);
            parameters.Add("@CheckerUserId", employee.CheckerUserId);
            parameters.Add("@CheckerUserName", employee.CheckerUserName);
            parameters.Add("@MakerAction", (int)employee.MakerAction);
            parameters.Add("@ApprovalDate", employee.ApprovalDate);
            parameters.Add("@IsActiveRecord", employee.IsActiveRecord);

            parameters.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "EmployeeCreateWithApproval",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Id");
        }

        public async Task<bool> UpdateEmployeeAsync(Employee employee)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", employee.Id);
            parameters.Add("@Code", employee.Code);
            parameters.Add("@Name", employee.Name);
            parameters.Add("@DateOfBirth", employee.DateOfBirth);
            parameters.Add("@Gender", employee.Gender);
            parameters.Add("@Email", employee.Email);
            parameters.Add("@Aadhar", employee.Aadhar);
            parameters.Add("@Pan", employee.Pan);
            parameters.Add("@DepartmentId", employee.DepartmentId);
            parameters.Add("@DesignationId", employee.DesignationId);
            parameters.Add("@DateOfJoining", employee.DateOfJoining);
            parameters.Add("@EligibleForLease", employee.EligibleForLease);
            parameters.Add("@TotalSalary", employee.TotalSalary);
            parameters.Add("@BasicSalary", employee.BasicSalary);
            parameters.Add("@HRA", employee.HRA);
            parameters.Add("@HouseRentAllowance", employee.HouseRentAllowance);
            parameters.Add("@TravelAllowance", employee.TravelAllowance);
            parameters.Add("@MedicalAllowance", employee.MedicalAllowance);
            parameters.Add("@OtherAllowance", employee.OtherAllowance);
            parameters.Add("@GrossSalaryAfterDeductions", employee.GrossSalaryAfterDeductions);
            parameters.Add("@ProvidentFund", employee.PF);
            parameters.Add("@ProfessionalTax", employee.ProfessionalTax);
            parameters.Add("@ESIC", employee.ESI);
            parameters.Add("@IsActive", employee.IsActive);
            parameters.Add("@UpdatedBy", employee.UpdatedBy);

            // Approval workflow parameters
            parameters.Add("@ApprovalStatus", (int)employee.ApprovalStatus);
            parameters.Add("@CheckerUserId", employee.CheckerUserId);
            parameters.Add("@CheckerUserName", employee.CheckerUserName);
            parameters.Add("@ApprovalDate", employee.ApprovalDate);

            var rowsAffected = await connection.ExecuteAsync(
                "EmployeeUpdateWithApproval",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            var rowsAffected = await connection.ExecuteAsync(
                "EmployeeDelete",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return rowsAffected > 0;
        }

        public async Task<IEnumerable<Department>> GetDepartmentsAsync()
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<Department>(
                "DepartmentRead",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<Designation>> GetDesignationsAsync()
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<Designation>(
                "DesignationRead",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task ToggleActiveStatus(int? Id)
        {
            using var connection = CreateConnection();
            await connection.ExecuteAsync(
                "ToggleEmployeeActive",
                new { EmployeeId = Id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesDropdownAsync()
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<Employee>("sp_GetAllEmployees", commandType: CommandType.StoredProcedure);
        }

        #endregion

        #region Approval Workflow Methods

        public async Task<IEnumerable<Employee>> GetApprovedEmployeesAsync(string searchTerm = "", string statusFilter = "", int page = 1, int pageSize = 10)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SearchTerm", searchTerm ?? string.Empty);
            parameters.Add("@StatusFilter", statusFilter ?? string.Empty);
            parameters.Add("@Page", page);
            parameters.Add("@PageSize", pageSize);

            return await connection.QueryAsync<Employee>(
                "GetApprovedEmployees",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> GetApprovedEmployeeCountAsync(string searchTerm = "", string statusFilter = "")
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SearchTerm", searchTerm ?? string.Empty);
            parameters.Add("@StatusFilter", statusFilter ?? string.Empty);

            return await connection.ExecuteScalarAsync<int>(
                "GetApprovedEmployeeCount",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<Employee>> GetPendingApprovalsAsync(string searchTerm = "", int page = 1, int pageSize = 10)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SearchTerm", searchTerm ?? string.Empty);
            parameters.Add("@Page", page);
            parameters.Add("@PageSize", pageSize);

            return await connection.QueryAsync<Employee>(
                "GetPendingEmployeeApprovals",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> GetPendingApprovalCountAsync(string searchTerm = "")
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SearchTerm", searchTerm ?? string.Empty);

            return await connection.ExecuteScalarAsync<int>(
                "GetPendingEmployeeApprovalCount",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<Employee>> GetRejectedEmployeesAsync(string searchTerm = "", int page = 1, int pageSize = 10)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SearchTerm", searchTerm ?? string.Empty);
            parameters.Add("@Page", page);
            parameters.Add("@PageSize", pageSize);

            return await connection.QueryAsync<Employee>(
                "GetRejectedEmployees",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> GetRejectedEmployeeCountAsync(string searchTerm = "")
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SearchTerm", searchTerm ?? string.Empty);

            return await connection.ExecuteScalarAsync<int>(
                "GetRejectedEmployeeCount",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> AddEmployeeForApprovalAsync(Employee employee, string makerUserId, string makerUserName, MakerAction makerAction)
        {


            employee.ApprovalStatus = ApprovalStatus.Pending;
            employee.MakerUserId = makerUserId;
            employee.MakerUserName = makerUserName;
            employee.IsActiveRecord = true;

            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            try
            {
                parameters.Add("@Code", employee.Code);
                parameters.Add("@Name", employee.Name);
                parameters.Add("@DateOfBirth", employee.DateOfBirth);
                parameters.Add("@Gender", employee.Gender);
                parameters.Add("@Email", employee.Email);
                parameters.Add("@Aadhar", employee.Aadhar);
                parameters.Add("@Pan", employee.Pan);
                parameters.Add("@DepartmentId", employee.DepartmentId);
                parameters.Add("@DesignationId", employee.DesignationId);
                parameters.Add("@DateOfJoining", employee.DateOfJoining);
                parameters.Add("@EligibleForLease", employee.EligibleForLease);
                parameters.Add("@TotalSalary", employee.TotalSalary);
                parameters.Add("@BasicSalary", employee.BasicSalary);
                parameters.Add("@HRA", employee.HRA);
                parameters.Add("@HouseRentAllowance", employee.HouseRentAllowance);
                parameters.Add("@TravelAllowance", employee.TravelAllowance);
                parameters.Add("@MedicalAllowance", employee.MedicalAllowance);
                parameters.Add("@OtherAllowance", employee.OtherAllowance);
                parameters.Add("@GrossSalaryAfterDeductions", employee.GrossSalaryAfterDeductions);
                parameters.Add("@ProvidentFund", employee.PF);
                parameters.Add("@ProfessionalTax", employee.ProfessionalTax);
                parameters.Add("@ESIC", employee.ESI);
                parameters.Add("@IsActive", employee.IsActive);
                parameters.Add("@MakerUserId", makerUserId);
                parameters.Add("@MakerUserName", makerUserName);
                parameters.Add("@MakerAction", (int)makerAction);
                parameters.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await connection.ExecuteAsync(
                    "AddEmployeeForApproval",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

            }
            catch (Exception ex)
            {

                throw ex;
            }

            return parameters.Get<int>("@Id");
        }

        public async Task<bool> UpdateEmployeeForApprovalAsync(Employee employee, string makerUserId, string makerUserName)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", employee.Id);
            parameters.Add("@Code", employee.Code);
            parameters.Add("@Name", employee.Name);
            parameters.Add("@DateOfBirth", employee.DateOfBirth);
            parameters.Add("@Gender", employee.Gender);
            parameters.Add("@Email", employee.Email);
            parameters.Add("@Aadhar", employee.Aadhar);
            parameters.Add("@Pan", employee.Pan);
            parameters.Add("@DepartmentId", employee.DepartmentId);
            parameters.Add("@DesignationId", employee.DesignationId);
            parameters.Add("@DateOfJoining", employee.DateOfJoining);
            parameters.Add("@EligibleForLease", employee.EligibleForLease);
            parameters.Add("@TotalSalary", employee.TotalSalary);
            parameters.Add("@BasicSalary", employee.BasicSalary);
            parameters.Add("@HRA", employee.HRA);
            parameters.Add("@HouseRentAllowance", employee.HouseRentAllowance);
            parameters.Add("@TravelAllowance", employee.TravelAllowance);
            parameters.Add("@MedicalAllowance", employee.MedicalAllowance);
            parameters.Add("@OtherAllowance", employee.OtherAllowance);
            parameters.Add("@GrossSalaryAfterDeductions", employee.GrossSalaryAfterDeductions);
            parameters.Add("@ProvidentFund", employee.PF);
            parameters.Add("@ProfessionalTax", employee.ProfessionalTax);
            parameters.Add("@ESIC", employee.ESI);
            parameters.Add("@IsActive", employee.IsActive);
            parameters.Add("@MakerUserId", makerUserId);
            parameters.Add("@MakerUserName", makerUserName);

            var rowsAffected = await connection.ExecuteAsync(
                "UpdateEmployeeForApproval",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteEmployeeForApprovalAsync(int employeeId, string makerUserId, string makerUserName)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", employeeId);
            parameters.Add("@MakerUserId", makerUserId);
            parameters.Add("@MakerUserName", makerUserName);

            var rowsAffected = await connection.ExecuteAsync(
                "DeleteEmployeeForApproval",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return rowsAffected > 0;
        }

        public async Task<bool> ApproveEmployeeAsync(int employeeId, string checkerUserId, string checkerUserName)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE Employees 
                SET ApprovalStatus = 2, 
IsActive = 1,
                    CheckerUserId = @CheckerUserId, 
                    CheckerUserName = @CheckerUserName, 
                    ApprovalDate = GETDATE()
                WHERE Id = @Id AND ApprovalStatus = 1";

            var parameters = new
            {
                Id = employeeId,
                CheckerUserId = checkerUserId,
                CheckerUserName = checkerUserName
            };

            var affectedRows = await connection.ExecuteAsync(sql, parameters);
            return affectedRows > 0;



            //var parameters = new DynamicParameters();
            //parameters.Add("@Id", employeeId);
            //parameters.Add("@CheckerUserId", checkerUserId);
            //parameters.Add("@CheckerUserName", checkerUserName);

            //var rowsAffected = await connection.ExecuteAsync(
            //    "ApproveEmployee",
            //    parameters,
            //    commandType: CommandType.StoredProcedure
            //);

            //return rowsAffected > 0;
        }

        public async Task<bool> RejectEmployeeAsync(int employeeId, string checkerUserId, string checkerUserName, string rejectionReason)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", employeeId);
            parameters.Add("@CheckerUserId", checkerUserId);
            parameters.Add("@CheckerUserName", checkerUserName);
            parameters.Add("@RejectionReason", rejectionReason);

            var rowsAffected = await connection.ExecuteAsync(
                "RejectEmployee",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return rowsAffected > 0;
        }

        public async Task<bool> HasPendingChangesAsync(int employeeId)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@EmployeeId", employeeId);

            var count = await connection.ExecuteScalarAsync<int>(
                "CheckEmployeePendingChanges",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return count > 0;
        }

        public async Task<Employee?> GetEmployeeByCodeAsync(string employeeCode)
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Code", employeeCode);

            return await connection.QueryFirstOrDefaultAsync<Employee>(
                "GetEmployeeByCode",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        #endregion
    }
}