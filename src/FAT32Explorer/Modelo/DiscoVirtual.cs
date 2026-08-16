using System.Text;
using System.Text.Json.Serialization;

namespace FAT32Explorer.Modelo;

public sealed class DiscoVirtual
{
    public const int VersionActual = 3;
    public const int TamanoEntradaDirectorio = 32;
    public int VersionModelo { get; set; } = VersionActual;
    public ConfiguracionDisco Configuracion { get; set; } = new();
    public ConfiguracionSistema SistemaOperativo { get; set; } = new();
    public TablaFat Fat { get; set; } = new();
    public List<Cluster> Clusters { get; set; } = [];
    public DirectorioVirtual Raiz { get; set; } = new() { Nombre = "C:\\" };

    [JsonIgnore] public long EspacioUsado => Clusters.Count(c => c.Estado == EstadoCluster.Ocupado) * (long)Configuracion.TamanoClusterBytes;
    [JsonIgnore] public long EspacioLibre => Configuracion.CapacidadBytes - EspacioUsado;

    public static DiscoVirtual CrearPredeterminado() => Crear(new ConfiguracionDisco());

    public static DiscoVirtual Crear(ConfiguracionDisco configuracion)
    {
        var disco = new DiscoVirtual
        {
            Configuracion = configuracion,
            Fat = TablaFat.Crear(configuracion.CantidadClusters),
            Clusters = Enumerable.Range(0, configuracion.CantidadClusters + 2).Select(i => new Cluster
            { Numero = i, Estado = i < 2 ? EstadoCluster.Reservado : EstadoCluster.Libre }).ToList()
        };
        disco.RedimensionarDirectorio(disco.Raiz);
        return disco;
    }

    public DirectorioVirtual CrearDirectorio(DirectorioVirtual padre, string nombre)
    {
        nombre = ValidarNombre(nombre, false); VerificarDuplicado(padre, nombre);
        var nuevo = new DirectorioVirtual { Nombre = nombre };
        int adicionales = ClustersNecesarios(TamanoLogicoDirectorio(nuevo)) + AdicionalesDirectorio(padre, 1);
        VerificarEspacio(adicionales, "No hay suficiente espacio para crear el directorio.");
        RedimensionarDirectorio(nuevo); padre.Directorios.Add(nuevo); RedimensionarDirectorio(padre);
        return nuevo;
    }

    public ArchivoVirtual CrearArchivo(DirectorioVirtual padre, string nombre, string contenido)
    {
        nombre = ValidarNombre(nombre, true); VerificarDuplicado(padre, nombre); contenido ??= "";
        var archivo = new ArchivoVirtual { Nombre = nombre, Contenido = contenido, TamanoBytes = Encoding.UTF8.GetByteCount(contenido) };
        int adicionales = ClustersNecesariosArchivo(archivo.TamanoBytes) + AdicionalesDirectorio(padre, 1);
        VerificarEspacio(adicionales, "No hay espacio suficiente para crear el archivo.");
        RedimensionarArchivo(archivo); padre.Archivos.Add(archivo); RedimensionarDirectorio(padre);
        return archivo;
    }

    public void EditarArchivo(DirectorioVirtual padre, ArchivoVirtual archivo, string nuevoNombre, string contenido)
    {
        nuevoNombre = ValidarNombre(nuevoNombre, true); VerificarDuplicado(padre, nuevoNombre, archivo); contenido ??= "";
        long bytes = Encoding.UTF8.GetByteCount(contenido);
        int adicionales = Math.Max(0, ClustersNecesariosArchivo(bytes) - Fat.Recorrer(archivo.PrimerCluster).Count);
        VerificarEspacio(adicionales, "No hay espacio suficiente. El archivo original no fue modificado.");
        archivo.Contenido = contenido; archivo.TamanoBytes = bytes; RedimensionarArchivo(archivo);
        archivo.Nombre = nuevoNombre; archivo.Modificado = DateTime.Now;
    }

    public void ReemplazarContenido(ArchivoVirtual archivo, string contenido)
    {
        var padre = TodosLosDirectorios().Single(d => d.Archivos.Contains(archivo));
        EditarArchivo(padre, archivo, archivo.Nombre, contenido);
    }

    public void RenombrarArchivo(DirectorioVirtual padre, ArchivoVirtual archivo, string nuevoNombre)
    {
        if (!padre.Archivos.Contains(archivo)) throw new InvalidOperationException("El archivo no pertenece al directorio indicado.");
        nuevoNombre = ValidarNombre(nuevoNombre, true); VerificarDuplicado(padre, nuevoNombre, archivo);
        archivo.Nombre = nuevoNombre; archivo.Modificado = DateTime.Now;
    }

    public void RenombrarDirectorio(DirectorioVirtual padre, DirectorioVirtual directorio, string nuevoNombre)
    {
        ValidarDirectorioMovible(padre, directorio); nuevoNombre = ValidarNombre(nuevoNombre, false); VerificarDuplicado(padre, nuevoNombre, directorio);
        directorio.Nombre = nuevoNombre;
    }

    public void EliminarArchivo(DirectorioVirtual padre, ArchivoVirtual archivo)
    {
        if (!padre.Archivos.Contains(archivo)) throw new InvalidOperationException("El archivo no pertenece al directorio indicado.");
        LiberarPropietario(archivo.PrimerCluster); padre.Archivos.Remove(archivo); RedimensionarDirectorio(padre);
    }

    public void MoverArchivo(DirectorioVirtual origen, DirectorioVirtual destino, ArchivoVirtual archivo)
    {
        ValidarDirectorioExistente(origen); ValidarDirectorioExistente(destino);
        if (ReferenceEquals(origen, destino)) return;
        if (!origen.Archivos.Contains(archivo)) throw new InvalidOperationException("El archivo no pertenece al directorio de origen.");
        VerificarDuplicado(destino, archivo.Nombre);
        int neto = Math.Max(0, AdicionalesDirectorio(destino, 1) - ClustersLiberablesDirectorio(origen, -1));
        VerificarEspacio(neto, "No hay espacio para actualizar el directorio de destino.");
        origen.Archivos.Remove(archivo); RedimensionarDirectorio(origen); destino.Archivos.Add(archivo); RedimensionarDirectorio(destino); archivo.Modificado = DateTime.Now;
    }

    public void MoverDirectorio(DirectorioVirtual origen, DirectorioVirtual destino, DirectorioVirtual directorio)
    {
        ValidarDirectorioExistente(origen); ValidarDirectorioExistente(destino);
        ValidarDirectorioMovible(origen, directorio);
        if (ReferenceEquals(origen, destino)) return;
        if (ReferenceEquals(directorio, destino) || EsDescendiente(directorio, destino)) throw new InvalidOperationException("No se puede mover una carpeta dentro de sí misma o de uno de sus descendientes.");
        VerificarDuplicado(destino, directorio.Nombre);
        int neto = Math.Max(0, AdicionalesDirectorio(destino, 1) - ClustersLiberablesDirectorio(origen, -1));
        VerificarEspacio(neto, "No hay espacio para actualizar el directorio de destino.");
        origen.Directorios.Remove(directorio); RedimensionarDirectorio(origen); destino.Directorios.Add(directorio); RedimensionarDirectorio(destino);
    }

    public void EliminarDirectorio(DirectorioVirtual padre, DirectorioVirtual directorio) => EliminarDirectorio(padre, directorio, false);

    public void EliminarDirectorio(DirectorioVirtual padre, DirectorioVirtual directorio, bool recursivo)
    {
        ValidarDirectorioMovible(padre, directorio);
        if (!recursivo && (directorio.Archivos.Count > 0 || directorio.Directorios.Count > 0))
            throw new InvalidOperationException($"No se puede eliminar \"{directorio.Nombre}\".\n\nLa carpeta contiene archivos o subcarpetas. Vacíela antes de eliminarla.");
        // Antes de una eliminación recursiva se recorren todas las cadenas y el árbol;
        // después de esta comprobación, liberar clusters no puede fallar a mitad del proceso.
        if (recursivo) ValidarIntegridad();
        foreach (var archivo in directorio.Archivos.ToList()) LiberarPropietario(archivo.PrimerCluster);
        foreach (var hijo in directorio.Directorios.ToList()) EliminarArbol(hijo);
        LiberarPropietario(directorio.PrimerCluster); padre.Directorios.Remove(directorio); RedimensionarDirectorio(padre);
    }

    private void EliminarArbol(DirectorioVirtual directorio)
    {
        foreach (var a in directorio.Archivos) LiberarPropietario(a.PrimerCluster);
        foreach (var d in directorio.Directorios) EliminarArbol(d);
        LiberarPropietario(directorio.PrimerCluster);
    }

    public IEnumerable<DirectorioVirtual> TodosLosDirectorios()
    {
        var pila = new Stack<DirectorioVirtual>(); var vistos = new HashSet<string>(); pila.Push(Raiz);
        while (pila.Count > 0) { var actual = pila.Pop(); if (!vistos.Add(actual.Id)) throw new InvalidDataException("Existe un ciclo en el árbol de directorios."); yield return actual; foreach (var d in actual.Directorios) pila.Push(d); }
    }
    public IEnumerable<ArchivoVirtual> TodosLosArchivos() => TodosLosDirectorios().SelectMany(d => d.Archivos);
    public string Cadena(ArchivoVirtual archivo) => FormatearCadena(archivo.PrimerCluster);
    public string Cadena(DirectorioVirtual directorio) => FormatearCadena(directorio.PrimerCluster);
    private string FormatearCadena(int? primero) => primero is null ? "(vacío)" : string.Join(" -> ", Fat.Recorrer(primero)) + " -> EOC";

    public int ClustersUtilizados(ArchivoVirtual archivo) => Fat.Recorrer(archivo.PrimerCluster).Count;
    public long EspacioFisico(ArchivoVirtual archivo) => ClustersUtilizados(archivo) * (long)Configuracion.TamanoClusterBytes;
    public long DesperdicioInterno(ArchivoVirtual archivo) => EspacioFisico(archivo) - archivo.TamanoBytes;
    public long TamanoLogicoDirectorio(DirectorioVirtual d) => (1L + d.Archivos.Count + d.Directorios.Count) * TamanoEntradaDirectorio;
    public int ClustersUtilizadosDirectorio(DirectorioVirtual d) => Fat.Recorrer(d.PrimerCluster).Count;
    public long EspacioFisicoDirectorio(DirectorioVirtual d) => ClustersUtilizadosDirectorio(d) * (long)Configuracion.TamanoClusterBytes;
    public long DesperdicioInternoDirectorio(DirectorioVirtual d) => EspacioFisicoDirectorio(d) - TamanoLogicoDirectorio(d);
    public int ArchivosTotales(DirectorioVirtual d) => d.Archivos.Count + d.Directorios.Sum(ArchivosTotales);
    public int SubdirectoriosTotales(DirectorioVirtual d) => d.Directorios.Count + d.Directorios.Sum(SubdirectoriosTotales);
    public long TamanoContenido(DirectorioVirtual d) => d.Archivos.Sum(a => a.TamanoBytes) + d.Directorios.Sum(TamanoContenido);
    public long EspacioTotalFisico(DirectorioVirtual d) => EspacioFisicoDirectorio(d) + d.Archivos.Sum(EspacioFisico) + d.Directorios.Sum(EspacioTotalFisico);

    public void ValidarIntegridad()
    {
        if (VersionModelo != VersionActual) throw new InvalidDataException("El disco guardado pertenece a una versión anterior del simulador y debe recrearse.");
        if (Fat.Entradas.Count != Configuracion.CantidadClusters + 2 || Clusters.Count != Configuracion.CantidadClusters + 2) throw new InvalidDataException("La geometría FAT no coincide con el área de datos.");
        if (Fat.Entradas[0] != TablaFat.Reservado || Fat.Entradas[1] != TablaFat.Reservado) throw new InvalidDataException("FAT[0] y FAT[1] deben ser entradas especiales RESERVED.");
        var objetos = new Dictionary<string, (TipoPropietario tipo, int? primero, int necesarios)>();
        var directorios = new HashSet<string>(); var archivos = new HashSet<string>();
        ValidarArbol(Raiz, true);
        void ValidarArbol(DirectorioVirtual d, bool raiz)
        {
            if (!directorios.Add(d.Id)) throw new InvalidDataException("Un directorio tiene varios padres o existe un ciclo lógico.");
            if (raiz && d.Nombre != "C:\\") throw new InvalidDataException("El directorio raíz no es válido.");
            objetos.Add(d.Id, (TipoPropietario.Directorio, d.PrimerCluster, ClustersNecesarios(TamanoLogicoDirectorio(d))));
            foreach (var a in d.Archivos) { if (!archivos.Add(a.Id)) throw new InvalidDataException("Un archivo aparece en más de un directorio."); objetos.Add(a.Id, (TipoPropietario.Archivo, a.PrimerCluster, ClustersNecesariosArchivo(a.TamanoBytes))); }
            foreach (var h in d.Directorios) ValidarArbol(h, false);
        }
        var usados = new HashSet<int>();
        foreach (var (id, dato) in objetos)
        {
            var cadena = Fat.Recorrer(dato.primero);
            if (cadena.Count != dato.necesarios) throw new InvalidDataException("El tamaño lógico no coincide con la cadena FAT.");
            foreach (int n in cadena) if (n < 2 || !usados.Add(n) || Clusters[n].Estado != EstadoCluster.Ocupado || Clusters[n].PropietarioId != id || Clusters[n].TipoPropietario != dato.tipo) throw new InvalidDataException("Propietario de cluster inconsistente.");
        }
        for (int i = 0; i < Clusters.Count; i++)
        {
            var c = Clusters[i];
            if (c.Numero != i) throw new InvalidDataException("La numeración de clusters no es coherente.");
            if (i < 2 && (c.Estado != EstadoCluster.Reservado || c.PropietarioId is not null || c.TipoPropietario is not null)) throw new InvalidDataException("Una entrada especial tiene propietario.");
            if (i >= 2 && Fat.Entradas[i] == TablaFat.Reservado) throw new InvalidDataException("Solo FAT[0] y FAT[1] pueden ser RESERVED.");
            if (i >= 2 && Fat.Entradas[i] == TablaFat.Libre && (c.Estado != EstadoCluster.Libre || c.PropietarioId is not null || c.TipoPropietario is not null)) throw new InvalidDataException("Un cluster libre conserva propietario.");
            if (i >= 2 && c.Estado == EstadoCluster.Ocupado && !usados.Contains(i)) throw new InvalidDataException("Existe un cluster ocupado huérfano.");
        }
    }

    private int ClustersNecesarios(long bytes) => Math.Max(1, checked((int)Math.Ceiling(bytes / (double)Configuracion.TamanoClusterBytes)));
    private int ClustersNecesariosArchivo(long bytes) => bytes == 0 ? 0 : checked((int)Math.Ceiling(bytes / (double)Configuracion.TamanoClusterBytes));
    private int AdicionalesDirectorio(DirectorioVirtual d, int cambio) => Math.Max(0, ClustersNecesarios((1L + d.Archivos.Count + d.Directorios.Count + cambio) * TamanoEntradaDirectorio) - ClustersUtilizadosDirectorio(d));
    private int ClustersLiberablesDirectorio(DirectorioVirtual d, int cambio) => Math.Max(0, ClustersUtilizadosDirectorio(d) - ClustersNecesarios((1L + d.Archivos.Count + d.Directorios.Count + cambio) * TamanoEntradaDirectorio));
    private void VerificarEspacio(int necesarios, string mensaje) { if (Fat.BuscarLibres(necesarios).Count != necesarios) throw new IOException(mensaje); }
    private void RedimensionarArchivo(ArchivoVirtual a) => a.PrimerCluster = Redimensionar(a.Id, TipoPropietario.Archivo, a.PrimerCluster, ClustersNecesariosArchivo(a.TamanoBytes));
    private void RedimensionarDirectorio(DirectorioVirtual d) => d.PrimerCluster = Redimensionar(d.Id, TipoPropietario.Directorio, d.PrimerCluster, ClustersNecesarios(TamanoLogicoDirectorio(d)));
    private int? Redimensionar(string id, TipoPropietario tipo, int? primero, int necesarios)
    {
        var anterior = Fat.Recorrer(primero).ToList(); var libres = Fat.BuscarLibres(Math.Max(0, necesarios - anterior.Count));
        if (libres.Count < necesarios - anterior.Count) throw new IOException("No hay espacio suficiente en el disco virtual.");
        var nueva = anterior.Take(necesarios).Concat(libres).ToList();
        foreach (int n in anterior.Skip(necesarios)) LiberarCluster(n);
        Fat.Enlazar(nueva); foreach (int n in nueva) { Clusters[n].Estado = EstadoCluster.Ocupado; Clusters[n].PropietarioId = id; Clusters[n].TipoPropietario = tipo; }
        return nueva.Count == 0 ? null : nueva[0];
    }
    private void LiberarPropietario(int? primero) { foreach (int n in Fat.Recorrer(primero).ToList()) LiberarCluster(n); }
    private void LiberarCluster(int n) { Fat.Entradas[n] = TablaFat.Libre; Clusters[n].Estado = EstadoCluster.Libre; Clusters[n].PropietarioId = null; Clusters[n].TipoPropietario = null; }
    private void ValidarDirectorioMovible(DirectorioVirtual padre, DirectorioVirtual d) { if (ReferenceEquals(d, Raiz)) throw new InvalidOperationException("No se puede mover, renombrar ni eliminar el directorio raíz C:\\."); if (!padre.Directorios.Contains(d)) throw new InvalidOperationException("La carpeta no pertenece al directorio de origen."); }
    private void ValidarDirectorioExistente(DirectorioVirtual d) { if (!TodosLosDirectorios().Any(x => ReferenceEquals(x, d))) throw new InvalidOperationException("El directorio no pertenece a este disco virtual."); }
    private static bool EsDescendiente(DirectorioVirtual posibleAncestro, DirectorioVirtual objetivo) => posibleAncestro.Directorios.Any(d => ReferenceEquals(d, objetivo) || EsDescendiente(d, objetivo));
    private static void VerificarDuplicado(DirectorioVirtual padre, string nombre, object? excluir = null) { if (padre.Archivos.Any(a => !ReferenceEquals(a, excluir) && a.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase)) || padre.Directorios.Any(d => !ReferenceEquals(d, excluir) && d.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Ya existe un elemento con ese nombre."); }
    private static string ValidarNombre(string nombre, bool archivo) { nombre = (nombre ?? "").Trim(); if (nombre.Length == 0 || nombre.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new ArgumentException("El nombre no es válido."); if (archivo && !nombre.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) nombre += ".txt"; return nombre; }
}
