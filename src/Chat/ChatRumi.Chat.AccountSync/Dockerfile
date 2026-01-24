FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Central package versions
COPY ["Directory.Packages.props", "."]
COPY ["src/Chat/ChatRumi.Chat.AccountSync/ChatRumi.Chat.AccountSync.csproj", "src/Chat/ChatRumi.Chat.AccountSync/"]
COPY ["src/Chat/ChatRumi.Chat.Application/ChatRumi.Chat.Application.csproj", "src/Chat/ChatRumi.Chat.Application/"]
COPY ["src/Chat/ChatRumi.Chat.Domain/ChatRumi.Chat.Domain.csproj", "src/Chat/ChatRumi.Chat.Domain/"]
COPY ["src/Chat/ChatRumi.Chat.Infrastructure/ChatRumi.Chat.Infrastructure.csproj", "src/Chat/ChatRumi.Chat.Infrastructure/"]
COPY ["src/ChatRumi.Kernel/ChatRumi.Kernel.csproj", "src/ChatRumi.Kernel/"]
COPY ["src/ChatRum.InterCommunication/ChatRum.InterCommunication.csproj", "src/ChatRum.InterCommunication/"]
RUN dotnet restore "src/Chat/ChatRumi.Chat.AccountSync/ChatRumi.Chat.AccountSync.csproj"
COPY . .
WORKDIR "/src/src/Chat/ChatRumi.Chat.AccountSync"
RUN dotnet build "ChatRumi.Chat.AccountSync.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ChatRumi.Chat.AccountSync.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatRumi.Chat.AccountSync.dll"]
