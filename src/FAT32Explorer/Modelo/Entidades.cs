using System.Text.Json.Serialization;

namespace FAT32Explorer.Modelo;

public enum EstadoCluster { Libre, Reservado, Ocupado }
public enum TipoPropietario { Archivo, Directorio }

public sealed class Cluster
{
    public int Numero { get; set; }
    public EstadoCluster Estado { get; set; }
    public string? PropietarioId { get; set; }
    public TipoPropietario? TipoPropietario { get; set; }
}

public sealed class ArchivoVirtual
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nombre { get; set; } = "nuevo.txt";
    public string Contenido { get; set; } = "";
    public long TamanoBytes { get; set; }
    public int? PrimerCluster { get; set; }
    public DateTime Creado { get; set; } = DateTime.Now;
    public DateTime Modificado { get; set; } = DateTime.Now;
}

public sealed class DirectorioVirtual
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nombre { get; set; } = "Carpeta";
    public DateTime Creado { get; set; } = DateTime.Now;
    public int? PrimerCluster { get; set; }
    public List<DirectorioVirtual> Directorios { get; set; } = [];
    public List<ArchivoVirtual> Archivos { get; set; } = [];
}

public sealed class ConfiguracionDisco
{
    public string Nombre { get; set; } = "Disco FAT32";
    public int TamanoClusterBytes { get; set; } = 1024;
    public int CantidadClusters { get; set; } = 128;
    [JsonIgnore] public string AlgoritmoAsignacion => "First Fit";
    [JsonIgnore] public long CapacidadBytes => (long)TamanoClusterBytes * CantidadClusters;
}

public sealed class ConfiguracionSistema
{
    public string Nombre { get; set; } = "FAT32 OS Didáctico";
    public string Usuario { get; set; } = "Estudiante";
}
