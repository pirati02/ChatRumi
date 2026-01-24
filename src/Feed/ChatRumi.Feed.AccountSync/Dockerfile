FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Central package versions
COPY ["Directory.Packages.props", "."]
COPY ["src/Feed/ChatRumi.Feed.AccountSync/ChatRumi.Feed.AccountSync.csproj", "src/Feed/ChatRumi.Feed.AccountSync/"]
COPY ["src/Feed/ChatRumi.Feed.Application/ChatRumi.Feed.Application.csproj", "src/Feed/ChatRumi.Feed.Application/"]
COPY ["src/Feed/ChatRumi.Feed.Domain/ChatRumi.Feed.Domain.csproj", "src/Feed/ChatRumi.Feed.Domain/"]
COPY ["src/ChatRumi.Kernel/ChatRumi.Kernel.csproj", "src/ChatRumi.Kernel/"]
COPY ["src/ChatRum.InterCommunication/ChatRum.InterCommunication.csproj", "src/ChatRum.InterCommunication/"]
RUN dotnet restore "src/Feed/ChatRumi.Feed.AccountSync/ChatRumi.Feed.AccountSync.csproj"
COPY . .
WORKDIR "/src/src/Feed/ChatRumi.Feed.AccountSync"
RUN dotnet build "ChatRumi.Feed.AccountSync.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ChatRumi.Feed.AccountSync.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatRumi.Feed.AccountSync.dll"]
