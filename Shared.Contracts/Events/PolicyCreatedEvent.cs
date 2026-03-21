using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{

    //Dùng để tạo mới bản ghi ở MongoDB với đầy đủ thông tin định danh (Customer, Agent, Insurance Type
    public class PolicyCreatedEvent
    {
        public int PolicyId { get; set; }
        public string PolicyNumber { get; set; }

        // Các trường ID để đối soát
        public int CustomerId { get; set; }
        public int AgentId { get; set; }
        public int InsTypeId { get; set; }

        // Thông tin tên hiển thị (đã Enrich từ Oracle)
        public string CustomerName { get; set; }
        public string AgentName { get; set; }
        public string InsTypeName { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal PremiumAmount { get; set; }
        public string Status { get; set; }
        public string Action { get; set; } = "CREATE";

        // --- BỔ SUNG CÁC TRƯỜNG MỚI DƯỚI ĐÂY ---

        // 1. Thông tin xe (Dùng cho báo cáo hiệu suất theo hãng xe)
        public VehicleInfo Vehicle { get; set; } = new VehicleInfo();

        // 2. Danh sách bồi thường (Dùng để tính Loss Ratio)
        // Khởi tạo sẵn List trống để tránh lỗi NullReferenceException
        public List<ClaimInfo> Claims { get; set; } = new List<ClaimInfo>();
    }

    // Bạn cần định nghĩa thêm 2 class phụ này (nếu chưa có trong cùng namespace)
    public class VehicleInfo
    {
        public string Brand { get; set; }
        public string Model { get; set; }
    }

    public class ClaimInfo
    {
        public int ClaimId { get; set; }
        public decimal AmountApproved { get; set; }
        public string Status { get; set; }
    }
}
