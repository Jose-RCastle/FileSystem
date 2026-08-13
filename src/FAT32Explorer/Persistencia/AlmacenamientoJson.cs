using System.Text.Json;
using System.Text.Json.Serialization;
using FAT32Explorer.Modelo;

namespace FAT32Explorer.Persistencia;

public sealed class AlmacenamientoJson(string ruta)
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public void Guardar(DiscoVirtual disco)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        string temporal = ruta + ".tmp";
        File.WriteAllText(temporal, JsonSerializer.Serialize(disco, Opciones));
        File.Move(temporal, ruta, true);
    }

    public DiscoVirtual? Cargar()
    {
        if (!File.Exists(ruta)) return null;
        var disco = JsonSerializer.Deserialize<DiscoVirtual>(File.ReadAllText(ruta), Opciones);
        disco?.ValidarIntegridad();
        return disco;
    }
}
