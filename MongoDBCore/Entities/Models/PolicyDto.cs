using MongoDB.Bson.Serialization.Attributes;

public class PolicyDto
{
    [BsonId]
    public int PolicyId { get; set; }

    // Thêm [BsonIgnoreIfNull] để nếu Oracle không gửi trường này, 
    // Mongo sẽ không lưu trường đó vào DB thay vì lưu giá trị null.
    [BsonIgnoreIfNull]
    public string? PolicyNumber { get; set; }

    public int CustomerId { get; set; }
    public int AgentId { get; set; }
    public int InsTypeId { get; set; }

    public string? CustomerName { get; set; }
    public string? AgentName { get; set; }
    public string? InsTypeName { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal PremiumAmount { get; set; }
    public string? Status { get; set; }

    // QUAN TRỌNG: Gán giá trị mặc định để tránh lỗi null reference khi xử lý logic
    public VehicleInfo Vehicle { get; set; } = new VehicleInfo();
    public List<ClaimInfo> Claims { get; set; } = new List<ClaimInfo>();

    public class VehicleInfo
    {
        // Cho phép null và báo cho MongoDB biết là có thể bỏ qua nếu null
        [BsonIgnoreIfNull]
        public string? Brand { get; set; }

        [BsonIgnoreIfNull]
        public string? Model { get; set; }
    }

    public class ClaimInfo
    {
        public int ClaimId { get; set; }
        public decimal AmountApproved { get; set; }
        public string? Status { get; set; }
    }
}