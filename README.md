# FAT32Explorer

Simulador didáctico de un disco FAT/FAT32 construido con **.NET 8 y Windows Forms**. Toda la información vive en objetos y en un archivo JSON del perfil de la aplicación; nunca se escriben sectores, particiones ni archivos que representen el contenido virtual.

## Ejecutar

Requiere Windows y el SDK de .NET 8:

```shell
dotnet run --project src/FAT32Explorer
```

Las pruebas autocontenidas del dominio no requieren paquetes de terceros:

```shell
dotnet run --project tests/FAT32Explorer.Pruebas
```

## Diseño

- `DiscoVirtual` ofrece las operaciones de carpetas y archivos y mantiene la UI separada del algoritmo.
- `TablaFat` es la fuente de verdad para enlazar, recorrer y liberar cadenas. `ArchivoVirtual` conserva solamente su primer cluster.
- Cada guardado calcula los bytes UTF-8. Antes de modificar una cadena se comprueba que estén disponibles todos los clusters adicionales, por lo que un fallo conserva el archivo anterior.
- La asignación usa First Fit. Mover un archivo cambia únicamente el directorio que lo contiene.
- `ExploradorForm` muestra árbol, contenido, tabla FAT, mapa de clusters y capacidad; `EditorTextoForm` funciona como un bloc de notas básico. El menú Configuración permite definir tanto el disco como el nombre y usuario del sistema operativo virtual.
- El estado se guarda atómicamente como JSON en `Application.UserAppDataPath` y se valida al cargar.

Los clusters reservados se contabilizan como espacio usado, pues no están disponibles para archivos. Un archivo vacío no necesita cluster y muestra `(vacío)` como cadena.
