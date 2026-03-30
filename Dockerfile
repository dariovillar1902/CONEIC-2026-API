FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Coneic.Api/Coneic.Api.csproj Coneic.Api/
RUN dotnet restore Coneic.Api/Coneic.Api.csproj
COPY . .
RUN dotnet publish Coneic.Api/Coneic.Api.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "Coneic.Api.dll"]
