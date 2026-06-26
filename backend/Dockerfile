FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY RunBase.slnx ./
COPY src/RunBase.Api/RunBase.Api.csproj src/RunBase.Api/
COPY src/RunBase.Application/RunBase.Application.csproj src/RunBase.Application/
COPY src/RunBase.Domain/RunBase.Domain.csproj src/RunBase.Domain/
COPY src/RunBase.Infrastructure/RunBase.Infrastructure.csproj src/RunBase.Infrastructure/

RUN dotnet restore src/RunBase.Api/RunBase.Api.csproj

COPY src/ src/
RUN dotnet publish src/RunBase.Api/RunBase.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RunBase.Api.dll"]
