FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SalaryProcessor/SalaryProcessor.csproj", "SalaryProcessor/"]
RUN dotnet restore "SalaryProcessor/SalaryProcessor.csproj"
COPY . .
WORKDIR "/src/SalaryProcessor"
RUN dotnet build "SalaryProcessor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SalaryProcessor.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SalaryProcessor.dll"] 