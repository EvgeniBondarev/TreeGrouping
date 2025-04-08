FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
# Указываем порт 80 в качестве URL для Kestrel
ENV ASPNETCORE_URLS=http://+:80
# Объявляем порт 80 для внешних подключений
EXPOSE 80

# Этап сборки приложения
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["TreeGrouping.Application/TreeGrouping.Application.csproj", "TreeGrouping.Application/"]
COPY ["TreeGrouping.Web/TreeGrouping.Web.csproj", "TreeGrouping.Web/"]

RUN dotnet restore "./TreeGrouping.Web/TreeGrouping.Web.csproj"
COPY . .
WORKDIR "/src/TreeGrouping.Web"
RUN dotnet build "./TreeGrouping.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этап публикации приложения
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TreeGrouping.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Финальный этап — готовый контейнер
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "TreeGrouping.Web.dll"]
