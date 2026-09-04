FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Application.Framework/Application.Framework.csproj Application.Framework/
COPY Application.Dto/Application.Dto.csproj Application.Dto/
COPY Domain.Customer/Domain.Customer.csproj Domain.Customer/
COPY Application.Contracts/Application.Contracts.csproj Application.Contracts/
COPY Customer.DomainServiceContract/Customer.DomainServiceContract.csproj Customer.DomainServiceContract/
COPY Application/Application.csproj Application/
COPY Infrastructure.Customer.Persistence/Infrastructure.Customer.Persistence.csproj Infrastructure.Customer.Persistence/
COPY Infrastructure.Security.Identity/Infrastructure.Security.Identity.csproj Infrastructure.Security.Identity/
COPY Customer.Bootstrap/Customer.Bootstrap.csproj Customer.Bootstrap/
COPY Security.Bootstrap/Security.Bootstrap.csproj Security.Bootstrap/
COPY CustomerSupport/CustomerSupport.csproj CustomerSupport/

RUN dotnet restore CustomerSupport/CustomerSupport.csproj

COPY . .
RUN dotnet publish CustomerSupport/CustomerSupport.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CustomerSupport.dll"]
