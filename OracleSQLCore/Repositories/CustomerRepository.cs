using Dapper;
using Oracle.ManagedDataAccess.Client;
using OracleSQLCore.Interface;
using OracleSQLCore.Models;
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
            var sql = @"
                INSERT INTO DHN_CUSTOMER (FULL_NAME, GENDER, DATE_OF_BIRTH, PHONE, EMAIL, ADDRESS)
                VALUES (:FullName, :Gender, :DateOfBirth, :Phone, :Email, :Address)
                RETURNING CUSTOMER_ID INTO :NewId";

            // Dùng DynamicParameters để lấy ID sau khi INSERT
            var parameters = new DynamicParameters(customer);
            parameters.Add("NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            using (var connection = CreateConnection())
            {
                await connection.ExecuteAsync(sql, parameters);
                return parameters.Get<int>("NewId");
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
                FROM DHN_CUSTOMER";
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
                FROM DHN_CUSTOMER 
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
            var sql = @"
                UPDATE DHN_CUSTOMER SET 
                    FULL_NAME = :FullName, GENDER = :Gender, 
                    DATE_OF_BIRTH = :DateOfBirth, PHONE = :Phone, 
                    EMAIL = :Email, ADDRESS = :Address
                WHERE CUSTOMER_ID = :CustomerId";

            using (var connection = CreateConnection())
            {
                var affectedRows = await connection.ExecuteAsync(sql, customer);
                return affectedRows > 0;
            }
        }

        // DELETE
        public async Task<bool> DeleteCustomerAsync(int customerId)
        {
            var sql = "DELETE FROM DHN_CUSTOMER WHERE CUSTOMER_ID = :Id";
            using (var connection = CreateConnection())
            {
                var affectedRows = await connection.ExecuteAsync(sql, new { Id = customerId });
                return affectedRows > 0;
            }
        }
    }
}
