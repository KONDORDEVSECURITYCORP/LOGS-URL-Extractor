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

# LOGS URL Extractor

**Extractor de credenciales ULP de logs de stealers — rápido, preciso y sin ruido**

[![Versión](https://img.shields.io/badge/versión-2.0.0-00d4ff?style=flat-square&logo=windows)](#)
[![.NET](https://img.shields.io/badge/.NET-9.0-7b2fff?style=flat-square&logo=dotnet)](#)
[![Plataforma](https://img.shields.io/badge/plataforma-Windows%2010%2B-0078d4?style=flat-square&logo=windows)](#)
[![Arquitectura](https://img.shields.io/badge/arch-x64-success?style=flat-square)](#)
[![Single-File](https://img.shields.io/badge/distribución-single--file%20.exe-ff8c00?style=flat-square)](#)
[![Licencia](https://img.shields.io/badge/licencia-Propietario-ff3b5c?style=flat-square)](#)

<br/>

<img src="https://i.imgur.com/mSEt3JW.png" alt="LOGS URL Extractor — Interfaz principal" width="720" style="border-radius:10px; border:1px solid #00d4ff33;"/>

</div>

---

## Tabla de Contenidos

- [Descripción](#descripción)
- [Novedades en la v2.0](#novedades-en-la-v20)
- [Características](#características)
- [Capturas de pantalla](#capturas-de-pantalla)
- [Formato de salida — ULP estricto](#formato-de-salida--ulp-estricto)
- [Arquitectura](#arquitectura)
- [Requisitos](#requisitos)
- [Instalación](#instalación)
- [Uso](#uso)
- [Formatos de entrada soportados](#formatos-de-entrada-soportados)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Compilación](#compilación)
- [Seguridad y privacidad](#seguridad-y-privacidad)
- [Aviso legal](#aviso-legal)

---

## Descripción

**LOGS URL Extractor** es una herramienta de escritorio para Windows que procesa carpetas de logs de stealers y extrae automáticamente credenciales en formato **ULP** (`url:login:password`) limpio y deduplicado, listo para alimentar un checker.

Combina un backend en **.NET 9 / WPF** con una interfaz web embebida vía **WebView2**, logrando una UI fluida con estética cyberpunk sin frameworks de UI pesados. Se distribuye como un **ejecutable único** (single-file self-contained): un solo `.exe` sin dependencias que instalar.

> Herramienta diseñada exclusivamente para **análisis forense y auditoría de seguridad autorizada**.

---

## Novedades en la v2.0

Esta versión es una reescritura profunda del motor de extracción y del empaquetado. Los cambios clave:

### Motor de extracción ULP-estricto
El extractor ahora emite **únicamente** tripletes válidos `host:login:password`. Todo lo demás se descarta de forma deliberada para que el archivo de salida sea compacto y 100% utilizable por un checker.

- **Solo ULP real** — se aceptan hosts con scheme (`https://`, `http://`, `ftp://`, `android://`), dominios con TLD válido (`mail.corp.com`) e IPv4 con puerto opcional (`10.0.0.5:8080`).
- **Se eliminan los datos inútiles** que antes inflaban la salida: URLs sueltas de historial de navegación, combos `email:password` sin URL, IPs huérfanas, volcados de drivers y texto basura.
- **Validación estricta** de cada campo: logins sin espacios ni caracteres de control, TLD alfabético de 2+ caracteres, rechazo de primer label puramente numérico.

### Detección robusta de encodings
Lectura best-effort que decodifica correctamente los logs reales de stealers:

- BOM UTF-8 / UTF-16 LE / UTF-16 BE / UTF-32.
- UTF-16 sin BOM (heurística por zero-bytes) — frecuente en RedLine, META, Vidar y LummaC2.
- Fallback a Windows-1252 / Latin-1 cuando el UTF-8 estricto falla.

### Parseo de múltiples pares por bloque
Un mismo bloque `URL:` con varios pares `Login:/Password:` ya no pierde credenciales: se emite un ULP por cada par.

### Distribución single-file
El release pasó a ser un **único `LogsUrlExtractor.exe`** autocontenido (self-contained, sin runtime de .NET preinstalado). Internamente empaqueta todo (runtime, WPF, WebView2) y se auto-extrae a `%TEMP%\.net` en el primer arranque.

> El arranque inicial toma 1-2 s la primera vez (extracción cacheada); las siguientes ejecuciones son instantáneas.

---

## Características

| Función | Detalle |
|--------|---------|
| **Salida ULP limpia** | Solo `url:login:password` — cero ruido, archivos pequeños |
| **Procesamiento paralelo** | Usa todos los núcleos disponibles (`Parallel.ForEach`) |
| **Multi-encoding** | UTF-8/16/32 (con/sin BOM) + Windows-1252/Latin-1 |
| **Múltiples formatos de log** | Detecta `URL:`, `Host:`, `Login:`, `Password:` y variantes |
| **Combos sin scheme** | Acepta `dominio.com:user:pass` e `IP:puerto:user:pass` |
| **Deduplicación** | Elimina entradas duplicadas antes de guardar |
| **UI en tiempo real** | Contadores animados, barra de progreso, ticker de archivo |
| **Single-file** | Un solo `.exe` autocontenido, sin instalador |
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

## Formato de salida — ULP estricto

La herramienta produce un único formato por línea:

```
host:login:password
```

Donde `host` puede ser:

| Tipo de host | Ejemplo |
|--------------|---------|
| URL con scheme | `https://mail.empresa.com/login:usuario@corp.com:P@ssw0rd2024` |
| Dominio sin scheme | `panel.hosting.net:webmaster:admin123` |
| IPv4 (con puerto opcional) | `10.0.0.5:8080:administrator:Admin!23` |

### Qué se descarta (y por qué)

Para mantener la salida compacta y útil, estas entradas **nunca** se escriben:

- ❌ URLs sueltas sin credenciales (historial de navegador: `https://youtu.be/...`, `https://google.com/search?...`)
- ❌ Combos `email:password` sin host (`alice@example.com:clave456`)
- ❌ IPs huérfanas (`8.8.8.8`) y volcados de sistema (`@system32\DRIVERS\...`)
- ❌ Combos numéricos sin dominio real (`01159031110:d3twdpxq`)
- ❌ Registros incompletos (URL sin password, login sin password, etc.)

### Ejemplo de archivo generado

```
# LOGS URL Extractor | @KONDORDEVSECURITY | https://t.me/KONDORDEVSECURITY
# Date: 2026-09-30 19:08:58
# Files processed: 24581
# ULP entries found: 15230
# Unique ULP entries: 12894
# Format: host:login:password  (host = http(s)/ftp URL, bare domain, or IPv4)

https://accounts.google.com/signin:victim@gmail.com:G00gl3Pass!
https://login.microsoftonline.com:user@corp.com:Corp@2024
mail.corp.co:cfo:Cfo!2025
10.0.0.5:administrator:Admin!23
```

---

## Arquitectura

```
┌─────────────────────────────────────────────────────────────┐
│                   LOGS URL Extractor v2.0                    │
│             WPF + WebView2 (win-x64, single-file)            │
├──────────────────────────┬──────────────────────────────────┤
│   Proceso Principal WPF  │      WebView2 (Chromium)         │
│   ─────────────────────  │   ───────────────────────────    │
│   App.xaml.cs            │   Assets/index.html              │
│   ├── OnStartup()        │   ├── CSS cyberpunk dark UI       │
│   │   (crea MainWindow   │   ├── Contadores animados         │
│   │    programáticamente)│   ├── Barra de progreso           │
│   └── Crash handler      │   └── JS bridge (chrome.webview) │
│   MainWindow.xaml.cs     │                                   │
│   ├── CoreWebView2       │                                   │
│   ├── WebMessageReceived │                                   │
│   └── LogExtractor       │                                   │
├──────────────────────────┴──────────────────────────────────┤
│                   Core/LogExtractor.cs                       │
│   ├── ProcessFolderAsync()  — orquesta el procesamiento      │
│   ├── ExtractFromFile()     — parseo ULP-estricto            │
│   ├── NormalizeHost()       — valida URL / dominio / IPv4    │
│   ├── TryParseUlpCombo()    — combos host:user:pass          │
│   └── ReadFileWithBestEncoding() — detección de encoding     │
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
      ├── Lectura con detección de encoding (UTF-8/16/32, CP1252)
      ├── Parseo por prefijos de stealer (URL:/Login:/Password:)
      └── Parseo de combos ULP en líneas libres
            │
            ▼
      Validación host:login:password (NormalizeHost)
            │
            ▼
      ConcurrentBag<string>  ──►  Distinct() + OrderBy()
            │
            ▼
      urls_YYYY-MM-DD_HH-mm-ss.txt   (solo ULP)
```

---

## Requisitos

| Componente | Versión mínima | Notas |
|-----------|----------------|-------|
| Windows | 10 (build 1809+) | WebView2 requiere Win10+ |
| WebView2 Runtime | Cualquier versión reciente | Incluido en Windows 11; descargable para Win10 |
| RAM | 256 MB | Recomendado 512 MB para logs grandes |
| Disco | 150 MB | Ejecutable autocontenido (~57 MB) + extracción temporal |

> **Windows 10 / 11 con WebView2 preinstalado** — sin dependencias de .NET adicionales (el runtime va dentro del `.exe`).

---

## Instalación

### Opción 1 — Ejecutable listo para usar (recomendado)

```
1. Descargar LogsUrlExtractor-v2.0-win-x64.zip desde Releases
2. Descomprimir — contiene un único LogsUrlExtractor.exe
3. Ejecutar directamente (doble clic). No requiere instalación.
   • En Windows 10 sin WebView2, el SO ofrecerá instalarlo.
   • La primera ejecución tarda 1-2 s extra (auto-extracción del bundle).
```

### Opción 2 — Compilar desde fuentes

Ver sección [Compilación](#compilación) más adelante.

---

## Uso

### Paso a paso

```
1. ▶ Abre LogsUrlExtractor.exe

2. 📁 INPUT  → [Browse] y selecciona la carpeta raíz con los logs
              (se escanea recursivamente)

3. 📁 OUTPUT → [Browse] para elegir dónde guardar los resultados
              (por defecto: <input>/extracted_urls/)

4. ▶ Click en [Start Extraction]

5. ⏳ Monitorea el progreso en tiempo real:
   • Barra de progreso con porcentaje
   • Contadores (Total / Procesados / Encontrados / Únicos)
   • Ticker con el archivo actual

6. ✅ Resultado guardado en:
   <output>/urls_YYYY-MM-DD_HH-mm-ss.txt
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

## Formatos de entrada soportados

La herramienta detecta los formatos más comunes de logs de stealers:

### Formato estructurado (Redline, Vidar, Raccoon, META, Lumma, etc.)

```
URL: https://example.com/login
Login: usuario@dominio.com
Password: Secreto123!
```

Reconoce variantes de prefijo:

| Campo URL | Campo Login | Campo Password |
|-----------|------------|----------------|
| `URL:` `Host:` `Hostname:` | `Login:` `Username:` `User:` | `Password:` `Pass:` `PASS:` |
| `Link:` `Site:` `Website:` | `Email:` `E-mail:` `Usr:` | `Passwd:` `PWD:` `PW:` |
| `Origin:` `Location:` `SoftURL:` | `UserName:` `User Name:` | — |

### Formato combo / ULP directo

```
https://mail.corp.com:admin:password123
panel.host.net:root:toor
10.0.0.5:8080:administrator:Admin!23
```

### Encodings aceptados

```
UTF-8 (BOM y sin BOM) · UTF-16 LE/BE (BOM y sin BOM) · UTF-32 · Windows-1252 · Latin-1
```

### Extensiones escaneadas

```
*.txt   *.log   *.csv   *.dat     (búsqueda recursiva en subcarpetas)
```

---

## Estructura del proyecto

```
LogsUrlExtractor/
├── Assets/
│   └── index.html          ← UI completa (HTML/CSS/JS embebida)
├── Core/
│   └── LogExtractor.cs     ← Motor de extracción ULP-estricto
├── Views/
│   ├── MainWindow.xaml     ← Ventana WPF (contiene WebView2)
│   └── MainWindow.xaml.cs  ← Lógica: bridge JS↔C#, diálogos, estado
├── App.xaml                ← Punto de entrada WPF (sin StartupUri)
├── App.xaml.cs             ← Arranque programático + crash handler
└── LogsUrlExtractor.csproj ← Configuración de compilación single-file
```

---

## Compilación

### Requisitos de desarrollo

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9) (o superior)
- Visual Studio 2022+ o VS Code con extensión C#
- Windows SDK (incluido con VS 2022)

### Compilar en modo Debug

```bash
dotnet build -c Debug
```

### Publicar el ejecutable único (single-file self-contained)

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

Salida: `bin/Release/net9.0-windows/win-x64/publish/LogsUrlExtractor.exe`

Flags clave en el `.csproj` que hacen posible el single-file con WPF + WebView2:

```xml
<PublishSingleFile>true</PublishSingleFile>
<IncludeAllContentForSelfExtract>true</IncludeAllContentForSelfExtract>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
```

> El arranque se hace programáticamente en `App.OnStartup` (sin `StartupUri`), porque el resolver `pack://` de WPF no localiza el XAML embebido de forma fiable dentro de un bundle single-file.

### Dependencias NuGet

| Paquete | Versión | Uso |
|---------|---------|-----|
| `Microsoft.Web.WebView2` | 1.0.3240.44 | Navegador embebido Chromium |

---

## Seguridad y privacidad

- **Sin conexiones de red** durante el procesamiento — toda la operación es local.
- **Sin telemetría** — no se envía ningún dato a servidores externos.
- **Menú contextual deshabilitado** (`AreDefaultContextMenusEnabled = false`).
- **DevTools deshabilitado** en la WebView en producción.
- Los logs y credenciales procesados **nunca salen del equipo**.
- Logs de error (si los hay) se escriben solo localmente en `%LocalAppData%\LogsUrlExtractor\crash.log`.

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
