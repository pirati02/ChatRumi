FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Central package management versions
COPY ["Directory.Packages.props", "."]
# Copy project files for restore
COPY ["src/Friendship/ChatRumi.Friendship.Api/ChatRumi.Friendship.Api.csproj", "Friendship/ChatRumi.Friendship.Api/"]
COPY ["src/Friendship/ChatRumi.Friendship.Application/ChatRumi.Friendship.Application.csproj", "Friendship/ChatRumi.Friendship.Application/"]
COPY ["src/Friendship/ChatRumi.Friendship.Domain/ChatRumi.Friendship.Domain.csproj", "Friendship/ChatRumi.Friendship.Domain/"]
COPY ["src/ChatRumi.Kernel/ChatRumi.Kernel.csproj", "ChatRumi.Kernel/"]
COPY ["src/ChatRum.InterCommunication/ChatRum.InterCommunication.csproj", "ChatRum.InterCommunication/"]
RUN dotnet restore "Friendship/ChatRumi.Friendship.Api/ChatRumi.Friendship.Api.csproj"
COPY . .
WORKDIR "/src/src/Friendship/ChatRumi.Friendship.Api"
RUN dotnet build "ChatRumi.Friendship.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ChatRumi.Friendship.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatRumi.Friendship.Api.dll"]
