using FAT32Explorer.Modelo;
using FAT32Explorer.Persistencia;

NuevoDiscoYGeometria();
ArchivosYDirectorios();
MovimientoYEliminacion();
CrecimientoFragmentacionYAtomicidad();
Persistencia();
Console.WriteLine("Todas las pruebas del dominio FAT finalizaron correctamente.");

static void NuevoDiscoYGeometria()
{
    var d = Crear(8, 64);
    Igual(TablaFat.Reservado, d.Fat.Entradas[0], "FAT[0] especial"); Igual(TablaFat.Reservado, d.Fat.Entradas[1], "FAT[1] especial");
    Igual(2, d.Raiz.PrimerCluster, "raíz obtiene primer cluster de datos"); Igual(8L * 64, d.Configuracion.CapacidadBytes, "capacidad excluye FAT[0]/FAT[1]"); Igual(64L, d.EspacioUsado, "solo raíz consume espacio");
    var temporal = d.CrearDirectorio(d.Raiz, "Temporal"); int clusterTemporal = temporal.PrimerCluster!.Value; d.EliminarDirectorio(d.Raiz, temporal); Igual(TablaFat.Libre, d.Fat.Entradas[clusterTemporal], "eliminar carpeta vacía libera cluster");
    d.ValidarIntegridad();
}

static void ArchivosYDirectorios()
{
    var d = Crear(12, 64); var carpeta = d.CrearDirectorio(d.Raiz, "Documentos");
    Igual(3, carpeta.PrimerCluster, "crear carpeta consume First Fit"); var primero = carpeta.PrimerCluster; var cadena = d.Cadena(carpeta);
    d.RenombrarDirectorio(d.Raiz, carpeta, "Universidad"); Igual(primero, carpeta.PrimerCluster, "renombrar carpeta conserva primer cluster"); Igual(cadena, d.Cadena(carpeta), "renombrar conserva cadena");
    var archivo = d.CrearArchivo(carpeta, "tarea", new string('á', 40));
    Igual(80L, archivo.TamanoBytes, "tamaño UTF-8"); Igual(2, d.ClustersUtilizados(archivo), "archivo multicluster"); Igual(128L, d.EspacioFisico(archivo), "espacio físico"); Igual(48L, d.DesperdicioInterno(archivo), "desperdicio");
    var cadenaArchivo = d.Cadena(archivo); d.RenombrarArchivo(carpeta, archivo, "final.txt"); Igual(cadenaArchivo, d.Cadena(archivo), "renombrar archivo conserva FAT");
    d.ValidarIntegridad();
}

static void MovimientoYEliminacion()
{
    var d = Crear(24, 64); var a = d.CrearDirectorio(d.Raiz, "A"); var hijo = d.CrearDirectorio(a, "Hijo"); var b = d.CrearDirectorio(d.Raiz, "B"); var f = d.CrearArchivo(hijo, "dato.txt", "contenido");
    string cadenaA = d.Cadena(a), cadenaHijo = d.Cadena(hijo), cadenaF = d.Cadena(f);
    d.MoverDirectorio(d.Raiz, b, a); Igual(cadenaA, d.Cadena(a), "mover carpeta conserva cadena"); Igual(cadenaHijo, d.Cadena(hijo), "mover conserva subdirectorio"); Igual(cadenaF, d.Cadena(f), "mover conserva archivo interno");
    Falla(() => d.MoverDirectorio(b, hijo, a), "no mover dentro de descendiente"); Falla(() => d.MoverDirectorio(b, a, d.Raiz), "no mover raíz");
    var duplicado = d.CrearDirectorio(d.Raiz, "A"); Falla(() => d.MoverDirectorio(b, d.Raiz, a), "duplicado en destino");
    var ocupados = d.Clusters.Count(c => c.Estado == EstadoCluster.Ocupado); d.EliminarDirectorio(b, a, true); Verdadero(d.Clusters.Count(c => c.Estado == EstadoCluster.Ocupado) < ocupados, "borrado recursivo libera clusters");
    d.EliminarDirectorio(d.Raiz, duplicado); d.ValidarIntegridad();
}

static void CrecimientoFragmentacionYAtomicidad()
{
    var d = Crear(12, 32); var bloqueador = d.CrearArchivo(d.Raiz, "bloque.txt", "x"); var carpeta = d.CrearDirectorio(d.Raiz, "Frag");
    d.EliminarArchivo(d.Raiz, bloqueador); d.CrearArchivo(carpeta, "vacio.txt", "");
    Igual("5 -> 3 -> EOC", d.Cadena(carpeta), "directorio fragmentado con First Fit");
    d.EliminarArchivo(carpeta, carpeta.Archivos.Single()); Igual(1, d.ClustersUtilizadosDirectorio(carpeta), "directorio disminuye y libera cluster"); d.ValidarIntegridad();

    var lleno = Crear(2, 64); var unica = lleno.CrearDirectorio(lleno.Raiz, "Unica"); lleno.CrearArchivo(unica, "uno.txt", ""); string antes = lleno.Cadena(unica);
    Falla(() => lleno.CrearArchivo(unica, "dos.txt", ""), "falta de espacio al crecer directorio"); Igual(1, unica.Archivos.Count, "fallo atómico no agrega entrada"); Igual(antes, lleno.Cadena(unica), "fallo conserva cadena"); lleno.ValidarIntegridad();
}

static void Persistencia()
{
    var d = Crear(10, 64); var carpeta = d.CrearDirectorio(d.Raiz, "Datos"); var archivo = d.CrearArchivo(carpeta, "a.txt", "abc");
    string ruta = Path.Combine(Path.GetTempPath(), $"fat32-{Guid.NewGuid():N}.json");
    try
    {
        var json = new AlmacenamientoJson(ruta); json.Guardar(d); var cargado = json.Cargar()!; cargado.ValidarIntegridad(); var dc = cargado.Raiz.Directorios.Single();
        Igual(d.Cadena(carpeta), cargado.Cadena(dc), "persistencia conserva directorio"); Igual(d.Cadena(archivo), cargado.Cadena(dc.Archivos.Single()), "persistencia conserva archivo"); Igual(TipoPropietario.Directorio, cargado.Clusters[dc.PrimerCluster!.Value].TipoPropietario, "persistencia conserva propietario");
        File.WriteAllText(ruta, "{}"); Falla(() => json.Cargar(), "rechaza modelo antiguo sin versión");
    }
    finally { File.Delete(ruta); }
}

static DiscoVirtual Crear(int datos, int cluster) => DiscoVirtual.Crear(new ConfiguracionDisco { CantidadClusters = datos, TamanoClusterBytes = cluster });
static void Igual<T>(T esperado, T actual, string caso) { if (!EqualityComparer<T>.Default.Equals(esperado, actual)) throw new Exception($"{caso}: esperado {esperado}, actual {actual}"); }
static void Falla(Action accion, string caso) { try { accion(); } catch { return; } throw new Exception($"{caso}: se esperaba una excepción"); }
static void Verdadero(bool valor, string caso) { if (!valor) throw new Exception($"{caso}: se esperaba verdadero"); }
