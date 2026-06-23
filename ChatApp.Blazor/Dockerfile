# ── Build stage: compile Blazor WebAssembly app ──
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["ChatApp.Blazor/ChatApp.Blazor.csproj", "ChatApp.Blazor/"]
RUN dotnet restore "ChatApp.Blazor/ChatApp.Blazor.csproj"

COPY . .
WORKDIR "/src/ChatApp.Blazor"
RUN dotnet publish "ChatApp.Blazor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ── Final stage: serve via nginx ──
FROM nginx:alpine
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY ChatApp.Blazor/nginx.conf /etc/nginx/nginx.conf

EXPOSE 80
