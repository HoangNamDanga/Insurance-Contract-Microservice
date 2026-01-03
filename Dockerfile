FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1. Copy toàn bộ mã nguồn vào (Thay vì copy từng file csproj lẻ tẻ)
# Việc này giúp giải quyết triệt để lỗi thiếu file khi restore qua .sln
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