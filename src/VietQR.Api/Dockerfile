FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY ["Directory.Build.props", "./"]

COPY ["src/", "src/"]

RUN dotnet restore "src/VietQR.Api/VietQR.Api.csproj"
RUN dotnet publish "src/VietQR.Api/VietQR.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends libfontconfig1 fontconfig && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "VietQR.Api.dll"]
