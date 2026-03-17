using MongoDBCore.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Interfaces
{
    public interface IPaymentRepository
    {
        Task UpsertPaymentAsync(PaymentDto payment);
        Task<PaymentDto?> GetByIdAsync(decimal paymentId);
        Task<IEnumerable<PaymentDto>> GetByPolicyIdAsync(decimal policyId);
    }
}
