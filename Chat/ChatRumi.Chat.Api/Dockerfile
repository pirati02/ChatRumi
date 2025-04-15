FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Chat/ChatRumi.Chat.Api/ChatRumi.Chat.Api.csproj", "Chat/ChatRumi.Chat.Api/"]
COPY ["Chat/ChatRumi.Chat.Application/ChatRumi.Chat.Application.csproj", "Chat/ChatRumi.Chat.Application/"]
COPY ["Chat/ChatRumi.Chat.Domain/ChatRumi.Chat.Domain.csproj", "Chat/ChatRumi.Chat.Domain/"]
COPY ["ChatRumi.Kernel/ChatRumi.Kernel.csproj", "ChatRumi.Kernel/"]
COPY ["ChatRum.InterCommunication/ChatRum.InterCommunication.csproj", "ChatRum.InterCommunication/"]
RUN dotnet restore "Chat/ChatRumi.Chat.Api/ChatRumi.Chat.Api.csproj"
COPY . .
WORKDIR "/src/Chat/ChatRumi.Chat.Api"
RUN dotnet build "ChatRumi.Chat.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ChatRumi.Chat.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatRumi.Chat.Api.dll"]
