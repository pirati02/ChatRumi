FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Account/ChatRumi.Account.Api/ChatRumi.Account.Api.csproj", "Account/ChatRumi.Account.Api/"]
COPY ["Account/ChatRumi.Account.Application/ChatRumi.Account.Application.csproj", "Account/ChatRumi.Account.Application/"]
COPY ["Account/ChatRumi.Account.Domain/ChatRumi.Account.Domain.csproj", "Account/ChatRumi.Account.Domain/"]
COPY ["ChatRumi.Kernel/ChatRumi.Kernel.csproj", "ChatRumi.Kernel/"]
COPY ["ChatRum.InterCommunication/ChatRum.InterCommunication.csproj", "ChatRum.InterCommunication/"]
RUN dotnet restore "Account/ChatRumi.Account.Api/ChatRumi.Account.Api.csproj"
COPY . .
WORKDIR "/src/Account/ChatRumi.Account.Api"
RUN dotnet build "ChatRumi.Account.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ChatRumi.Account.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatRumi.Account.Api.dll"]
