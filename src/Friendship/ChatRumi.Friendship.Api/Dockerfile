FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Friendship/ChatRumi.Friendship.Api/ChatRumi.Friendship.Api.csproj", "Friendship/ChatRumi.Friendship.Api/"]
COPY ["ChatRum.InterCommunication/ChatRum.InterCommunication.csproj", "ChatRum.InterCommunication/"]
COPY ["Friendship/ChatRumi.Friendship.Application/ChatRumi.Friendship.Application.csproj", "Friendship/ChatRumi.Friendship.Application/"]
COPY ["Friendship/ChatRumi.Friendship.Domain/ChatRumi.Friendship.Domain.csproj", "Friendship/ChatRumi.Friendship.Domain/"]
COPY ["ChatRumi.Kernel/ChatRumi.Kernel.csproj", "ChatRumi.Kernel/"]
RUN dotnet restore "Friendship/ChatRumi.Friendship.Api/ChatRumi.Friendship.Api.csproj"
COPY . .
WORKDIR "/src/Friendship/ChatRumi.Friendship.Api"
RUN dotnet build "ChatRumi.Friendship.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ChatRumi.Friendship.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatRumi.Friendship.Api.dll"]
