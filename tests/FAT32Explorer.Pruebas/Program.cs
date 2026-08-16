using FAT32Explorer.Modelo;
using FAT32Explorer.Persistencia;

NuevoDiscoYGeometria();
GeometriaInvalida();
ArchivosYDirectorios();
MovimientoYEliminacion();
CrecimientoFragmentacionYAtomicidad();
ArchivosVaciosYDiscoLleno();
Persistencia();
Console.WriteLine("Todas las pruebas del dominio FAT finalizaron correctamente.");

static void NuevoDiscoYGeometria()
{
    var d = Crear(8, 1);
    Igual(TablaFat.Reservado, d.Fat.Entradas[0], "FAT[0] especial"); Igual(TablaFat.Reservado, d.Fat.Entradas[1], "FAT[1] especial");
    Igual(2, d.Raiz.PrimerCluster, "raíz obtiene primer cluster de datos"); Igual(8L * 512, d.Configuracion.CapacidadBytes, "capacidad excluye FAT[0]/FAT[1]"); Igual(512, d.Configuracion.TamanoClusterBytes, "cluster = bytes por sector × sectores por cluster"); Igual(512L, d.EspacioUsado, "solo raíz consume espacio");
    var temporal = d.CrearDirectorio(d.Raiz, "Temporal"); int clusterTemporal = temporal.PrimerCluster!.Value; d.EliminarDirectorio(d.Raiz, temporal); Igual(TablaFat.Libre, d.Fat.Entradas[clusterTemporal], "eliminar carpeta vacía libera cluster");
    d.ValidarIntegridad();
}

static void GeometriaInvalida()
{
    Falla(() => DiscoVirtual.Crear(new ConfiguracionDisco { BytesPorSector = 700 }), "rechaza bytes por sector arbitrarios");
    Falla(() => DiscoVirtual.Crear(new ConfiguracionDisco { SectoresPorCluster = 3 }), "rechaza sectores por cluster no permitidos");
    Falla(() => DiscoVirtual.Crear(new ConfiguracionDisco { NumeroDeFat = 3 }), "rechaza número de FAT inválido");
}

static void ArchivosYDirectorios()
{
    var d = Crear(12, 1); var carpeta = d.CrearDirectorio(d.Raiz, "Documentos");
    Igual(3, carpeta.PrimerCluster, "crear carpeta consume First Fit"); var primero = carpeta.PrimerCluster; var cadena = d.Cadena(carpeta);
    d.RenombrarDirectorio(d.Raiz, carpeta, "Universidad"); Igual(primero, carpeta.PrimerCluster, "renombrar carpeta conserva primer cluster"); Igual(cadena, d.Cadena(carpeta), "renombrar conserva cadena");
    var archivo = d.CrearArchivo(carpeta, "tarea", new string('á', 300));
    Igual(600L, archivo.TamanoBytes, "tamaño UTF-8"); Igual(2, d.ClustersUtilizados(archivo), "archivo multicluster"); Igual(1024L, d.EspacioFisico(archivo), "espacio físico"); Igual(424L, d.DesperdicioInterno(archivo), "desperdicio");
    var cadenaArchivo = d.Cadena(archivo); d.RenombrarArchivo(carpeta, archivo, "final.txt"); Igual(cadenaArchivo, d.Cadena(archivo), "renombrar archivo conserva FAT");
    d.ValidarIntegridad();
}

static void MovimientoYEliminacion()
{
    var d = Crear(24, 1); var a = d.CrearDirectorio(d.Raiz, "A"); var hijo = d.CrearDirectorio(a, "Hijo"); var b = d.CrearDirectorio(d.Raiz, "B"); var f = d.CrearArchivo(hijo, "dato.txt", "contenido");
    string cadenaA = d.Cadena(a), cadenaHijo = d.Cadena(hijo), cadenaF = d.Cadena(f);
    d.MoverDirectorio(d.Raiz, b, a); Igual(cadenaA, d.Cadena(a), "mover carpeta conserva cadena"); Igual(cadenaHijo, d.Cadena(hijo), "mover conserva subdirectorio"); Igual(cadenaF, d.Cadena(f), "mover conserva archivo interno");
    Falla(() => d.MoverDirectorio(b, hijo, a), "no mover dentro de descendiente"); Falla(() => d.MoverDirectorio(b, a, d.Raiz), "no mover raíz");
    var duplicado = d.CrearDirectorio(d.Raiz, "A"); Falla(() => d.MoverDirectorio(b, d.Raiz, a), "duplicado en destino");
    var ocupados = d.Clusters.Count(c => c.Estado == EstadoCluster.Ocupado); d.EliminarDirectorio(b, a, true); Verdadero(d.Clusters.Count(c => c.Estado == EstadoCluster.Ocupado) < ocupados, "borrado recursivo libera clusters");
    d.EliminarDirectorio(d.Raiz, duplicado); d.ValidarIntegridad();
}

static void CrecimientoFragmentacionYAtomicidad()
{
    var d = Crear(8, 1);
    var bloqueador = d.CrearArchivo(d.Raiz, "bloque.txt", "x");
    var archivo = d.CrearArchivo(d.Raiz, "frag.txt", new string('x', 1024));
    Verdadero(!d.EstaFragmentado(archivo), "cadena consecutiva es contigua");
    d.EliminarArchivo(d.Raiz, bloqueador);
    d.ReemplazarContenido(archivo, new string('x', 1536));
    Verdadero(d.EstaFragmentado(archivo), "crecimiento First Fit detecta cadena fragmentada");
    d.ValidarIntegridad();
}

static void ArchivosVaciosYDiscoLleno()
{
    var d = Crear(4, 1);
    var carpeta = d.CrearDirectorio(d.Raiz, "Entradas");
    d.CrearArchivo(d.Raiz, "lleno.txt", new string('x', 1024));
    for (int i = 0; i < 15; i++)
    {
        var vacio = d.CrearArchivo(carpeta, $"v{i}.txt", "");
        Igual<int?>(null, vacio.PrimerCluster, "archivo vacío no usa cluster");
    }
    Igual(0, d.ClustersLibres, "disco lleno");
    Falla(() => d.CrearArchivo(carpeta, "crece.txt", ""), "entrada falla si directorio debe crecer sin cluster");
    Igual(15, carpeta.Archivos.Count, "creación fallida es atómica");

    var vacioEditable = carpeta.Archivos[0];
    Falla(() => d.ReemplazarContenido(vacioEditable, "x"), "escribir un byte falla con disco lleno");
    Igual(0L, vacioEditable.TamanoBytes, "fallo conserva archivo vacío");
    d.ValidarIntegridad();

    var disponible = Crear(3, 1);
    var archivoVacio = disponible.CrearArchivo(disponible.Raiz, "uno.txt", "");
    disponible.ReemplazarContenido(archivoVacio, "x");
    Verdadero(archivoVacio.PrimerCluster is >= 2, "un byte asigna primer cluster");
}

static void Persistencia()
{
    var d = Crear(10, 4); var carpeta = d.CrearDirectorio(d.Raiz, "Datos"); var archivo = d.CrearArchivo(carpeta, "a.txt", "abc");
    string ruta = Path.Combine(Path.GetTempPath(), $"fat32-{Guid.NewGuid():N}.json");
    try
    {
        var json = new AlmacenamientoJson(ruta); json.Guardar(d); var cargado = json.Cargar()!; cargado.ValidarIntegridad(); var dc = cargado.Raiz.Directorios.Single();
        Igual(d.Cadena(carpeta), cargado.Cadena(dc), "persistencia conserva directorio"); Igual(d.Cadena(archivo), cargado.Cadena(dc.Archivos.Single()), "persistencia conserva archivo"); Igual(TipoPropietario.Directorio, cargado.Clusters[dc.PrimerCluster!.Value].TipoPropietario, "persistencia conserva propietario"); Igual(512, cargado.Configuracion.BytesPorSector, "persistencia conserva bytes por sector"); Igual(4, cargado.Configuracion.SectoresPorCluster, "persistencia conserva sectores por cluster"); Igual(2, cargado.Configuracion.NumeroDeFat, "persistencia conserva número de FAT");
        File.WriteAllText(ruta, "{}"); Falla(() => json.Cargar(), "rechaza modelo antiguo sin versión");
    }
    finally { File.Delete(ruta); }
}

static DiscoVirtual Crear(int datos, int sectores) => DiscoVirtual.Crear(new ConfiguracionDisco { CantidadClusters = datos, BytesPorSector = 512, SectoresPorCluster = sectores, NumeroDeFat = 2 });
static void Igual<T>(T esperado, T actual, string caso) { if (!EqualityComparer<T>.Default.Equals(esperado, actual)) throw new Exception($"{caso}: esperado {esperado}, actual {actual}"); }
static void Falla(Action accion, string caso) { try { accion(); } catch { return; } throw new Exception($"{caso}: se esperaba una excepción"); }
static void Verdadero(bool valor, string caso) { if (!valor) throw new Exception($"{caso}: se esperaba verdadero"); }
