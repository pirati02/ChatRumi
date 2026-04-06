FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["src/Notification/ChatRumi.Notification.Api/ChatRumi.Notification.Api.csproj", "src/Notification/ChatRumi.Notification.Api/"]
COPY ["src/Notification/ChatRumi.Notification.Application/ChatRumi.Notification.Application.csproj", "src/Notification/ChatRumi.Notification.Application/"]
COPY ["src/Notification/ChatRumi.Notification.Infrastructure/ChatRumi.Notification.Infrastructure.csproj", "src/Notification/ChatRumi.Notification.Infrastructure/"]
COPY ["src/ChatRum.InterCommunication/ChatRum.InterCommunication.csproj", "src/ChatRum.InterCommunication/"]
COPY ["src/ChatRumi.Infrastructure/ChatRumi.Infrastructure.csproj", "src/ChatRumi.Infrastructure/"]
RUN dotnet restore "src/Notification/ChatRumi.Notification.Api/ChatRumi.Notification.Api.csproj"
COPY . .
WORKDIR "/src/src/Notification/ChatRumi.Notification.Api"
RUN dotnet build "ChatRumi.Notification.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ChatRumi.Notification.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatRumi.Notification.Api.dll"]
