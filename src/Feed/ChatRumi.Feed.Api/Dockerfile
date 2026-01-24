FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Central package versions
COPY ["Directory.Packages.props", "."]
COPY ["src/Feed/ChatRumi.Feed.Api/ChatRumi.Feed.Api.csproj", "src/Feed/ChatRumi.Feed.Api/"]
COPY ["src/Feed/ChatRumi.Feed.Application/ChatRumi.Feed.Application.csproj", "src/Feed/ChatRumi.Feed.Application/"]
COPY ["src/Feed/ChatRumi.Feed.Domain/ChatRumi.Feed.Domain.csproj", "src/Feed/ChatRumi.Feed.Domain/"]
COPY ["src/Feed/ChatRumi.Feed.Infrastructure/ChatRumi.Feed.Infrastructure.csproj", "src/Feed/ChatRumi.Feed.Infrastructure/"]
COPY ["src/ChatRumi.Kernel/ChatRumi.Kernel.csproj", "src/ChatRumi.Kernel/"]
COPY ["src/ChatRum.InterCommunication/ChatRum.InterCommunication.csproj", "src/ChatRum.InterCommunication/"]
RUN dotnet restore "src/Feed/ChatRumi.Feed.Api/ChatRumi.Feed.Api.csproj"
COPY . .
WORKDIR "/src/Feed/ChatRumi.Feed.Api"
RUN dotnet build "ChatRumi.Feed.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ChatRumi.Feed.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatRumi.Feed.Api.dll"]
