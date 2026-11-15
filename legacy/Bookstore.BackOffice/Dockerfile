# escape=`
# Windows images are built by the classic builder: no "# syntax", heredoc, RUN --mount or ADD --checksum
ARG WINDOWS_VERSION=ltsc2025

FROM mcr.microsoft.com/dotnet/framework/sdk:4.8.1-windowsservercore-${WINDOWS_VERSION} AS build
WORKDIR C:\src
COPY legacy/Bookstore.BackOffice/Bookstore.BackOffice.csproj legacy/Bookstore.BackOffice/
RUN msbuild legacy\Bookstore.BackOffice\Bookstore.BackOffice.csproj -t:Restore -v:minimal
COPY legacy/Bookstore.BackOffice/ legacy/Bookstore.BackOffice/
RUN msbuild legacy\Bookstore.BackOffice\Bookstore.BackOffice.csproj -p:Configuration=Release -v:minimal

FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8.1-windowsservercore-${WINDOWS_VERSION} AS final
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]
# LogMonitor sends IIS logs and Application event log errors to stdout (docker logs)
RUN New-Item -ItemType Directory C:\LogMonitor | Out-Null; `
    Invoke-WebRequest -Uri https://github.com/microsoft/windows-container-tools/releases/download/v2.2.1/LogMonitor.exe -OutFile C:\LogMonitor\LogMonitor.exe; `
    if ((Get-FileHash C:\LogMonitor\LogMonitor.exe -Algorithm SHA256).Hash -ne '9f2c6dc34d363595afefddc4d4f21a5ecea332c30578c2b89e0f9ed7a4412191') { throw 'LogMonitor.exe checksum mismatch' }
COPY legacy/Bookstore.BackOffice/LogMonitor/LogMonitorConfig.json C:\LogMonitor\
WORKDIR C:\inetpub\wwwroot
COPY --from=build C:\src\legacy\Bookstore.BackOffice\bin\ bin\
COPY legacy/Bookstore.BackOffice/Web.config legacy/Bookstore.BackOffice/prices.ashx legacy/Bookstore.BackOffice/health.ashx ./
EXPOSE 80
ENTRYPOINT ["C:\\LogMonitor\\LogMonitor.exe", "C:\\ServiceMonitor.exe", "w3svc"]
