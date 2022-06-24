FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["BuildVersionManager/BuildVersionManager.csproj", "BuildVersionManager/"]
RUN dotnet restore "BuildVersionManager/BuildVersionManager.csproj"
COPY . .
WORKDIR "/src/BuildVersionManager"
RUN dotnet build "BuildVersionManager.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BuildVersionManager.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BuildVersionManager.dll", "increment", "/src/build.json"]
