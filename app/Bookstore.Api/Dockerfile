FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Bookstore.Api.csproj .
RUN dotnet restore
COPY . .
ARG VERSION=1.0.0
RUN dotnet publish -c Release -o /app /p:Version=$VERSION --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Bookstore.Api.dll"]
