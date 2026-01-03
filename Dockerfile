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

COPY . .

# 2. Restore đích danh project API 
# Việc này sẽ tự động kéo theo dependencies của Shared.Contracts, MongoDBCore, OracleSQLCore
RUN dotnet restore "CoNhungNgayMicroservice/CoNhungNgayMicroservice.csproj"

# 3. Build và Publish
WORKDIR "/src/CoNhungNgayMicroservice"
RUN dotnet publish "CoNhungNgayMicroservice.csproj" -c Release -o /app/publish

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Đảm bảo cổng khớp với docker-compose
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CoNhungNgayMicroservice.dll"]