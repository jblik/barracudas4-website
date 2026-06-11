FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Install Tailwind v4 standalone CLI (arch-aware)
RUN ARCH=$(uname -m) && \
    case "$ARCH" in \
        x86_64)  TW_ARCH="x64"   ;; \
        aarch64) TW_ARCH="arm64" ;; \
        *) echo "Unsupported arch: $ARCH" && exit 1 ;; \
    esac && \
    curl -sLo /usr/local/bin/tailwindcss \
        "https://github.com/tailwindlabs/tailwindcss/releases/latest/download/tailwindcss-linux-${TW_ARCH}" \
    && chmod +x /usr/local/bin/tailwindcss

COPY src/Barracudas.Web/Barracudas.Web.fsproj src/Barracudas.Web/
RUN dotnet restore src/Barracudas.Web/Barracudas.Web.fsproj -r linux-x64

COPY . .

RUN tailwindcss -i src/Barracudas.Web/assets/app.css \
               -o src/Barracudas.Web/wwwroot/css/site.css \
               --minify

RUN dotnet publish src/Barracudas.Web/Barracudas.Web.fsproj \
    -c Release -o /publish --no-restore -r linux-x64 --self-contained false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Barracudas.Web.dll"]
