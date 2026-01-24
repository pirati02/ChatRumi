FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copy central package management props so PackageReference versions are resolved during restore
COPY ["Directory.Packages.props", "."]
# Copy project file and restore
COPY ["src/ChatRum.Gateway/ChatRum.Gateway.csproj", "src/ChatRum.Gateway/"]
RUN dotnet restore "src/ChatRum.Gateway/ChatRum.Gateway.csproj"
# Copy the rest of the source and build
COPY . .
WORKDIR "/src/src/ChatRum.Gateway"
RUN dotnet build "ChatRum.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ChatRum.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatRum.Gateway.dll"]
