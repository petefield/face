# Build raylib 6.0 for direct DRM/KMS rendering (raylib-cs ships no linux-arm64 native lib)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS raylib
RUN apt-get update && apt-get install -y --no-install-recommends \
        build-essential git ca-certificates libdrm-dev libgbm-dev libegl-dev libgles-dev libasound2-dev \
    && rm -rf /var/lib/apt/lists/*
RUN git clone --depth 1 --branch 6.0 https://github.com/raysan5/raylib.git /raylib \
    && make -C /raylib/src -j"$(nproc)" PLATFORM=PLATFORM_DRM RAYLIB_LIBTYPE=SHARED

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/face.csproj src/
RUN dotnet restore src/face.csproj
COPY src/ src/
RUN dotnet publish src/face.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends \
        libdrm2 libgbm1 libegl1 libgles2 libgl1-mesa-dri libasound2t64 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=raylib /raylib/src/libraylib.so* /app/
RUN mkdir -p runtimes/linux-arm64/native && cp -L libraylib.so runtimes/linux-arm64/native/libraylib.so \
    && mkdir /data

ENV LD_LIBRARY_PATH=/app \
    ASPNETCORE_URLS=http://0.0.0.0:5000 \
    SERVO_ANGLES_FILE=/data/servo-angles.json
EXPOSE 5000

# Runs as root: DRM master and /dev/i2c-1 access need device permissions
ENTRYPOINT ["dotnet", "face.dll"]
