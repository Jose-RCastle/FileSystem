using FAT32Explorer.Modelo;
using FAT32Explorer.Persistencia;

namespace FAT32Explorer.UI;

/// <summary>
/// Menú principal del simulador. Reúne las cuatro categorías pedidas en el enunciado:
/// Configuración OS, Configuración Disco Local, Ejecución de Operaciones con archivos y Salir.
/// Las tres primeras reutilizan exactamente los mismos formularios ya probados
/// (ConfiguracionSistemaForm, ConfiguracionForm y ExploradorForm); este formulario solo actúa
/// como punto de entrada y no duplica lógica de negocio.
/// </summary>
public sealed class MenuPrincipalForm : Form
{
    private DiscoVirtual disco;
    private readonly AlmacenamientoJson almacenamiento;

    public MenuPrincipalForm(DiscoVirtual disco, AlmacenamientoJson almacenamiento)
    {
        this.disco = disco; this.almacenamiento = almacenamiento;
        Text = "FAT32Explorer — Menú principal";
        Size = new Size(640, 600); MinimumSize = new Size(600, 560);
        StartPosition = FormStartPosition.CenterScreen; MaximizeBox = false; FormBorderStyle = FormBorderStyle.FixedSingle;
        EstiloVisual.Aplicar(this);

        var titulo = new Label { Text = "FAT32Explorer", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(30, 26, 0, 0), Font = new Font("Segoe UI Semibold", 20), ForeColor = Color.FromArgb(28, 34, 42) };
        var subtitulo = new Label { Text = "Simulador didáctico de sistema de archivos FAT32  ·  Sistemas Operativos I", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(30, 4, 0, 20), ForeColor = EstiloVisual.TextoSecundario };

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(30, 0, 30, 24), ColumnCount = 1, RowCount = 4 };
        for (int i = 0; i < 4; i++) panel.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        panel.Controls.Add(CrearOpcion(
            "1.  Configuración del Sistema Operativo",
            "Nombre del SO virtual, usuario, capacidades físicas y distribución lógica de los archivos.",
            AbrirConfiguracionOS), 0, 0);
        panel.Controls.Add(CrearOpcion(
            "2.  Configuración del Disco Local",
            "Geometría del volumen: bytes por sector, tamaño de cluster, número de FAT y algoritmo de asignación.",
            AbrirConfiguracionDisco), 0, 1);
        panel.Controls.Add(CrearOpcion(
            "3.  Ejecución de Operaciones con Archivos",
            "Crear, guardar, reemplazar, eliminar y mover archivos y carpetas en el disco virtual.",
            AbrirExplorador), 0, 2);
        panel.Controls.Add(CrearOpcion(
            "4.  Salir",
            "Cerrar FAT32Explorer.",
            Close, esSalir: true), 0, 3);

        Controls.Add(panel); Controls.Add(subtitulo); Controls.Add(titulo);
    }

    private static Panel CrearOpcion(string titulo, string descripcion, Action accion, bool esSalir = false)
    {
        var tarjeta = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 8, 0, 8), Padding = new Padding(20, 16, 20, 16), Cursor = Cursors.Hand };
        var lblTitulo = new Label { Text = titulo, Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 13), ForeColor = esSalir ? Color.FromArgb(178, 34, 34) : EstiloVisual.Acento };
        var lblDescripcion = new Label { Text = descripcion, Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 6, 0, 0), ForeColor = EstiloVisual.TextoSecundario };
        tarjeta.Controls.Add(lblDescripcion); tarjeta.Controls.Add(lblTitulo);
        void Activar(object? s, EventArgs e) => accion();
        tarjeta.Click += Activar; lblTitulo.Click += Activar; lblDescripcion.Click += Activar;
        tarjeta.MouseEnter += (_, _) => tarjeta.BackColor = Color.FromArgb(240, 245, 250);
        tarjeta.MouseLeave += (_, _) => tarjeta.BackColor = Color.White;
        return tarjeta;
    }

    private void AbrirConfiguracionOS()
    {
        using var form = new ConfiguracionSistemaForm(disco.SistemaOperativo, disco.Configuracion);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        if (form.Nombre.Length == 0 || form.Usuario.Length == 0) { MessageBox.Show(this, "Nombre del SO y usuario son obligatorios."); return; }
        disco.SistemaOperativo.Nombre = form.Nombre; disco.SistemaOperativo.Usuario = form.Usuario;
        GuardarSilenciosamente();
    }

    private void AbrirConfiguracionDisco()
    {
        if ((disco.TodosLosArchivos().Any() || disco.Raiz.Directorios.Any()) &&
            MessageBox.Show(this, "ADVERTENCIA:\n\nAplicar esta configuración formateará el disco virtual y eliminará todos sus archivos y carpetas.\n\n¿Continuar?", "Formatear disco virtual", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        using var form = new ConfiguracionForm(disco.Configuracion);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var sistema = disco.SistemaOperativo;
            disco = DiscoVirtual.Crear(form.Configuracion);
            disco.SistemaOperativo = sistema;
            GuardarSilenciosamente();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "No se pudo aplicar la configuración", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void AbrirExplorador()
    {
        using var explorador = new ExploradorForm(disco, almacenamiento);
        explorador.ShowDialog(this);
        // El explorador puede reformatear o modificar el disco internamente;
        // recargamos desde el JSON para que el menú principal quede sincronizado.
        disco = almacenamiento.Cargar() ?? disco;
    }

    private void GuardarSilenciosamente() { try { almacenamiento.Guardar(disco); } catch { } }
}
