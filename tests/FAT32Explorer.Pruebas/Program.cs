using FAT32Explorer.Modelo;
using FAT32Explorer.Persistencia;

var disco = DiscoVirtual.Crear(new ConfiguracionDisco { CantidadClusters = 8, ClustersReservados = 2, TamanoClusterBytes = 4 });
var documentos = disco.CrearDirectorio(disco.Raiz, "Documentos");
var archivo = disco.CrearArchivo(disco.Raiz, "tarea", "12345");
Igual(2, disco.Fat.Recorrer(archivo.PrimerCluster).Count, "archivo multicluster");
Igual(2, archivo.PrimerCluster, "First Fit comienza en el primer cluster de datos");
Verdadero(archivo.PrimerCluster is >= 2, "0 y 1 nunca se asignan");
Igual("12345", archivo.Contenido, "lectura de contenido");
Igual(5L, archivo.TamanoBytes, "tamaño lógico UTF-8"); Igual(8L, disco.EspacioFisico(archivo), "espacio físico"); Igual(3L, disco.DesperdicioInterno(archivo), "desperdicio interno");

disco.ReemplazarContenido(archivo, "123456789");
Igual(3, disco.Fat.Recorrer(archivo.PrimerCluster).Count, "crecimiento");
disco.ReemplazarContenido(archivo, "1");
Igual(1, disco.Fat.Recorrer(archivo.PrimerCluster).Count, "reducción");

var cadenaAntes = disco.Cadena(archivo);
disco.MoverArchivo(disco.Raiz, documentos, archivo);
Igual(cadenaAntes, disco.Cadena(archivo), "mover conserva FAT");
Falla(() => disco.CrearArchivo(documentos, "tarea.txt", "x"), "duplicados");
int primeroAntes = archivo.PrimerCluster!.Value; cadenaAntes = disco.Cadena(archivo);
disco.RenombrarArchivo(documentos, archivo, "tarea-final");
Igual(primeroAntes, archivo.PrimerCluster, "renombrar conserva PrimerCluster"); Igual(cadenaAntes, disco.Cadena(archivo), "renombrar conserva cadena FAT");
var conflicto = disco.CrearArchivo(documentos, "conflicto.txt", ""); Falla(() => disco.RenombrarArchivo(documentos, archivo, conflicto.Nombre), "duplicado al renombrar");

int liberado = archivo.PrimerCluster!.Value;
disco.EliminarArchivo(documentos, archivo);
var reutilizado = disco.CrearArchivo(documentos, "otro.txt", "xx");
Igual(liberado, reutilizado.PrimerCluster, "First Fit reutiliza clusters");

var carpetaA = disco.CrearDirectorio(disco.Raiz, "A"); var carpetaB = disco.CrearDirectorio(disco.Raiz, "B");
disco.RenombrarDirectorio(disco.Raiz, carpetaA, "Renombrada"); Igual("Renombrada", carpetaA.Nombre, "renombrar carpeta");
Falla(() => disco.RenombrarDirectorio(disco.Raiz, carpetaA, carpetaB.Nombre), "duplicado de carpeta");
disco.CrearArchivo(carpetaA, "contenido.txt", ""); Falla(() => disco.EliminarDirectorio(disco.Raiz, carpetaA), "no eliminar carpeta no vacía");

var lleno = disco.CrearArchivo(disco.Raiz, "grande.txt", new string('x', 20));
var cadenaOriginal = disco.Cadena(lleno); var contenidoOriginal = lleno.Contenido;
Falla(() => disco.ReemplazarContenido(lleno, new string('x', 40)), "sin espacio");
Igual(cadenaOriginal, disco.Cadena(lleno), "fallo no corrompe FAT");
Igual(contenidoOriginal, lleno.Contenido, "fallo conserva contenido");
Igual(disco.Configuracion.CapacidadBytes, disco.EspacioUsado + disco.EspacioLibre, "cálculo de espacio");
disco.ValidarIntegridad();

var fragmentado = DiscoVirtual.Crear(new ConfiguracionDisco { CantidadClusters = 10, ClustersReservados = 2, TamanoClusterBytes = 1 });
var a = fragmentado.CrearArchivo(fragmentado.Raiz, "a.txt", "aa"); fragmentado.CrearArchivo(fragmentado.Raiz, "b.txt", "bb"); fragmentado.EliminarArchivo(fragmentado.Raiz, a);
var c = fragmentado.CrearArchivo(fragmentado.Raiz, "c.txt", "cccc"); Igual("2 -> 3 -> 6 -> 7 -> EOC", fragmentado.Cadena(c), "fragmentación First Fit no contigua");

string ruta = Path.Combine(Path.GetTempPath(), $"fat32-{Guid.NewGuid():N}.json");
try { var json = new AlmacenamientoJson(ruta); json.Guardar(fragmentado); var cargado = json.Cargar()!; cargado.ValidarIntegridad(); Igual(fragmentado.Cadena(c), cargado.Cadena(cargado.Raiz.Archivos.Single(x => x.Nombre == "c.txt")), "persistencia conserva FAT"); Igual(c.Contenido, cargado.Raiz.Archivos.Single(x => x.Nombre == "c.txt").Contenido, "persistencia conserva contenido"); } finally { File.Delete(ruta); }
Console.WriteLine("Todas las pruebas del dominio FAT finalizaron correctamente.");

static void Igual<T>(T esperado, T actual, string caso)
{
    if (!EqualityComparer<T>.Default.Equals(esperado, actual)) throw new Exception($"{caso}: se esperaba {esperado}, se obtuvo {actual}");
}
static void Falla(Action accion, string caso)
{
    try { accion(); } catch { return; }
    throw new Exception($"{caso}: se esperaba una excepción");
}
static void Verdadero(bool valor, string caso) { if (!valor) throw new Exception($"{caso}: se esperaba verdadero"); }
