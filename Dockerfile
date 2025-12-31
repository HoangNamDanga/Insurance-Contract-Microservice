FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1. Copy file Solution
COPY CoNhungNgayMicroservice.sln ./

# 2. Tạo các thư mục tương ứng và Copy các file .csproj vào đúng chỗ
# Việc này giúp lệnh 'dotnet restore' ở file .sln hoạt động chính xác
COPY Shared.Contracts/Shared.Contracts.csproj Shared.Contracts/
COPY CoNhungNgayMicroservice/CoNhungNgayMicroservice.csproj CoNhungNgayMicroservice/
COPY MongoDBCore/MongoDBCore.csproj MongoDBCore/
COPY OracleSQLCore/OracleSQLCore.csproj OracleSQLCore/
COPY Insurance.Tests/Insurance.Tests.csproj Insurance.Tests/
# 3. Restore toàn bộ các dependencies dựa trên file .sln
RUN dotnet restore

# 4. Copy toàn bộ mã nguồn của tất cả các project vào container
COPY . .

# 5. Chuyển đến thư mục dự án cần chạy và Build
WORKDIR /src/CoNhungNgayMicroservice
RUN dotnet publish -c Release -o /app/publish

# --- Stage 2: Runtime (Giúp Image nhẹ hơn - Tùy chọn nhưng nên làm) ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CoNhungNgayMicroservice.dll"]