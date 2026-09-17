# PGM611 - Scripting - Llusco Galilea

Proyecto de practica en Unity 2D con Universal Render Pipeline (URP).

## Requisitos

- Git y Unity Hub.
- Unity Editor **6000.3.16f1**, version indicada en `ProjectSettings/ProjectVersion.txt`.
- Conexion a Internet para que Unity descargue los paquetes al abrirlo por primera vez.
- Para generar un ejecutable, instalar desde Unity Hub el modulo de compilacion de la plataforma de destino.

## Clonar y probar

```sh
git clone https://github.com/Galileya/PGM611_scripting_LluscoGalilea.git
```

1. En Unity Hub, seleccionar **Add / Agregar proyecto desde disco** y elegir la carpeta clonada (la que contiene `Assets`, `Packages` y `ProjectSettings`).
2. Abrir con Unity **6000.3.16f1** y esperar la importacion de recursos y paquetes.
3. Abrir `Assets/clase1.unity` desde la ventana Project.
4. Pulsar **Play**. La escena contiene el piso/tilemap, la camara y la iluminacion 2D.

Los scripts de `Assets/scrips` son ejercicios iniciales de clases y namespaces. Los metodos de jugador estan vacios: todavia no hay movimiento ni controles de juego implementados.

## Crear un ejecutable

En **File > Build Profiles**, seleccionar la plataforma instalada y agregar `Assets/clase1.unity` a la lista de escenas. Dejarla habilitada como primera escena y compilar. La configuracion original conserva `SampleScene` como escena de compilacion predeterminada.

## Archivos incluidos

- `Assets/`: escenas, scripts, recursos y archivos `.meta` que conservan las referencias.
- `Packages/`: manifiesto y versiones resueltas de dependencias.
- `ProjectSettings/`: configuracion del proyecto y version del editor.

`Library`, `Temp`, `Logs`, `UserSettings` y archivos de IDE se generan localmente y no se versionan. No eliminar ni regenerar manualmente los `.meta` incluidos.

## Verificacion

Se revisaron la estructura del proyecto, las dependencias y la inclusion de recursos y metadatos. No se ejecuto una prueba Play ni una compilacion del proyecto en Unity durante la preparacion del repositorio.
