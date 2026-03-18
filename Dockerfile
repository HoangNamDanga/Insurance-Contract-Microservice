# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy các file .csproj (vì context là "." nên đường dẫn tính từ CoNhungNgayMicroservice)
COPY Shared.Contracts/Shared.Contracts.csproj Shared.Contracts/
COPY MongoDBCore/MongoDBCore.csproj MongoDBCore/
COPY OracleSQLCore/OracleSQLCore.csproj OracleSQLCore/
COPY CoNhungNgayMicroservice/CoNhungNgayMicroservice.csproj CoNhungNgayMicroservice/
COPY Insurance.Tests/Insurance.Tests.csproj Insurance.Tests/

# Restore đích danh project chính
RUN dotnet restore "CoNhungNgayMicroservice/CoNhungNgayMicroservice.csproj"

# Copy toàn bộ code
COPY . .

# Build và Publish
WORKDIR "/src/CoNhungNgayMicroservice"
RUN dotnet publish "CoNhungNgayMicroservice.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CoNhungNgayMicroservice.dll"]