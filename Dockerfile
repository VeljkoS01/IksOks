FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY src/IksOks.Web/IksOks.Web.csproj src/IksOks.Web/

RUN dotnet restore src/IksOks.Web/IksOks.Web.csproj

COPY src/IksOks.Web/ src/IksOks.Web/

RUN dotnet publish src/IksOks.Web/IksOks.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "IksOks.Web.dll"]