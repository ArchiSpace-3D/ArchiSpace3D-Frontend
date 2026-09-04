# ArchiSpace3D — Frontend Móvil (.NET MAUI)

<div align="center">

![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Android](https://img.shields.io/badge/Android-API%2024%2B-3DDC84?style=for-the-badge&logo=android&logoColor=white)
![Realidad Aumentada](https://img.shields.io/badge/ARCore-Google%20AR-4285F4?style=for-the-badge&logo=google&logoColor=white)
![Estilo](https://img.shields.io/badge/Design-Apple%20Cupertino-000000?style=for-the-badge&logo=apple&logoColor=white)

**Plataforma de visualización arquitectónica, medición en Realidad Aumentada y gestión colaborativa de obras y espacios 3D.**

</div>

---

## Descripción del Proyecto

**ArchiSpace3D** es una aplicación móvil desarrollada en **.NET MAUI** diseñada para arquitectos, ingenieros civiles y clientes. Permite gestionar proyectos de construcción, inspeccionar planos y espacios físicos, dimensionar estructuras, capturar medidas in situ mediante sensores de Realidad Aumentada y colaborar en tiempo real entre estudios de arquitectura y propietarios de obras.

La aplicación cuenta con una interfaz de usuario inspirada en el estándar de diseño **Apple Cupertino**, incorporando micro-animaciones fluidas, menús deslizables (*Bottom Sheets*), tipografía arquitectónica y gráficos vectoriales SVG limpios.

---

## Paleta de Color Arquitectónica

La identidad visual del proyecto utiliza una paleta cromática sofisticada con alto contraste y legibilidad:

| Tono | Código HEX | Muestra | Uso Principal |
| :--- | :---: | :---: | :--- |
| **Inkwell** | `#2C3639` | ![#2C3639](https://via.placeholder.com/15/2C3639/000000?text=+) | Fondo general de la aplicación y máxima profundidad. |
| **Lunar Eclipse** | `#3F4E4F` | ![#3F4E4F](https://via.placeholder.com/15/3F4E4F/000000?text=+) | Tarjetas de superficie, bordes de modales y cabeceras. |
| **Creme Brulee** | `#A27B5B` | ![#A27B5B](https://via.placeholder.com/15/A27B5B/000000?text=+) | Color de acento primario, botones de acción (*CTA*) e indicadores. |
| **Au Lait** | `#DCD7C9` | ![#DCD7C9](https://via.placeholder.com/15/DCD7C9/000000?text=+) | Tipografía principal, etiquetas claras y superficies iluminadas. |

---

## Módulos y Pestañas de la Aplicación

### 1. Inicio / Proyectos (`DashboardPage`)
- Visualización de proyectos activos, en diseño o en construcción.
- Creación rápida de proyectos con cálculo de presupuesto estimado y asignación de cliente.
- Detalle interactivo de obra mediante **Apple Bottom Sheet**: visualización de identificadores de sala AR, dirección, estado y opciones de borrado.
- Generación de códigos de invitación de obra para arquitectos y canje de códigos para clientes.
- Centro de notificaciones con lectura en tiempo real.

### 2. Diseño 3D & Espacios (`DesignPage`)
- **Espacios Físicos:** Configuración de dimensiones (ancho, largo, alto) con cálculo automático de superficie ($m^2$) y volumen ($m^3$), además de orientación azimutal.
- **Versiones de Diseño:** Control de versiones del proyecto (v1.0, v2.0), creación de iteraciones y asignación de la versión activa.
- **Elementos Estructurales:** Registro y visualización de paredes, columnas, vigas, puertas y ventanas con material y medidas exactas.
- **Modelos 3D Importados:** Explorador de modelos GLTF/OBJ vinculados al proyecto.

### 3. Mediciones AR & Sensores (`MainPage` / `ARPage`)
- Telemetría en tiempo real con sensores de hardware: acelerómetro, giroscopio y barómetro de altitud.
- Modo de Realidad Aumentada con integración ARCore para anclaje de puntos espaciales y cálculo de distancias euclidianas.
- Historial sincronizado con el backend: guardado de mediciones en la base de datos y eliminación desde la app.
- Selector de proyecto activo integrado en hoja deslizante sin alertas bloqueantes.

### 4. Perfil de Usuario (`ProfilePage`)
- Identificación de rol de usuario (**Arquitecto** o **Cliente**).
- Actualización de perfil profesional, número de teléfono y especialidad arquitectónica.
- Cierre de sesión seguro con limpieza de tokens de autenticación y estado.

---

## Paridad 100% con Controladores del Backend

El frontend consume de forma directa el API REST de `ArchiSpace3D.Api`:

| Controlador Backend | Endpoints Consumidos | Funcionalidad en App |
| :--- | :--- | :--- |
| `AuthController` | `POST /api/Auth/login`, `POST /api/Auth/register` | Autenticación y registro con selector de rol |
| `proyectoController` | `GET`, `POST`, `PUT`, `DELETE /api/Proyecto` | CRUD integral de proyectos de arquitectura |
| `invitacionController` | `POST /api/Invitacion/crear`, `POST /api/Invitacion/canjear` | Códigos de acceso y vinculación de clientes |
| `notificacionController` | `GET /api/Notificacion`, `PUT /api/Notificacion/{id}/leida` | Notificaciones de cambios de obra |
| `espacioFisicoController` | `GET /api/EspacioFisico/proyecto/{id}`, `POST /api/EspacioFisico` | Dimensionamiento de ambientes y volúmenes |
| `versionDiseñoController` | `GET /api/VersionDiseno/proyecto/{id}`, `POST`, `PUT /marcar-actual` | Gestión de iteraciones y planos de diseño |
| `elementoeEstructuralController` | `GET /api/ElementoEstructural/proyecto/{id}`, `POST` | Especificaciones de muros, columnas y vigas |
| `modeloImportadoController` | `GET /api/ModeloImportado/proyecto/{id}` | Catálogo de archivos 3D importados |
| `MedicionController` | `GET /api/Medicion/proyecto/{id}`, `POST`, `DELETE` | Persistencia y gestión de medidas tomadas |
| `usuarioController` | `GET /api/Usuario/{id}`, `PUT /api/Usuario/{id}` | Datos de contacto y perfil profesional |

---

## Requisitos del Entorno de Desarrollo

- **SDK:** [.NET 10.0 SDK](https://dotnet.microsoft.com/)
- **Cargas de trabajo:** `dotnet workload install maui-android`
- **IDE recomendados:** 
  - Visual Studio 2022 (v17.12+) con carga de trabajo *.NET Multi-platform App UI*.
  - Visual Studio Code con extensiones *.NET MAUI*, *C# Dev Kit* y *Android Tools*.
- **Dispositivo de prueba:**
  - Teléfono Android físico con Android 8.0+ (API 26+) y soporte de Google Play Services for AR (ARCore).
  - Depuración USB o WiFi activada.

---

## Compilación y Ejecución

### 1. Clonar el repositorio
```bash
git clone https://github.com/ArchiSpace-3D/ArchiSpace3D-Frontend.git
cd ArchiSpace3D-Frontend/archie-prueba1/MauiApp1
```

### 2. Restaurar dependencias
```bash
dotnet restore
```

### 3. Compilar para Android
```bash
dotnet build -f net10.0-android
```

### 4. Instalar en dispositivo conectado vía ADB
```bash
dotnet run -f net10.0-android
```
*O instalar directamente el APK generado:*
```bash
adb install -r bin/Debug/net10.0-android/com.companyname.mauiapp1-Signed.apk
```

---

## Estructura del Código

```text
ArchiSpace3D-Frontend/
├── .gitignore                      # Exclusiones de build, binarios y cachés
├── README.md                       # Documentación general
└── archie-prueba1/
    └── MauiApp1/
        ├── AppShell.xaml           # Sistema de navegación Apple Cupertino (4 tabs)
        ├── App.xaml.cs             # Ciclo de vida y arranque
        ├── DashboardPage.xaml/.cs  # Listado de proyectos, modales y notificaciones
        ├── DesignPage.xaml/.cs     # Espacios físicos, versiones y elementos estructurales
        ├── MainPage.xaml/.cs       # Medición AR, sensores y mediciones guardadas
        ├── LoginPage.xaml/.cs      # Login y modal de registro estilizado
        ├── ProfilePage.xaml/.cs    # Gestión del perfil de usuario y cierre de sesión
        ├── ARPage.xaml/.cs         # Cámara y realidad aumentada ARCore
        ├── Models/
        │   └── ApiModels.cs        # DTOs y modelos de datos tipados del backend
        ├── Services/
        │   ├── ApiService.cs       # Cliente HTTP unificado para el backend REST
        │   └── UserSession.cs      # Control en memoria de sesión, rol y token
        └── Resources/
            ├── Images/             # Iconos vectoriales SVG sin emojis
            └── Styles/
                └── Colors.xaml     # Paleta Inkwell, Lunar Eclipse, Creme Brulee, Au Lait
```

---

<div align="center">

**Desarrollado con dedicación para ArchiSpace 3D.**

</div>
