using MassTransit;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using Polly;
using Polly.Retry;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OracleSQLCore.Services.Imp
{

    //Service xử lý RetryPolicy và Publish Event như vậy là rất chuẩn theo mô hình Eventual Consistency
    //(Đảm bảo dữ liệu Oracle xong thì phải cố gắng đẩy sang RabbitMQ bằng được
    public class PolicyService : IPolicyService
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly AsyncRetryPolicy _retryPolicy;
        public PolicyService(
            IPolicyRepository policyRepository,
            IPublishEndpoint publishEndpoint,
            AsyncRetryPolicy retryPolicy)
        {
            _policyRepository = policyRepository;
            _publishEndpoint = publishEndpoint;
            _retryPolicy = retryPolicy;
        }

        public async Task<PolicyCreatedEvent> CreateAsync(PolicyDto policy)
        {
            // 1. Lưu vào Oracle thông qua Repository
            var resultEvent = await _policyRepository.CreateAsync(policy);

            // 2. Bắn Event sang RabbitMQ với Polly Retry
            if (resultEvent != null)
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _publishEndpoint.Publish(resultEvent);
                });
            }

            return resultEvent;
        }

        public async Task<PolicyCreatedEvent> UpdateAsync(PolicyDto policy)
        {
            // 1. Cập nhật Oracle
            var resultEvent = await _policyRepository.UpdateAsync(policy);

            // 2. Bắn Event Update sang RabbitMQ với Polly Retry
            if (resultEvent != null)
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _publishEndpoint.Publish(resultEvent);
                });
            }

            return resultEvent;
        }

        public async Task<PolicyCreatedEvent> DeleteAsync(int id)
        {
            // 1. Xóa trong Oracle
            var resultEvent = await _policyRepository.DeleteAsync(id);

            // 2. Bắn Event Delete sang RabbitMQ với Polly Retry
            if (resultEvent != null)
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _publishEndpoint.Publish(resultEvent);
                });
            }

            return resultEvent;
        }

        public async Task<List<PolicyDto>> GetAllAsync()
        {
            return await _policyRepository.GetAllAsync();
        }

        public async Task<PolicyDto> GetByIdAsync(int id)
        {
            return await _policyRepository.GetByIdAsync(id);
        }

        public async Task<PolicyChangedEvent> RenewAsync(RenewPolicyDto request)
        {
            var resultEvent = await _policyRepository.RenewAsync(request);

            if (resultEvent != null)
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _publishEndpoint.Publish(resultEvent);
                });
            }
            return resultEvent;
        }

        public async Task<PolicyChangedEvent> CancelAsync(CancelPolicyDto request)
        {
            var resultEvent = await _policyRepository.CancelAsync(request);

            if (resultEvent != null)
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _publishEndpoint.Publish(resultEvent);
                });
            }
            return resultEvent;
        }
    }
}