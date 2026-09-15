# ArchiSpace3D Frontend (MAUI)

ArchiSpace3D es una plataforma innovadora diseñada para arquitectos y clientes, que permite gestionar proyectos, organizar mediciones y visualizar espacios físicos mediante Realidad Aumentada (AR).

## Características Principales

*   **Diseño Moderno (Apple Style):** Interfaz limpia, minimalista y completamente responsiva, con animaciones fluidas a 60FPS (Fade In, Slide Up) y diseño adaptativo con soporte para Modo Oscuro y Modo Claro.
*   **Regla AR Integrada (WebXR Markerless):** Herramienta de medición 3D de alta precisión que escanea superficies físicas (pisos, paredes) y permite dibujar mediciones flotantes en el espacio real. Implementada con Three.js y WebXR (Hit-Test), funcionando 100% sin necesidad de marcadores físicos (códigos impresos).
*   **Autenticación Robusta:** Inicio de sesión y registro seguros, enlazados a Supabase PostgreSQL y autenticación .NET Core.
*   **Gestión de Proyectos:** Los Arquitectos pueden crear, editar, eliminar y generar códigos de vinculación de proyectos. Los Clientes pueden unirse a proyectos mediante estos códigos.
*   **Perfiles de Usuario:** Actualización y sincronización de datos de perfil, incluyendo carga de avatar almacenado de forma remota.

## Tecnologías Utilizadas

*   **.NET MAUI:** Framework de frontend multiplataforma (probado exhaustivamente en Android).
*   **C# & XAML:** Estructura y lógica de las vistas.
*   **Three.js & WebXR:** Motor 3D empotrado para la experiencia de Realidad Aumentada.
*   **REST APIs:** Comunicación en tiempo real con el backend (ASP.NET Core).

## Instrucciones de Compilación y Ejecución

1.  Abre la carpeta del proyecto en **Visual Studio 2022** (asegúrate de tener instalada la carga de trabajo de .NET MAUI).
2.  Restaura los paquetes NuGet.
3.  Selecciona tu dispositivo de destino (Ej. Emulador de Android o un dispositivo físico por USB).
4.  Presiona **Ejecutar** (F5) para iniciar la compilación y la depuración en vivo.

> **Nota para Pruebas AR:** La función de medición (Regla) requiere que el dispositivo Android físico soporte **ARCore** y tenga instalado **Google Play Services for AR**.
