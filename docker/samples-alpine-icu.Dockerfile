FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish samples/Bookstore.Bcl.Samples -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine
# The alpine image has no ICU and runs in invariant mode by default.
RUN apk add --no-cache icu-data-full icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Bookstore.Bcl.Samples.dll"]
