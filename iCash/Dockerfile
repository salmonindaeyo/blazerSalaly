FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# คัดลอกไฟล์ .csproj เพื่อให้สามารถเรียกใช้ dotnet restore ได้
COPY ["iCash/iCash.csproj", "iCash/"]

RUN dotnet restore "iCash/iCash.csproj"

# คัดลอกโค้ดทั้งหมด
COPY . .
WORKDIR "/src/iCash"
RUN dotnet build "iCash.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "iCash.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "iCash.dll"] 