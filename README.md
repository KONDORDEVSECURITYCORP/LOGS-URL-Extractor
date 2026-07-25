<div align="center">

```
██╗      ██████╗  ██████╗ ███████╗    ██╗   ██╗██████╗ ██╗
██║     ██╔═══██╗██╔════╝ ██╔════╝    ██║   ██║██╔══██╗██║
██║     ██║   ██║██║  ███╗███████╗    ██║   ██║██████╔╝██║
██║     ██║   ██║██║   ██║╚════██║    ██║   ██║██╔══██╗██║
███████╗╚██████╔╝╚██████╔╝███████║    ╚██████╔╝██║  ██║███████╗
╚══════╝ ╚═════╝  ╚═════╝ ╚══════╝     ╚═════╝ ╚═╝  ╚═╝╚══════╝
         E X T R A C T O R
```

**LOGS URL Extractor** — Extractor de credenciales y URLs de logs de stealers

[![Versión](https://img.shields.io/badge/versión-2.0.0-00d4ff?style=flat-square&logo=windows)](#)
[![.NET](https://img.shields.io/badge/.NET-9.0-7b2fff?style=flat-square&logo=dotnet)](#)
[![Plataforma](https://img.shields.io/badge/plataforma-Windows%2010%2B-0078d4?style=flat-square&logo=windows)](#)
[![Arquitectura](https://img.shields.io/badge/arch-x64-success?style=flat-square)](#)
[![Licencia](https://img.shields.io/badge/licencia-Propietario-ff3b5c?style=flat-square)](#)

<br/>

<img src="https://i.imgur.com/mSEt3JW.png" alt="LOGS URL Extractor — Interfaz principal" width="720" style="border-radius:10px; border:1px solid #00d4ff33;"/>

</div>

---

## Tabla de Contenidos

- [Descripción](#descripción)
- [Características](#características)
- [Capturas de pantalla](#capturas-de-pantalla)
- [Arquitectura](#arquitectura)
- [Requisitos](#requisitos)
- [Instalación](#instalación)
- [Uso](#uso)
- [Formatos soportados](#formatos-soportados)
- [Estructura del proyecto](#estructura-del-proyecto)
- [API interna JS↔C#](#api-interna-jsc)
- [Compilación](#compilación)
- [Seguridad y privacidad](#seguridad-y-privacidad)
- [Aviso legal](#aviso-legal)

---

## Descripción

**LOGS URL Extractor** es una herramienta de escritorio para Windows que procesa carpetas de logs de stealers y extrae automáticamente URLs, credenciales y pares `URL:login:password` en formato limpio y deduplicado.

La aplicación combina un backend en **.NET 9 / WPF** con una interfaz web moderna embebida vía **WebView2**, logrando una UI fluida con estética cyberpunk sin depender de frameworks de UI pesados.

> Herramienta diseñada exclusivamente para **análisis forense y auditoría de seguridad autorizada**.

---

## Características

| Función | Detalle |
|--------|---------|
| **Procesamiento paralelo** | Utiliza todos los núcleos disponibles (`Parallel.ForEach`) |
| **Múltiples formatos** | Detecta `URL:`, `Host:`, `Login:`, `Password:` y variantes |
| **Regex standalone** | Extrae URLs embebidas en texto libre |
| **Deduplicación** | Elimina entradas duplicadas antes de guardar |
| **Formato ULP** | Genera salida `url:login:password` compatible con checkers |
| **UI en tiempo real** | Contadores animados, barra de progreso, ticker de archivo |
| **Sin instalador** | Ejecutable autocontenido (`SelfContained=true`) |
| **Sin telemetría** | Cero conexiones de red en operación normal |
| **Extensiones escaneadas** | `.txt`, `.log`, `.csv`, `.dat` (recursivo) |

---

## Capturas de pantalla

<div align="center">

<img src="https://i.imgur.com/mSEt3JW.png" alt="LOGS URL Extractor — Vista principal" width="700"/>

*Interfaz principal — modo idle con diseño cyberpunk dark*

</div>

| Zona | Descripción |
|------|-------------|
| **Barra de título** | Icono hexagonal, nombre de app, versión y controles de ventana |
| **Directories** | Rutas de entrada/salida con botones Browse y acciones Start / Stop |
| **Live Statistics** | Contadores animados: Total · Procesados · Encontrados · Únicos |
| **Progress** | Barra de progreso con gradiente tricolor y porcentaje en tiempo real |
| **Status row** | Dot pulsante + texto de estado + badge `IDLE / RUNNING / DONE / ERROR` |

---

## Arquitectura

```
┌─────────────────────────────────────────────────────────────┐
│                   LOGS URL Extractor v2.0                    │
│                  WPF + WebView2 (win-x64)                    │
├──────────────────────────┬──────────────────────────────────┤
│   Proceso Principal WPF  │      WebView2 (Chromium)         │
│   ─────────────────────  │   ───────────────────────────    │
│   MainWindow.xaml.cs     │   Assets/index.html              │
│   ├── CoreWebView2       │   ├── CSS cyberpunk dark UI       │
│   ├── WebMessageReceived │   ├── Contadores animados         │
│   ├── OpenFolderDialog   │   ├── Barra de progreso           │
│   └── LogExtractor       │   └── JS bridge (chrome.webview) │
│       (procesamiento)    │                                   │
├──────────────────────────┴──────────────────────────────────┤
│                   Core/LogExtractor.cs                       │
│   ├── ProcessFolderAsync()  — orquesta el procesamiento      │
│   ├── ExtractFromFile()     — parseo línea a línea           │
│   ├── UrlRegex()            — regex compilada (.NET source)  │
│   └── FormatEntry()         — formatea url:login:password    │
└─────────────────────────────────────────────────────────────┘
```

### Flujo de datos

```
Carpeta de logs
      │
      ▼
Directory.GetFiles() ──► *.txt *.log *.csv *.dat (recursivo)
      │
      ▼
Parallel.ForEach (MaxDegree = ProcessorCount)
      │
      ├── Lectura del archivo (UTF-8)
      ├── Parseo línea a línea (prefijos de stealer)
      └── Regex sobre texto completo (URLs embebidas)
            │
            ▼
      ConcurrentBag<string>
            │
            ▼
      Distinct() + OrderBy()  ──► Deduplicación
            │
            ▼
      urls_YYYY-MM-DD_HH-mm-ss.txt
```

---

## Requisitos

| Componente | Versión mínima | Notas |
|-----------|----------------|-------|
| Windows | 10 (build 1809+) | WebView2 requiere Win10+ |
| WebView2 Runtime | Cualquier versión reciente | Incluido en Windows 11; descargable para Win10 |
| RAM | 256 MB | Recomendado 512 MB para logs grandes |
| Disco | 150 MB | Ejecutable autocontenido (~120 MB) |

> **Windows 10 / 11 con WebView2 preinstalado** — sin dependencias adicionales.

---

## Instalación

### Opción 1 — Ejecutable listo para usar

```
1. Descargar LogsUrlExtractor.exe desde la sección de releases
2. Ejecutar directamente — no requiere instalación
3. En Windows 10 sin WebView2, el SO pedirá instalarlo automáticamente
```

### Opción 2 — Compilar desde fuentes

Ver sección [Compilación](#compilación) más adelante.

---

## Uso

### Paso a paso

```
1. ▶ Abre LogsUrlExtractor.exe

2. 📁 INPUT  → Haz click en [Browse] y selecciona
              la carpeta raíz que contiene los logs
              (la app escanea recursivamente)

3. 📁 OUTPUT → Haz click en [Browse] para elegir
              dónde guardar los resultados
              (por defecto: <input>/extracted_urls/)

4. ▶ Haz click en [Start Extraction]

5. ⏳ Monitorea el progreso:
   • Barra de progreso con porcentaje
   • Contadores en tiempo real (Total / Procesados / Encontrados / Únicos)
   • Ticker con el archivo actual

6. ✅ Al finalizar, el resultado se guarda en:
   <output>/urls_YYYY-MM-DD_HH-mm-ss.txt
```

### Resultado de ejemplo

```
# LOGS URL Extractor | @KONDORDEVSECURITY | https://t.me/KONDORDEVSECURITY
# Date: 2026-07-25 11:30:00
# Files processed: 1250
# URLs found: 8743
# Unique entries: 3291

https://mail.empresa.com:usuario@corp.com:P@ssw0rd2024
https://vpn.target.org:jdoe:Hunter2!
https://panel.hosting.net/cpanel:webmaster:admin123
https://accounts.google.com/sign-in
...
```

### Atajos del teclado / ventana

| Acción | Control |
|--------|---------|
| Minimizar | Botón `—` en la barra de título |
| Maximizar / Restaurar | Botón `☐` en la barra de título |
| Cerrar | Botón `✕` en la barra de título |
| Mover ventana | Arrastrar la barra de título |
| Detener extracción | Botón `■ Stop` |

---

## Formatos soportados

La herramienta detecta los formatos más comunes de logs de stealers:

### Formato estructurado (Redline, Vidar, Raccoon, etc.)

```
URL: https://example.com/login
Login: usuario@dominio.com
Password: Secreto123!
```

También reconoce variantes:

| Campo URL | Campo Login | Campo Password |
|-----------|------------|----------------|
| `URL:` | `Login:` | `Password:` |
| `Host:` | `Username:` | `Pass:` |
| `Hostname:` | `User:` | `PASS:` |
| `Link:` | `Email:` | `Passwd:` |
| `Site:` | `LOGIN:` | — |

### Formato combo / texto libre

```
https://mail.corp.com:admin:password123
usuario@empresa.com:clave456
https://panel.host.net
```

La regex `(https?://[^\s\r\n"'<>]+)` captura cualquier URL HTTP/HTTPS presente en el texto.

### Extensiones escaneadas

```
*.txt   *.log   *.csv   *.dat
```

La búsqueda es **recursiva** — escanea todas las subcarpetas.

---

## Estructura del proyecto

```
LogsUrlExtractor/
├── Assets/
│   └── index.html          ← UI completa (HTML/CSS/JS embebida)
├── Core/
│   └── LogExtractor.cs     ← Motor de extracción paralela
├── Views/
│   ├── MainWindow.xaml     ← Ventana WPF (solo contiene WebView2)
│   └── MainWindow.xaml.cs  ← Lógica: bridge JS↔C#, diálogos, estado
├── App.xaml                ← Punto de entrada WPF
├── App.xaml.cs
└── LogsUrlExtractor.csproj ← Configuración de compilación
```

---

## API interna JS↔C#

La comunicación entre la UI (WebView2) y el proceso WPF se realiza mediante mensajes JSON.

### Mensajes JS → C# (`window.chrome.webview.postMessage`)

| Comando `cmd` | Descripción |
|--------------|-------------|
| `browseInput` | Abre diálogo para seleccionar carpeta de entrada |
| `browseOutput` | Abre diálogo para seleccionar carpeta de salida |
| `start` | Inicia la extracción |
| `stop` | Cancela la extracción en curso |
| `minimize` | Minimiza la ventana |
| `maximize` | Maximiza o restaura la ventana |
| `close` | Cierra la aplicación |
| `openLink` | Abre el Telegram de KONDORDEVSECURITY |

### Funciones C# → JS (`ExecuteScriptAsync`)

| Función JS | Parámetros | Descripción |
|-----------|------------|-------------|
| `setInputPath(path)` | `string` | Actualiza el campo de ruta de entrada |
| `setOutputPath(path)` | `string` | Actualiza el campo de ruta de salida |
| `setRunning(running)` | `bool` | Activa/desactiva botones, modo "ejecutando" |
| `updateStats(data)` | `{total, processed, found, unique, currentFile}` | Actualiza contadores y barra de progreso |
| `setDone(savedTo)` | `string` | Muestra resultado exitoso con ruta del archivo |
| `setError(msg)` | `string` | Muestra mensaje de error |
| `setStatus(text)` | `string` | Actualiza el texto de estado |

---

## Compilación

### Requisitos de desarrollo

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9) (Windows)
- Visual Studio 2022+ o VS Code con extensión C#
- Windows SDK (incluido con VS 2022)

### Compilar en modo Debug

```bash
dotnet build -c Debug
# Salida: bin/Debug/net9.0-windows/win-x64/
```

### Compilar release autocontenido

```bash
dotnet publish -c Release -r win-x64 --self-contained
# Salida: bin/Release/net9.0-windows/win-x64/publish/
```

### Generar ejecutable único (single-file)

Agrega al `.csproj`:

```xml
<PublishSingleFile>true</PublishSingleFile>
<PublishTrimmed>true</PublishTrimmed>
<TrimmerRootDescriptor>trimmer.xml</TrimmerRootDescriptor>
```

Luego:

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

> El HTML embebido (`Assets/index.html`) se incluye como recurso con
> `<EmbeddedResource>` en el `.csproj`, por lo que no necesita distribuirse por separado.

### Dependencias NuGet

| Paquete | Versión | Uso |
|---------|---------|-----|
| `Microsoft.Web.WebView2` | 1.0.3240.44 | Navegador embebido Chromium |

---

## Seguridad y privacidad

- **Sin conexiones de red** durante el procesamiento — toda la operación es local.
- **Sin telemetría** — no se envía ningún dato a servidores externos.
- **Contexto de menú deshabilitado** (`AreDefaultContextMenusEnabled = false`).
- **DevTools deshabilitado** en la WebView en producción.
- **Sin almacenamiento de resultados en la nube** — todo queda en el disco local.
- Los logs y credenciales procesados **nunca salen del equipo**.

---

## Aviso legal

> **IMPORTANTE:** Esta herramienta está diseñada exclusivamente para:
>
> - Auditorías de seguridad **autorizadas por escrito**
> - Análisis forense en sistemas propios
> - Investigación de incidentes de seguridad
> - Equipos de Red Team con contrato vigente
>
> **El uso no autorizado de esta herramienta contra sistemas de terceros es ilegal**
> y puede constituir un delito informático en múltiples jurisdicciones.
> El autor y KONDOR DEV SECURITY CORP no se responsabilizan del uso indebido.

---

<div align="center">

**KONDOR DEV SECURITY CORP**

*Seguridad Ofensiva &nbsp;·&nbsp; Red Team &nbsp;·&nbsp; Auditoría de Accesos*

[![Telegram](https://img.shields.io/badge/Telegram-@KONDORDEVSECURITY-26A5E4?style=flat-square&logo=telegram)](https://t.me/KONDORDEVSECURITY)

</div>
