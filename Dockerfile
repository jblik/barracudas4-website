FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Install Tailwind v4 standalone CLI
RUN curl -sLo /usr/local/bin/tailwindcss \
    https://github.com/tailwindlabs/tailwindcss/releases/latest/download/tailwindcss-linux-x64 \
    && chmod +x /usr/local/bin/tailwindcss

COPY src/Barracudas.Web/Barracudas.Web.fsproj src/Barracudas.Web/
RUN dotnet restore src/Barracudas.Web/Barracudas.Web.fsproj

COPY . .

RUN tailwindcss -i src/Barracudas.Web/assets/app.css \
               -o src/Barracudas.Web/wwwroot/css/site.css \
               --minify

RUN dotnet publish src/Barracudas.Web/Barracudas.Web.fsproj \
    -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Barracudas.Web.dll"]
