FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY OracleDbExample.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish -r linux-x64 --self-contained false \
  -p:PublishSingleFile=false \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -p:AllowedReferenceRelatedFileExtensions=- \
  -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OracleDbExample.dll"]