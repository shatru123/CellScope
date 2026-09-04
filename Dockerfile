# Stage 1: Base Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Stage 2: Build SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/CellScope.Domain/CellScope.Domain.csproj", "src/CellScope.Domain/"]
COPY ["src/CellScope.Application/CellScope.Application.csproj", "src/CellScope.Application/"]
COPY ["src/CellScope.Infrastructure/CellScope.Infrastructure.csproj", "src/CellScope.Infrastructure/"]
COPY ["src/CellScope.Api/CellScope.Api.csproj", "src/CellScope.Api/"]
COPY ["src/CellScope.Web/CellScope.Web.csproj", "src/CellScope.Web/"]
COPY ["Directory.Build.props", "./"]

RUN dotnet restore "src/CellScope.Web/CellScope.Web.csproj"

COPY . .
WORKDIR "/src/src/CellScope.Web"
RUN dotnet build "CellScope.Web.csproj" -c Release -o /app/build

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish "CellScope.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:ErrorOnDuplicatePublishOutputFiles=false

# Stage 4: Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CellScope.Web.dll"]
