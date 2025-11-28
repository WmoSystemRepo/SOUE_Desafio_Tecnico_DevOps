FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY MockSecurePay/MockSecurePay.csproj MockSecurePay/

RUN dotnet restore MockSecurePay/MockSecurePay.csproj

COPY MockSecurePay/ MockSecurePay/

WORKDIR /src/MockSecurePay
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "MockSecurePay.dll"]

