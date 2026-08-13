using FAT32Explorer.Modelo;

var disco = DiscoVirtual.Crear(new ConfiguracionDisco { CantidadClusters = 8, ClustersReservados = 2, TamanoClusterBytes = 4 });
var documentos = disco.CrearDirectorio(disco.Raiz, "Documentos");
var archivo = disco.CrearArchivo(disco.Raiz, "tarea", "12345");
Igual(2, disco.Fat.Recorrer(archivo.PrimerCluster).Count, "archivo multicluster");
Igual("12345", archivo.Contenido, "lectura de contenido");

disco.ReemplazarContenido(archivo, "123456789");
Igual(3, disco.Fat.Recorrer(archivo.PrimerCluster).Count, "crecimiento");
disco.ReemplazarContenido(archivo, "1");
Igual(1, disco.Fat.Recorrer(archivo.PrimerCluster).Count, "reducción");

var cadenaAntes = disco.Cadena(archivo);
disco.MoverArchivo(disco.Raiz, documentos, archivo);
Igual(cadenaAntes, disco.Cadena(archivo), "mover conserva FAT");
Falla(() => disco.CrearArchivo(documentos, "tarea.txt", "x"), "duplicados");

int liberado = archivo.PrimerCluster!.Value;
disco.EliminarArchivo(documentos, archivo);
var reutilizado = disco.CrearArchivo(documentos, "otro.txt", "xx");
Igual(liberado, reutilizado.PrimerCluster, "First Fit reutiliza clusters");

var lleno = disco.CrearArchivo(disco.Raiz, "grande.txt", new string('x', 20));
var cadenaOriginal = disco.Cadena(lleno); var contenidoOriginal = lleno.Contenido;
Falla(() => disco.ReemplazarContenido(lleno, new string('x', 40)), "sin espacio");
Igual(cadenaOriginal, disco.Cadena(lleno), "fallo no corrompe FAT");
Igual(contenidoOriginal, lleno.Contenido, "fallo conserva contenido");
Igual(disco.Configuracion.CapacidadBytes, disco.EspacioUsado + disco.EspacioLibre, "cálculo de espacio");
disco.ValidarIntegridad();
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
