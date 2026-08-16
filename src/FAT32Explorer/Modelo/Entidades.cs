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
    public DateTime Modificado { get; set; } = DateTime.Now;
    public int? PrimerCluster { get; set; }
    public List<DirectorioVirtual> Directorios { get; set; } = [];
    public List<ArchivoVirtual> Archivos { get; set; } = [];
}

public sealed class ConfiguracionDisco
{
    public static readonly int[] SectoresPorClusterPermitidos = [1, 2, 4, 8, 16, 32, 64];
    public string Nombre { get; set; } = "Disco FAT32";
    public int BytesPorSector { get; set; } = 512;
    public int SectoresPorCluster { get; set; } = 8;
    public int NumeroDeFat { get; set; } = 1;
    public int SectoresReservados { get; set; } = 32;
    public int CantidadClusters { get; set; } = 128;
    [JsonIgnore] public int TamanoClusterBytes => checked(BytesPorSector * SectoresPorCluster);
    [JsonIgnore] public int PrimerClusterDatos => 2;
    [JsonIgnore] public int ClusterRaiz => 2;
    [JsonIgnore] public string AlgoritmoAsignacion => "First Fit";
    [JsonIgnore] public long CapacidadBytes => (long)TamanoClusterBytes * CantidadClusters;

    public void Validar()
    {
        if (BytesPorSector != 512) throw new ArgumentOutOfRangeException(nameof(BytesPorSector), "Bytes por sector debe ser 512 en esta simulación.");
        if (!SectoresPorClusterPermitidos.Contains(SectoresPorCluster)) throw new ArgumentOutOfRangeException(nameof(SectoresPorCluster), "Sectores por cluster debe ser 1, 2, 4, 8, 16, 32 o 64.");
        if (NumeroDeFat is not (1 or 2)) throw new ArgumentOutOfRangeException(nameof(NumeroDeFat), "El número de FAT debe ser 1 o 2.");
        if (SectoresReservados <= 0) throw new ArgumentOutOfRangeException(nameof(SectoresReservados));
        if (CantidadClusters <= 0) throw new ArgumentOutOfRangeException(nameof(CantidadClusters), "Debe existir al menos un cluster de datos.");
        if (string.IsNullOrWhiteSpace(Nombre)) throw new ArgumentException("El nombre del volumen es obligatorio.", nameof(Nombre));
        _ = TamanoClusterBytes;
    }
}

public sealed class ConfiguracionSistema
{
    public string Nombre { get; set; } = "FAT32 OS Didáctico";
    public string Usuario { get; set; } = "Estudiante";
}
