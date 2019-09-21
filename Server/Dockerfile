FROM mcr.microsoft.com/dotnet/core/sdk:3.0-alpine AS build
WORKDIR /src
COPY Server/Server.csproj Server/
RUN dotnet restore Server/Server.csproj
COPY . .
WORKDIR /src/Server
RUN dotnet publish Server.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.0
RUN apt-get update \
 && apt-get install -y --no-install-recommends libgdiplus libc6-dev \
 && apt-get clean \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
ENTRYPOINT ["dotnet", "Server.dll"]
