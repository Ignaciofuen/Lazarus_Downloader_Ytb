# Manual de Usuario - Lazarus Downloader YouTube (v1.1)

Bienvenido a **Lazarus Downloader**, la herramienta definitiva para descargar videos y música de YouTube en la máxima calidad, diseñada con una estética Cyberpunk moderna y construida para saltarse todas las restricciones recientes.

Este documento te guiará paso a paso para que puedas aprovechar al máximo todas las funciones de la aplicación.

---

## 1. Conceptos Básicos

La interfaz de Lazarus está dividida en tres áreas principales:
* **Encabezado (Arriba):** Muestra el logo y el botón de actualización.
* **Cuerpo Principal (Centro):** Contiene las pestañas de **AUDIO** y **VIDEO**. Aquí es donde pegas el enlace y descargas.
* **Barra de Ajustes (Abajo):** Desde aquí puedes cambiar la carpeta de destino, seleccionar tu navegador web y cargar archivos de cookies. ¡Todo tu progreso se guarda automáticamente para la próxima vez que abras la aplicación!

---

## 2. Primera Configuración (Importante)

YouTube actualmente bloquea las descargas realizadas por "bots". Para demostrarle a YouTube que eres un humano genuino que simplemente quiere descargar el video, Lazarus usa **las cookies de tu navegador web**. 

Tienes **dos métodos** para inyectar tus cookies:

### Método A: Extracción Automática (Recomendado para Firefox/Brave)
1. En la parte inferior, busca el menú desplegable que dice **NAVEGADOR**.
2. Selecciona el nombre del navegador que utilizas habitualmente (ej. `firefox`).
3. ¡Listo! Lazarus extraerá las cookies automáticamente en segundo plano.

### Método B: Archivo Manual (Obligatorio para Chrome/Edge)
Debido a las fuertes medidas de seguridad (encriptación DPAPI) introducidas en las últimas versiones de Chrome y Edge, el método automático puede fallar. Si es así, sigue estos pasos:
1. En la parte inferior, haz clic en el botón morado **Extensión Chrome 🌐**.
2. Instala la extensión oficial ("Get cookies.txt LOCALLY") en tu navegador.
3. Entra a YouTube.com y haz clic en el icono de la extensión para exportar un archivo `.txt`.
4. En Lazarus, haz clic en **Cargar Archivo...** y selecciona el archivo `.txt` que descargaste.
5. Verás el mensaje **TXT COOKIES: Capturadas ❌** en verde neón indicando que todo está listo. (Lazarus recordará este archivo para tus futuras sesiones, no tienes que hacerlo cada vez).

---

## 3. ¿Cómo Descargar Música? (Audio)

1. Ve a la pestaña **AUDIO**.
2. Pega el enlace de YouTube en la caja de texto.
3. Selecciona tu formato favorito (`mp3`, `flac` o `wav`). 
   * *Nota: La aplicación incrustará automáticamente la carátula oficial del video dentro del archivo de música (excepto en `wav`, donde la descargará como imagen separada).*
4. Pulsa **Descargar**.
5. Verás la barra de neón deslizarse suavemente. Al finalizar, aparecerá el botón **📁 Abrir archivo descargado**.

---

## 4. ¿Cómo Descargar Videos en Máxima Calidad? (Video)

1. Ve a la pestaña **VIDEO**.
2. Pega el enlace de YouTube.
3. Haz clic en **CONSULTAR RESOLUCIONES**.
4. Lazarus se comunicará con los servidores de YouTube y te mostrará una lista con las calidades reales disponibles (desde `144p` hasta `4K` o superior, dependiendo del video).
5. Haz clic en el botón de la calidad que prefieras. 
6. Lazarus descargará el video y el audio de más alta fidelidad por separado y los unirá mágicamente usando tecnología de cine (`FFmpeg`).

---

## 5. Mantenimiento y Actualizaciones

El código de YouTube cambia casi todas las semanas para intentar bloquear las descargas. Si un día Lazarus deja de descargar y te arroja errores de extracción:
* Solo haz clic en el botón **↻ Actualizar Motor** en la parte superior derecha. 
* Esto descargará automáticamente la última versión del algoritmo de extracción directamente desde los servidores de desarrollo, reparando la aplicación en cuestión de segundos.

---

## 6. Solución de Problemas Frecuentes

* **La barra de progreso pasa muy rápido o se queda trabada en 100% al descargar videos:**
  Esto es totalmente normal. Al descargar videos de alta resolución (ej. 1080p o 4K), el programa descarga primero el video, luego descarga el audio y, al final (cuando la barra llega a 100%), los une. El proceso de unión puede tardar unos segundos dependiendo de la potencia de tu PC. Ten paciencia y espera a que aparezca el botón de "Abrir carpeta".
* **El botón "Cargar Archivo..." me desapareció:**
  Esto sucede porque ya has cargado unas cookies y el sistema las tiene activas. Haz clic en la **❌ roja** al lado de "Capturadas" para borrar la memoria y el botón de cargar archivo volverá a aparecer.

*¡Disfruta tus descargas!*
