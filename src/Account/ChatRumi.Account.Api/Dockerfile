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
# Copy project files for restore
COPY ["src/Account/ChatRumi.Account.Api/ChatRumi.Account.Api.csproj", "src/Account/ChatRumi.Account.Api/"]
COPY ["src/Account/ChatRumi.Account.Application/ChatRumi.Account.Application.csproj", "src/Account/ChatRumi.Account.Application/"]
COPY ["src/Account/ChatRumi.Account.Domain/ChatRumi.Account.Domain.csproj", "src/Account/ChatRumi.Account.Domain/"]
COPY ["src/Account/ChatRumi.Account.Infrastructure/ChatRumi.Account.Infrastructure.csproj", "src/Account/ChatRumi.Account.Infrastructure/"]
COPY ["src/ChatRumi.Kernel/ChatRumi.Kernel.csproj", "src/ChatRumi.Kernel/"]
COPY ["src/ChatRumi.Infrastructure/ChatRumi.Infrastructure.csproj", "src/ChatRumi.Infrastructure/"]
COPY ["src/ChatRum.InterCommunication/ChatRum.InterCommunication.csproj", "src/ChatRum.InterCommunication/"]
RUN dotnet restore "src/Account/ChatRumi.Account.Api/ChatRumi.Account.Api.csproj"
COPY . .
WORKDIR "/src/src/Account/ChatRumi.Account.Api"
RUN dotnet build "ChatRumi.Account.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ChatRumi.Account.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatRumi.Account.Api.dll"]
