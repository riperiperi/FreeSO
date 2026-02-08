# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY TSOClient/ TSOClient/
COPY Other/libs/MSDFData/ Other/libs/MSDFData/
COPY Other/libs/TargaImagePCL/ Other/libs/TargaImagePCL/
RUN dotnet publish TSOClient/FSO.Server.Core/FSO.Server.Core.csproj \
    -c Release --self-contained false -o /app

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0

LABEL org.opencontainers.image.title="FreeSO Server"
LABEL org.opencontainers.image.source="https://github.com/riperiperi/FreeSO"
LABEL org.opencontainers.image.licenses="MPL-2.0"

WORKDIR /app
COPY --from=build /app .
COPY docker/entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh
EXPOSE 9000 33100 34100 35100
ENTRYPOINT ["/app/entrypoint.sh"]
