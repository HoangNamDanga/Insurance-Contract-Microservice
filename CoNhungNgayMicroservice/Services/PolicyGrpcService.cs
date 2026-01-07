using CoNhungNgayMicroservice.Grpc;
using Grpc.Core;
using MongoDB.Driver;
using MongoDBCore.Entities.Models;

namespace CoNhungNgayMicroservice.Services
{

    //(Triển khai logic - Service Implementation)
    // PolicyGrpc.PolicyGrpcBase chính là class nằm trong file PolicyGrpc.cs trong obj của solution
    public class PolicyGrpcService : PolicyGrpc.PolicyGrpcBase
    {
        private readonly IMongoCollection<PolicyDto> _collection;

        public PolicyGrpcService(IMongoCollection<PolicyDto> collection)
        {
            _collection = collection;
        }

        public override async Task<PolicyResponse> GetPolicyById(PolicyRequest request, ServerCallContext context)
        {
            // Tìm dữ liệu từ MongoDb bằng ID truyền lên từ gRPC request
            var policy = await _collection.Find(x => x.PolicyId == request.Id).FirstOrDefaultAsync();

            if (policy == null)
            {
                //Trả về lỗi nếu không tìm thấy
                throw new RpcException(new Status(StatusCode.NotFound, $"Policy {request.Id} not found"));
            }

            // Map trực tiếp từ PolicyDto sang PolicyResponse (Class tự sinh)
            return new PolicyResponse
            {
                PolicyId = policy.PolicyId,
                PolicyNumber = policy.PolicyNumber,
                Status = policy.Status,
                CustomerName = policy.CustomerName,
                PremiumAmount = (double)policy.PremiumAmount // gRPC dùng double thay vì decimal
            };
        }
    }
}