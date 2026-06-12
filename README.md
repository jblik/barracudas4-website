# Zürich Barracudas — Team Website

Server-rendered website for the Zürich Barracudas baseball team (1. Liga).

## Stack

- **Backend:** F# / .NET 10 + Giraffe on ASP.NET Core
- **Views:** Giraffe.ViewEngine (type-safe HTML DSL)
- **Interactivity:** HTMX (no JS framework, no bundler)
- **Styling:** Tailwind v4 CLI (standalone binary, no npm)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Tailwind v4 standalone CLI](https://github.com/tailwindlabs/tailwindcss/releases/latest) — on macOS: `brew install tailwindcss`

## Develop locally

You need two terminals running in parallel.

**Terminal A — Tailwind watch**

```bash
tailwindcss -i src/Barracudas.Web/assets/app.css \
            -o src/Barracudas.Web/wwwroot/css/site.css \
            --watch
```

**Terminal B — backend with hot reload**

```bash
dotnet watch run --project src/Barracudas.Web --urls http://localhost:8080
```

The app runs on port **8080** (not 5000, which macOS ControlCenter/AirPlay occupies).

Open [http://localhost:8080](http://localhost:8080).

## Build for production (Docker)

```bash
docker build -t barracudas-web .
docker run -p 8080:8080 -e EasyScore__ApiKey=<key> barracudas-web
```

The multi-stage `Dockerfile` runs Tailwind and `dotnet publish` in the SDK image, then copies the output into a lean ASP.NET runtime image.

## Configuration

The EasyScore API key is read from the `EasyScore__ApiKey` environment variable (or `appsettings.Development.json` locally). The committed `appsettings.json` intentionally leaves it empty.
