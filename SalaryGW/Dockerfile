FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SalaryGW/SalaryGW.csproj", "SalaryGW/"]
RUN dotnet restore "SalaryGW/SalaryGW.csproj"
COPY . .
WORKDIR "/src/SalaryGW"
RUN dotnet build "SalaryGW.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SalaryGW.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SalaryGW.dll"] 