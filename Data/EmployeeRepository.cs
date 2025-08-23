using Dapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using RentManagement.Models;
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
            parameters.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "EmployeeCreate",
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

            var rowsAffected = await connection.ExecuteAsync(
                "EmployeeUpdate",
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
    }
}