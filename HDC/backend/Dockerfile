FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restaurar primero (cachea los paquetes mientras no cambien los .csproj)
COPY HDC.sln ./
COPY HDC.Entity/HDC.Entity.csproj HDC.Entity/
COPY HDC.DAL/HDC.DAL.csproj HDC.DAL/
COPY HDC.BLL/HDC.BLL.csproj HDC.BLL/
COPY HDC.API/HDC.API.csproj HDC.API/
RUN dotnet restore HDC.API/HDC.API.csproj

COPY . .
RUN dotnet publish HDC.API/HDC.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080 \
    TZ=America/Argentina/Buenos_Aires
EXPOSE 8080
ENTRYPOINT ["dotnet", "HDC.API.dll"]
