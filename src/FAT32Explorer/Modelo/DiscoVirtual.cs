using System.Text;

namespace FAT32Explorer.Modelo;

public sealed class DiscoVirtual
{
    public ConfiguracionDisco Configuracion { get; set; } = new();
    public ConfiguracionSistema SistemaOperativo { get; set; } = new();
    public TablaFat Fat { get; set; } = new();
    public List<Cluster> Clusters { get; set; } = [];
    public DirectorioVirtual Raiz { get; set; } = new() { Nombre = "C:\\" };

    public long EspacioUsado => Clusters.Count(c => c.Estado != EstadoCluster.Libre) * (long)Configuracion.TamanoClusterBytes;
    public long EspacioLibre => Configuracion.CapacidadBytes - EspacioUsado;

    public static DiscoVirtual CrearPredeterminado() => Crear(new ConfiguracionDisco());

    public static DiscoVirtual Crear(ConfiguracionDisco configuracion)
    {
        var fat = TablaFat.Crear(configuracion.CantidadClusters, configuracion.ClustersReservados);
        return new DiscoVirtual
        {
            Configuracion = configuracion,
            Fat = fat,
            Clusters = Enumerable.Range(0, configuracion.CantidadClusters).Select(i => new Cluster
            { Numero = i, Estado = i < configuracion.ClustersReservados ? EstadoCluster.Reservado : EstadoCluster.Libre }).ToList()
        };
    }

    public DirectorioVirtual CrearDirectorio(DirectorioVirtual padre, string nombre)
    {
        nombre = ValidarNombre(nombre, false);
        VerificarDuplicado(padre, nombre);
        var nuevo = new DirectorioVirtual { Nombre = nombre };
        padre.Directorios.Add(nuevo);
        return nuevo;
    }

    public void RenombrarArchivo(DirectorioVirtual padre, ArchivoVirtual archivo, string nuevoNombre)
    {
        if (!padre.Archivos.Contains(archivo)) throw new InvalidOperationException("El archivo no pertenece al directorio indicado.");
        nuevoNombre = ValidarNombre(nuevoNombre, true);
        VerificarDuplicado(padre, nuevoNombre, archivo);
        archivo.Nombre = nuevoNombre;
        archivo.Modificado = DateTime.Now;
    }

    public void RenombrarDirectorio(DirectorioVirtual padre, DirectorioVirtual directorio, string nuevoNombre)
    {
        if (ReferenceEquals(directorio, Raiz)) throw new InvalidOperationException("No se puede renombrar el directorio raíz C:\\.");
        if (!padre.Directorios.Contains(directorio)) throw new InvalidOperationException("La carpeta no pertenece al directorio indicado.");
        nuevoNombre = ValidarNombre(nuevoNombre, false);
        VerificarDuplicado(padre, nuevoNombre, directorio);
        directorio.Nombre = nuevoNombre;
    }

    public void EditarArchivo(DirectorioVirtual padre, ArchivoVirtual archivo, string nuevoNombre, string contenido)
    {
        // Validar el nombre antes de tocar la FAT evita cambios parciales si cambian nombre y contenido.
        nuevoNombre = ValidarNombre(nuevoNombre, true);
        VerificarDuplicado(padre, nuevoNombre, archivo);
        ReemplazarContenido(archivo, contenido);
        archivo.Nombre = nuevoNombre;
    }

    public ArchivoVirtual CrearArchivo(DirectorioVirtual padre, string nombre, string contenido)
    {
        nombre = ValidarNombre(nombre, true);
        VerificarDuplicado(padre, nombre);
        var archivo = new ArchivoVirtual { Nombre = nombre };
        ReemplazarContenido(archivo, contenido);
        padre.Archivos.Add(archivo);
        return archivo;
    }

    public void ReemplazarContenido(ArchivoVirtual archivo, string contenido)
    {
        contenido ??= "";
        long bytes = Encoding.UTF8.GetByteCount(contenido);
        int necesarios = bytes == 0 ? 0 : checked((int)Math.Ceiling(bytes / (double)Configuracion.TamanoClusterBytes));
        var anterior = Fat.Recorrer(archivo.PrimerCluster).ToList();
        int adicionales = Math.Max(0, necesarios - anterior.Count);
        var libres = Fat.BuscarLibres(adicionales);
        if (libres.Count != adicionales) throw new IOException("No hay espacio suficiente. El archivo original no fue modificado.");

        // La comprobación anterior hace transaccional la operación: solo ahora se modifica la FAT.
        var nueva = anterior.Take(necesarios).Concat(libres).ToList();
        foreach (int sobrante in anterior.Skip(necesarios)) { Fat.Entradas[sobrante] = TablaFat.Libre; LiberarCluster(sobrante); }
        if (nueva.Count > 0) Fat.Enlazar(nueva);
        foreach (int numero in nueva) { Clusters[numero].Estado = EstadoCluster.Ocupado; Clusters[numero].ArchivoId = archivo.Id; }
        archivo.PrimerCluster = nueva.Count == 0 ? null : nueva[0];
        archivo.Contenido = contenido;
        archivo.TamanoBytes = bytes;
        archivo.Modificado = DateTime.Now;
    }

    public void EliminarArchivo(DirectorioVirtual padre, ArchivoVirtual archivo)
    {
        foreach (int numero in Fat.Recorrer(archivo.PrimerCluster)) LiberarCluster(numero);
        Fat.Liberar(archivo.PrimerCluster);
        padre.Archivos.Remove(archivo);
    }

    public void MoverArchivo(DirectorioVirtual origen, DirectorioVirtual destino, ArchivoVirtual archivo)
    {
        if (ReferenceEquals(origen, destino)) return;
        VerificarDuplicado(destino, archivo.Nombre);
        if (!origen.Archivos.Remove(archivo)) throw new InvalidOperationException("El archivo no pertenece al directorio de origen.");
        destino.Archivos.Add(archivo); // La FAT y PrimerCluster permanecen intactos.
        archivo.Modificado = DateTime.Now;
    }

    public void EliminarDirectorio(DirectorioVirtual padre, DirectorioVirtual directorio)
    {
        if (directorio.Archivos.Count > 0 || directorio.Directorios.Count > 0)
            throw new InvalidOperationException($"No se puede eliminar \"{directorio.Nombre}\".\n\nLa carpeta contiene archivos o subcarpetas. Vacíela antes de eliminarla.");
        padre.Directorios.Remove(directorio);
    }

    public IEnumerable<DirectorioVirtual> TodosLosDirectorios()
    {
        var pila = new Stack<DirectorioVirtual>(); pila.Push(Raiz);
        while (pila.Count > 0) { var actual = pila.Pop(); yield return actual; foreach (var d in actual.Directorios) pila.Push(d); }
    }

    public IEnumerable<ArchivoVirtual> TodosLosArchivos() => TodosLosDirectorios().SelectMany(d => d.Archivos);

    public string Cadena(ArchivoVirtual archivo) => archivo.PrimerCluster is null ? "(vacío)" : string.Join(" -> ", Fat.Recorrer(archivo.PrimerCluster)) + " -> EOC";

    public int ClustersUtilizados(ArchivoVirtual archivo) => Fat.Recorrer(archivo.PrimerCluster).Count;
    public long EspacioFisico(ArchivoVirtual archivo) => ClustersUtilizados(archivo) * (long)Configuracion.TamanoClusterBytes;
    public long DesperdicioInterno(ArchivoVirtual archivo) => EspacioFisico(archivo) - archivo.TamanoBytes;

    public void ValidarIntegridad()
    {
        if (Configuracion.ClustersReservados < 2 || Fat.Entradas.Count != Configuracion.CantidadClusters || Clusters.Count != Configuracion.CantidadClusters) throw new InvalidDataException("La geometría guardada no coincide con FAT32 simulado.");
        var usados = new HashSet<int>();
        for (int i = 0; i < Clusters.Count; i++)
        {
            if (i < Configuracion.ClustersReservados && (Fat.Entradas[i] != TablaFat.Reservado || Clusters[i].Estado != EstadoCluster.Reservado || Clusters[i].ArchivoId is not null)) throw new InvalidDataException("Los clusters reservados no son coherentes.");
            if (Fat.Entradas[i] == TablaFat.Libre && (Clusters[i].Estado != EstadoCluster.Libre || Clusters[i].ArchivoId is not null)) throw new InvalidDataException("Un cluster libre conserva estado o propietario.");
        }
        foreach (var archivo in TodosLosArchivos())
        {
            var cadena = Fat.Recorrer(archivo.PrimerCluster);
            int esperados = archivo.TamanoBytes == 0 ? 0 : checked((int)Math.Ceiling(archivo.TamanoBytes / (double)Configuracion.TamanoClusterBytes));
            if (cadena.Count != esperados || (cadena.Count == 0) != (archivo.PrimerCluster is null)) throw new InvalidDataException("El tamaño lógico no coincide con la cadena FAT.");
            foreach (int n in cadena)
                if (n < Configuracion.ClustersReservados || !usados.Add(n) || Clusters[n].Estado != EstadoCluster.Ocupado || Clusters[n].ArchivoId != archivo.Id) throw new InvalidDataException("Asignación FAT inconsistente.");
        }
        for (int i = Configuracion.ClustersReservados; i < Clusters.Count; i++)
            if (Clusters[i].Estado == EstadoCluster.Ocupado && !usados.Contains(i)) throw new InvalidDataException("Existe un cluster ocupado sin una entrada lógica propietaria.");
    }

    private void LiberarCluster(int numero) { Clusters[numero].Estado = EstadoCluster.Libre; Clusters[numero].ArchivoId = null; }
    private static void VerificarDuplicado(DirectorioVirtual padre, string nombre, object? excluir = null)
    {
        if (padre.Archivos.Any(a => !ReferenceEquals(a, excluir) && a.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase)) || padre.Directorios.Any(d => !ReferenceEquals(d, excluir) && d.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Ya existe un elemento con ese nombre.");
    }
    private static string ValidarNombre(string nombre, bool archivo)
    {
        nombre = (nombre ?? "").Trim();
        if (nombre.Length == 0 || nombre.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new ArgumentException("El nombre no es válido.");
        if (archivo && !nombre.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) nombre += ".txt";
        return nombre;
    }
}
