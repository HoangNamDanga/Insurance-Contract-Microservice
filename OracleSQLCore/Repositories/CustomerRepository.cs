using Dapper;
using Oracle.ManagedDataAccess.Client;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;
        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        private IDbConnection CreateConnection() => new OracleConnection(_connectionString);

        // CREATE
        public async Task<int> AddCustomerAsync(Customer customer)
        {
            var parameters = new DynamicParameters();
            parameters.Add("P_FULL_NAME", customer.FullName);
            parameters.Add("P_GENDER", customer.Gender);
            parameters.Add("P_DOB", customer.DateOfBirth);
            parameters.Add("P_PHONE", customer.Phone);
            parameters.Add("P_EMAIL", customer.Email);
            parameters.Add("P_ADDRESS", customer.Address);
            parameters.Add("P_NEW_ID", dbType: DbType.Int32, direction: ParameterDirection.Output);

            using (var connection = CreateConnection())
            {
                await connection.ExecuteAsync("INSURANCE_USER.DHN_CUSTOMER_PKG.CREATE_CUSTOMER", parameters, commandType: CommandType.StoredProcedure);
                return parameters.Get<int>("P_NEW_ID");
            }
        }

        // READ ALL
        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            var sql = @"
                SELECT 
                    CUSTOMER_ID AS CustomerId, 
                    FULL_NAME AS FullName, 
                    GENDER AS Gender, 
                    DATE_OF_BIRTH AS DateOfBirth, 
                    PHONE AS Phone, 
                    EMAIL AS Email, 
                    ADDRESS AS Address, 
                    CREATE_DATE AS CreateDate
                FROM INSURANCE_USER.DHN_CUSTOMER";
            using (var connection = CreateConnection())
            {
                return await connection.QueryAsync<Customer>(sql);
            }
        }

        // READ BY ID
        public async Task<Customer> GetCustomerByIdAsync(int customerId)
        {
            // Sử dụng Alias để ánh xạ tên cột Oracle (Snake_Case) sang C# (PascalCase)
            var sql = @"
                SELECT 
                    CUSTOMER_ID AS CustomerId, 
                    FULL_NAME AS FullName, 
                    GENDER AS Gender, 
                    DATE_OF_BIRTH AS DateOfBirth, 
                    PHONE AS Phone, 
                    EMAIL AS Email, 
                    ADDRESS AS Address, 
                    CREATE_DATE AS CreateDate
                FROM INSURANCE_USER.DHN_CUSTOMER 
                WHERE CUSTOMER_ID = :Id"; // Tham số Oracle binding

                    using (var connection = CreateConnection())
                    {
                        // Dapper sẽ sử dụng tham số mới { Id = customerId } để thay thế :Id
                        var a = await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { Id = customerId });
                        return a;
                    }
        }

        // UPDATE
        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            var parameters = new DynamicParameters();
            parameters.Add("P_CUSTOMER_ID", customer.CustomerId);
            parameters.Add("P_FULL_NAME", customer.FullName);
            parameters.Add("P_GENDER", customer.Gender);
            parameters.Add("P_DOB", customer.DateOfBirth);
            parameters.Add("P_PHONE", customer.Phone);
            parameters.Add("P_EMAIL", customer.Email);
            parameters.Add("P_ADDRESS", customer.Address);

            using (var connection = CreateConnection())
            {
                var affectedRows = await connection.ExecuteAsync("INSURANCE_USER.DHN_CUSTOMER_PKG.UPDATE_CUSTOMER", parameters, commandType: CommandType.StoredProcedure);
                return true; // Với SP Oracle, thường ta trả về true nếu không có Exception
            }
        }

        // DELETE
        public async Task<bool> DeleteCustomerAsync(int customerId)
        {
            var sql = "DELETE FROM INSURANCE_USER.DHN_CUSTOMER WHERE CUSTOMER_ID = :Id";
            using (var connection = CreateConnection())
            {
                var affectedRows = await connection.ExecuteAsync(sql, new { Id = customerId });
                return affectedRows > 0;
            }
        }
    }
}
