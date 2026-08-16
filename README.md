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

`CantidadClusters` representa exclusivamente los clusters del área de datos. Las entradas FAT 0 y 1 son especiales, aparecen en la tabla pero no consumen capacidad. Un archivo vacío no necesita cluster; un directorio vacío sí conserva almacenamiento para su entrada propia.

## Modelo FAT32 simulado

- **Geometría:** `FAT[0]` y `FAT[1]` son entradas especiales `RESERVED`, no clusters físicos ni sectores reservados. La capacidad es `clusters de datos × tamaño de cluster`; los datos comienzan en el cluster 2.
- **Directorio raíz:** al formatear, `C:\` recibe mediante First Fit su propia cadena FAT (normalmente `2 -> EOC`).
- **Directorios físicos:** cada directorio guarda solo `PrimerCluster` y reconstruye su cadena desde la FAT, igual que un archivo. Una carpeta vacía ocupa como mínimo un cluster.
- **Entradas simplificadas:** se modelan 32 bytes por la entrada propia del directorio y por cada elemento directo. Al crecer o reducirse el número de elementos, la cadena del directorio crece o se contrae transaccionalmente.
- **FAT como fuente de verdad:** la entrada lógica del archivo conserva nombre, tamaño, fechas y `PrimerCluster`; desde este se siguen los enlaces hasta `EOC` (End Of Chain). No se guarda una segunda lista de clusters.
- **First Fit y fragmentación:** se toman los primeros clusters libres, aunque no sean contiguos. Eliminar libera la cadena y permite reutilizar sus huecos, por lo que cadenas como `2 -> 3 -> 6 -> 7 -> EOC` son visibles y esperables.
- **Espacio:** el tamaño lógico son los bytes UTF-8 reales. El espacio físico es la cantidad de clusters por el tamaño de cluster; su diferencia es el desperdicio interno. La barra inferior separa capacidad, usado y libre.
- **Operaciones lógicas:** renombrar y mover archivos o árboles de carpetas cambia relaciones lógicas, no sus cadenas. Se impiden ciclos y duplicados. El borrado recursivo explícito libera archivos, subdirectorios y el directorio seleccionado.
- **Persistencia:** la geometría, FAT, clusters, jerarquía, contenidos y metadatos se guardan en JSON. Cadenas y métricas derivadas se reconstruyen al cargar y se valida su integridad.

## Diferencias respecto a FAT32 real

> No estamos implementando un volumen FAT32 binario compatible con Windows; estamos emulando didácticamente sus mecanismos esenciales de administración de archivos.

- Los directorios sí consumen clusters, pero sus entradas de 32 bytes son una medida académica: no se codifican campos binarios, `.`/`..`, nombres 8.3 ni entradas LFN adicionales.
- No se implementan sectores, MBR/GPT, BPB, FSInfo, dos copias físicas de FAT ni acceso a discos reales.
- El contenido completo se conserva como texto JSON; los clusters representan su asignación y capacidad, no contienen bloques binarios separados.
- Solo se admiten archivos TXT y no hay permisos ni journaling.

## Compatibilidad del estado guardado

El formato actual usa `VersionModelo = 3`. Como esta versión asigna clusters físicos a directorios y cambia la geometría, un JSON de fases anteriores se rechaza con un mensaje explícito y debe recrearse; no se intenta una migración silenciosa que pudiera superponer cadenas FAT.
