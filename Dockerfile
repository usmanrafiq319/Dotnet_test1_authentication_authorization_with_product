
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore Dotnet_test1_authentication_authorization_with_product.csproj

RUN dotnet publish Dotnet_test1_authentication_authorization_with_product.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet","Dotnet_test1_authentication_authorization_with_product.dll"]