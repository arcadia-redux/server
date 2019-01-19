FROM microsoft/dotnet:2.1-sdk-alpine AS build
WORKDIR /src
COPY Server/Server.csproj Server/
RUN dotnet restore Server/Server.csproj
COPY . .
WORKDIR /src/Server
RUN dotnet build Server.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Server.csproj -c Release -o /app

FROM microsoft/dotnet:2.1-aspnetcore-runtime
EXPOSE 80
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Server.dll"]
