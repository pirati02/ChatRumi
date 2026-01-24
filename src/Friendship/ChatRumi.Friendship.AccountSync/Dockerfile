FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Central package versions
COPY ["Directory.Packages.props", "."]
COPY ["src/Friendship/ChatRumi.Friendship.AccountSync/ChatRumi.Friendship.AccountSync.csproj", "src/Friendship/ChatRumi.Friendship.AccountSync/"]
COPY ["src/Friendship/ChatRumi.Friendship.Application/ChatRumi.Friendship.Application.csproj", "src/Friendship/ChatRumi.Friendship.Application/"]
COPY ["src/Friendship/ChatRumi.Friendship.Domain/ChatRumi.Friendship.Domain.csproj", "src/Friendship/ChatRumi.Friendship.Domain/"]
COPY ["src/ChatRumi.Kernel/ChatRumi.Kernel.csproj", "src/ChatRumi.Kernel/"]
COPY ["src/ChatRum.InterCommunication/ChatRum.InterCommunication.csproj", "src/ChatRum.InterCommunication/"]
RUN dotnet restore "src/Friendship/ChatRumi.Friendship.AccountSync/ChatRumi.Friendship.AccountSync.csproj"
COPY . .
WORKDIR "/src/Friendship/ChatRumi.Friendship.AccountSync"
RUN dotnet build "ChatRumi.Friendship.AccountSync.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ChatRumi.Friendship.AccountSync.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatRumi.Friendship.AccountSync.dll"]
