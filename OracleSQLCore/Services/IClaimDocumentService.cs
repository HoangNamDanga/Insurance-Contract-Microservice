using Microsoft.AspNetCore.Http;
using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Services
{
    public interface IClaimDocumentService
    {
        // Nhận vào IFormFile để xử lý lưu trữ vật lý và DB
        Task<int> UploadDocumentAsync(int claimId, IFormFile file);

        // Xóa tài liệu (Xóa cả DB và có thể gọi API xóa file nếu cần)
        Task<bool> RemoveDocumentAsync(int docId);

        // Cập nhật thông tin
        Task<bool> UpdateDocumentMetadataAsync(ClaimDocumentDto document);
    }
}
