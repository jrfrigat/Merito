# Build context: the repository root (docker compose and CI pass `.`).
#
# The SDK tag is pinned to the global.json floor; raise both together. A floating `sdk:10.0` tag can
# resolve below the floor, and restore then fails before reading a project.

# -- Stage 1: project files only, so restore stays layer-cached without a hand-kept csproj list --
FROM mcr.microsoft.com/dotnet/sdk:10.0.400 AS projects
WORKDIR /src
COPY . .
RUN mkdir -p /projects && \
    find . -type f \( -name "*.csproj" -o -name "Directory.Build.props" -o -name "Directory.Packages.props" \
        -o -name "global.json" \) \
        -exec install -D {} /projects/{} \;

# -- Stage 2: restore and publish --
FROM mcr.microsoft.com/dotnet/sdk:10.0.400 AS build
WORKDIR /repo
COPY --from=projects /projects .
RUN dotnet restore "src/Merito.Server/Merito.Server.csproj"
COPY . .
RUN dotnet publish "src/Merito.Server/Merito.Server.csproj" -c Release -o /publish --no-restore

# -- Stage 3: runtime --
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Merito.Server.dll"]
