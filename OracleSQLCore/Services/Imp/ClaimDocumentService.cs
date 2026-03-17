using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Services.Imp
{
    public class ClaimDocumentService : IClaimDocumentService
    {
        private readonly IClaimDocumentRepository _claimDocumentRepo;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _env;

        public ClaimDocumentService(IClaimDocumentRepository claimDocumentRepo, IHttpClientFactory httpClientFactory, IWebHostEnvironment env)
        {
            _claimDocumentRepo = claimDocumentRepo;
            _httpClientFactory = httpClientFactory;
            _env = env;
        }

        public async Task<bool> RemoveDocumentAsync(int docId)
        {
            // 1. Lấy thông tin tài liệu trước khi xóa để có FILE_PATH
            // Bạn cần gọi Repo lấy data lên trước khi nó bị xóa mất khỏi DB
            var document = await _claimDocumentRepo.GetDocumentByIdAsync(docId);
            if (document == null) return false;

            // 2. Thực hiện xóa trong Oracle
            bool isDeleted = await _claimDocumentRepo.DeleteDocumentAsync(docId);

            if (isDeleted)
            {
                try
                {
                    // 3. Xóa file vật lý trên ổ đĩa
                    // Kết hợp WebRootPath (wwwroot) với FilePath (/uploads/...)
                    string fullPath = Path.Combine(_env.WebRootPath, document.FilePath.TrimStart('/'));

                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }
                catch (Exception ex)
                {
                    // Log lỗi xóa file nhưng không làm dừng luồng trả về 
                    // vì DB đã xóa thành công rồi
                    Console.WriteLine($"Lỗi khi xóa file vật lý: {ex.Message}");
                }

                // 4. ĐỒNG BỘ XÓA SANG MONGODB (Xóa cả bản ghi Mongo và Cache Redis)
                await SyncToMongoAsync(new ClaimDocumentDto { DocId = docId }, "DELETE");
            }

            return isDeleted;
        }

        public async Task<bool> UpdateDocumentMetadataAsync(ClaimDocumentDto document)
        {
            // 1. Update Oracle
            bool isUpdated = await _claimDocumentRepo.UpdateDocumentAsync(document);

            // 2. ĐỒNG BỘ SANG MONGODB (POST /sync - vì hàm bên Mongo là Upsert)
            if (isUpdated)
            {
                await SyncToMongoAsync(document, "POST");
            }

            return isUpdated;
        }

        public async Task<int> UploadDocumentAsync(int claimId, IFormFile file)
        {
            // 1. Xử lý lưu file vật lý vào wwwroot
            string rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string subFolder = Path.Combine("uploads", "claim-documents");
            string fullFolderPath = Path.Combine(rootPath, subFolder);

            if (!Directory.Exists(fullFolderPath))
                Directory.CreateDirectory(fullFolderPath);

            string fileName = $"{Guid.NewGuid()}_{file.FileName}";
            string fullPath = Path.Combine(fullFolderPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 2. Lưu vào cơ sở dữ liệu Oracle
            var dto = new ClaimDocumentDto
            {
                ClaimId = claimId,
                FileName = file.FileName,
                FilePath = $"/uploads/claim-documents/{fileName}".Replace("\\", "/")
            };

            int newDocId = await _claimDocumentRepo.CreateDocumentAsync(dto);

            // 3. ĐỒNG BỘ SANG MONGODB (POST /sync)
            if (newDocId > 0)
            {
                dto.DocId = newDocId; // Gán ID vừa tạo từ Oracle
                await SyncToMongoAsync(dto, "POST");
            }

            return newDocId;
        }

        // Hàm helper gọi API sang MongoDB Service
        private async Task SyncToMongoAsync(ClaimDocumentDto dto, string method)
        {
            try
            {
                // Sử dụng Named Client đã cấu hình trong Program.cs để tối ưu connection pooling
                var client = _httpClientFactory.CreateClient("MongoSyncClient");
                HttpResponseMessage response;

                // Lưu ý: URL phải trỏ đúng về phía Mongo Controller (ví dụ cổng 8080 của service 'api')
                string baseUrl = "http://api:8080/api/MongoClaimDocument";

                if (method.ToUpper() == "POST")
                {
                    // Gọi sang endpoint Upsert (Thêm/Sửa)
                    response = await client.PostAsJsonAsync($"{baseUrl}/sync", dto);
                }
                else if (method.ToUpper() == "DELETE")
                {
                    // Gọi sang endpoint Xóa
                    response = await client.DeleteAsync($"{baseUrl}/sync/{dto.DocId}");
                }
                else
                {
                    return;
                }

                if (!response.IsSuccessStatusCode)
                {
                    // Đồng bộ log lỗi giống như bên ClaimSync
                    Console.WriteLine($"Cảnh báo: Đồng bộ tài liệu {dto.DocId} sang MongoDB thất bại. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Tránh làm treo luồng nghiệp vụ chính của Oracle nếu service Mongo chết
                Console.WriteLine($"Lỗi hệ thống khi kết nối đồng bộ MongoDB: {ex.Message}");
            }
        }
    }
}
