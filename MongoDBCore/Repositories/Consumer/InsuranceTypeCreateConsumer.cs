using MassTransit;
using MongoDBCore.Entities.Models;
using MongoDBCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Shared.Contracts.Events;

namespace MongoDBCore.Repositories.Consumer
{
    public class InsuranceTypeCreateConsumer : IConsumer<InsuranceTypeEvent>
    {
        private readonly IInsuranceRepository _repo;

        public InsuranceTypeCreateConsumer(IInsuranceRepository repo)
        {
            _repo = repo;
        }

        public async Task Consume(ConsumeContext<InsuranceTypeEvent> context)
        {
            var eventData = context.Message;

            // Chuyển đổi dữ liệu từ Event sang DTO để lưu vào Mongo
            var dto = new InsuranceTypeDto
            {
                InsTypeId = eventData.InsTypeId,
                TypeName = eventData.TypeName,
                Description = eventData.Description
            };

            // Dựa vào trường 'Action' bạn gửi từ Controller để quyết định làm gì
            switch (eventData.Action.ToUpper())
            {
                case "CREATE":
                case "UPDATE":
                    // Dùng Upsert: Nếu chưa có thì Insert (Tự tạo Collection), nếu có rồi thì Update
                    await _repo.UpsertAsync(dto);
                    break;

                case "DELETE":
                    await _repo.DeleteAsync(eventData.InsTypeId);
                    break;
            }
        }
    }
}
