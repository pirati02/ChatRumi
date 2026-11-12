FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Ensure central package versions are available
COPY ["Directory.Packages.props", "."]
# Copy project files for restore, including dependencies
COPY ["src/Chat/ChatRumi.Chat.Api/ChatRumi.Chat.Api.csproj", "src/Chat/ChatRumi.Chat.Api/"]
COPY ["src/Chat/ChatRumi.Chat.Application/ChatRumi.Chat.Application.csproj", "src/Chat/ChatRumi.Chat.Application/"]
COPY ["src/Chat/ChatRumi.Chat.Domain/ChatRumi.Chat.Domain.csproj", "src/Chat/ChatRumi.Chat.Domain/"]
COPY ["src/Chat/ChatRumi.Chat.Infrastructure/ChatRumi.Chat.Infrastructure.csproj", "src/Chat/ChatRumi.Chat.Infrastructure/"]
COPY ["src/ChatRumi.Kernel/ChatRumi.Kernel.csproj", "src/ChatRumi.Kernel/"]
COPY ["src/ChatRumi.Infrastructure/ChatRumi.Infrastructure.csproj", "src/ChatRumi.Infrastructure/"]
COPY ["src/ChatRum.InterCommunication/ChatRum.InterCommunication.csproj", "src/ChatRum.InterCommunication/"]
RUN dotnet restore "src/Chat/ChatRumi.Chat.Api/ChatRumi.Chat.Api.csproj"
COPY . .
WORKDIR "/src/src/Chat/ChatRumi.Chat.Api"
RUN dotnet build "ChatRumi.Chat.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ChatRumi.Chat.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatRumi.Chat.Api.dll"]
