using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Services
{
    public interface IPaymentService
    {
        //Tạo giao dịch và lấy thông tin để User đi thanh toán
        Task<decimal> CreatePaymentTransactionAsync(PaymentDto paymentDto);

        // Xử lý két quả trả về từ bên thứ 3 (Webhook/Callback)
        Task<bool> CompletePaymentAsync(decimal paymentId, string status, string transactionId);


        //Lấy thông tin từ MongoDb (Read Database) để hiển thị nhanh cho Client
        Task<PaymentDto?> GetPaymentDetailsAsync(decimal paymentId);

        // Lấy danh sách lịch sử từ MongoDb
        Task<IEnumerable<PaymentDto>> GetPaymentHistoryByPolicyAsync(decimal policyId);
    }
}
