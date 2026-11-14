# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src
COPY nuget.config ./
COPY IsbnCheck/IsbnCheck.csproj IsbnCheck/
# The token exists only while this RUN runs, as the NUGET_TOKEN environment variable
RUN --mount=type=secret,id=nuget_token,env=NUGET_TOKEN,required=true \
    dotnet restore IsbnCheck/IsbnCheck.csproj
COPY IsbnCheck/ IsbnCheck/
RUN dotnet publish IsbnCheck/IsbnCheck.csproj -c Release -o /app --no-restore
ENTRYPOINT ["dotnet", "/app/IsbnCheck.dll"]
