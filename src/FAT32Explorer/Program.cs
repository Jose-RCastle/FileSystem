using FAT32Explorer.Modelo;
using FAT32Explorer.Persistencia;
using FAT32Explorer.UI;

namespace FAT32Explorer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        string ruta = Path.Combine(Application.UserAppDataPath, "disco-virtual.json");
        var almacenamiento = new AlmacenamientoJson(ruta);
        DiscoVirtual disco;
        try { disco = almacenamiento.Cargar() ?? DiscoVirtual.CrearPredeterminado(); }
        catch (Exception ex)
        {
            MessageBox.Show($"{ex.Message}\n\nSe creará un disco virtual nuevo. El archivo anterior no será sobrescrito hasta que guarde o cierre la aplicación.", "Disco incompatible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            disco = DiscoVirtual.CrearPredeterminado();
        }
        Application.Run(new MenuPrincipalForm(disco, almacenamiento));
    }
}
