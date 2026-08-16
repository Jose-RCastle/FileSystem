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

## Qué elementos de FAT32 se simulan

- **Clusters y área de datos:** las entradas 0 y 1 están reservadas y los archivos solo pueden usar clusters desde el 2. El mapa distingue `RESERVED`, `FREE`, ocupado y seleccionado.
- **FAT como fuente de verdad:** la entrada lógica del archivo conserva nombre, tamaño, fechas y `PrimerCluster`; desde este se siguen los enlaces hasta `EOC` (End Of Chain). No se guarda una segunda lista de clusters.
- **First Fit y fragmentación:** se toman los primeros clusters libres, aunque no sean contiguos. Eliminar libera la cadena y permite reutilizar sus huecos, por lo que cadenas como `2 -> 3 -> 6 -> 7 -> EOC` son visibles y esperables.
- **Espacio:** el tamaño lógico son los bytes UTF-8 reales. El espacio físico es la cantidad de clusters por el tamaño de cluster; su diferencia es el desperdicio interno. La barra inferior separa capacidad, usado y libre.
- **Operaciones lógicas:** crear, editar, renombrar o mover conserva la separación entre la entrada del directorio y los datos físicos. Un cambio sin espacio o con nombre duplicado no modifica el archivo anterior.
- **Persistencia:** la geometría, FAT, clusters, jerarquía, contenidos y metadatos se guardan en JSON. Cadenas y métricas derivadas se reconstruyen al cargar y se valida su integridad.

## Simplificaciones respecto a FAT32 real

> No estamos implementando un volumen FAT32 binario compatible con Windows; estamos emulando didácticamente sus mecanismos esenciales de administración de archivos.

- Los directorios son colecciones lógicas jerárquicas y **no consumen clusters**. En FAT32 real, incluido el raíz, son archivos especiales almacenados en el área de datos. Mantenerlos lógicos evita una migración riesgosa del formato JSON y conserva un motor pequeño para exposición.
- No se implementan entradas binarias de 32 bytes, nombres 8.3/LFN, sectores, MBR, BPB, FSInfo, dos copias físicas de FAT ni acceso a discos reales.
- El contenido completo se conserva como texto JSON; los clusters representan su asignación y capacidad, no contienen bloques binarios separados.
- Solo se admiten archivos TXT, no hay permisos, journaling ni eliminación recursiva, y mover carpetas queda fuera de esta fase.
