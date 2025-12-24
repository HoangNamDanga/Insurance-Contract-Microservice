FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1. Copy solution + tất cả csproj
COPY CoNhungNgayMicroservice.sln ./
COPY CoNhungNgayMicroservice/*.csproj CoNhungNgayMicroservice/
COPY MongoDBCore/*.csproj MongoDBCore/
COPY OracleSQLCore/*.csproj OracleSQLCore/

# 2. Restore tất cả dự án (MassTransit sẽ được restore đúng scope)
RUN dotnet restore

# 3. Copy toàn bộ source code
COPY . .

# 4. Build & publish project chính
WORKDIR /src/CoNhungNgayMicroservice
RUN dotnet publish -c Release -o /app/publish

# 5. Chạy API khi container start
WORKDIR /app/publish
ENTRYPOINT ["dotnet", "CoNhungNgayMicroservice.dll"]
