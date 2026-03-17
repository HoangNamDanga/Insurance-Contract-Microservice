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
using System.Transactions;

namespace OracleSQLCore.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly string _connectionString;

        public PaymentRepository(string connectionString)
        {
            _connectionString = connectionString;
        }


        //Khi gọi procedure thì không cần gọi Returning để lấy Id vừa tạo nữa
        //Còn khi sử dụng Với SQL Thuần (Dùng RETURNING)
        public async Task<decimal> CreatePaymentAsync(PaymentDto dto)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                var p = new DynamicParameters();
                p.Add("p_policy_id", dto.PolicyId);
                p.Add("p_period", dto.PaymentPeriod);
                p.Add("p_transaction_id", dto.TransactionId);
                p.Add("p_amount", dto.Amount);
                p.Add("p_method", dto.Method);
                // p_payment_id là tham số OUT trong Procedure
                p.Add("p_payment_id", dbType: DbType.Decimal, direction: ParameterDirection.Output);

                await connection.ExecuteAsync(
                    "INSURANCE_USER.DHN_PAYMENT_PKG.PRC_CREATE_PAYMENT",
                    p,
                    commandType: CommandType.StoredProcedure);

                return p.Get<decimal>("p_payment_id");
            }
        }

        public async Task<PaymentDto?> GetByIdAsync(decimal paymentId)
        {
            string sql = @"SELECT PAYMENT_ID as PaymentId, 
                              POLICY_ID as PolicyId, 
                              PAYMENT_DATE as PaymentDate, 
                              PAYMENT_PERIOD as PaymentPeriod, 
                              TRANSACTION_ID as TransactionId, 
                              AMOUNT as Amount, 
                              METHOD as Method, 
                              STATUS as Status, 
                              CREATED_AT as CreatedAt
                       FROM INSURANCE_USER.DHN_PAYMENT 
                       WHERE PAYMENT_ID = :paymentId";

            using (var connection = new OracleConnection(_connectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<PaymentDto>(sql, new { paymentId });
            }
        }

        public async Task<IEnumerable<PaymentDto>> GetByPolicyIdAsync(decimal policyId)
        {
            string sql = @"SELECT PAYMENT_ID as PaymentId, 
                              POLICY_ID as PolicyId, 
                              PAYMENT_DATE as PaymentDate, 
                              AMOUNT as Amount, 
                              METHOD as Method, 
                              STATUS as Status
                       FROM INSURANCE_USER.DHN_PAYMENT 
                       WHERE POLICY_ID = :policyId 
                       ORDER BY CREATED_AT DESC";

            using (var connection = new OracleConnection(_connectionString))
            {
                return await connection.QueryAsync<PaymentDto>(sql, new { policyId });
            }
        }


        public async Task<bool> UpdateStatusAsync(decimal paymentId, string status, string transactionId = null)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                var p = new DynamicParameters();
                p.Add("p_payment_id", paymentId);
                p.Add("p_status", status);
                p.Add("p_transaction_id", transactionId);

                int rows = await connection.ExecuteAsync(
                    "INSURANCE_USER.DHN_PAYMENT_PKG.PRC_UPDATE_PAYMENT_STATUS",
                    p,
                    commandType: CommandType.StoredProcedure);

                return true; // Procedure trong Oracle thường ném lỗi nếu thất bại, nên trả về true nếu chạy xong
            }
        }
    }
}
