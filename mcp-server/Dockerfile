# Build from the repo root: the project compiles a few source files shared with brain/.
#   docker build -f mcp-server/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY mcp-server/src/McpServer.csproj mcp-server/src/
COPY brain/src/Skills/ISkill.cs brain/src/Skills/
COPY brain/src/Skills/Timers/TimersFile.cs brain/src/Skills/Timers/TimersOptions.cs brain/src/Skills/Timers/
RUN dotnet restore mcp-server/src/McpServer.csproj

COPY mcp-server/src/ mcp-server/src/
WORKDIR /source/mcp-server/src
RUN dotnet publish McpServer.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5555

ENTRYPOINT ["dotnet", "McpServer.dll"]
