using FAT32Explorer.Modelo;

namespace FAT32Explorer.UI;

internal sealed class EditorTextoForm : Form
{
    private readonly TextBox nombre = new() { Dock = DockStyle.Top };
    private readonly TextBox contenido = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, AcceptsTab = true, Font = new Font("Consolas", 11) };
    public string Nombre => nombre.Text.Trim();
    public string Contenido => contenido.Text;

    public EditorTextoForm(ArchivoVirtual? archivo)
    {
        Text = archivo is null ? "Nuevo archivo TXT" : $"Editar - {archivo.Nombre}";
        Size = new Size(720, 520); StartPosition = FormStartPosition.CenterParent;
        nombre.Text = archivo?.Nombre ?? "nuevo.txt"; contenido.Text = archivo?.Contenido ?? "";
        var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(5) };
        botones.Controls.Add(new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, AutoSize = true });
        botones.Controls.Add(new Button { Text = "Guardar", DialogResult = DialogResult.OK, AutoSize = true });
        Controls.Add(contenido); Controls.Add(new Label { Text = "Nombre del archivo:", Dock = DockStyle.Top, Height = 22 }); Controls.Add(nombre); Controls.Add(botones);
        AcceptButton = (Button)botones.Controls[1]; CancelButton = (Button)botones.Controls[0];
    }
}

internal static class Entrada
{
    public static string? Pedir(IWin32Window owner, string titulo, string etiqueta, string inicial = "")
    {
        using var form = new Form { Text = titulo, Size = new Size(390, 150), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var caja = new TextBox { Text = inicial, Left = 15, Top = 35, Width = 340 };
        var aceptar = new Button { Text = "Aceptar", DialogResult = DialogResult.OK, Left = 190, Top = 70 };
        form.Controls.AddRange([new Label { Text = etiqueta, Left = 15, Top = 12, AutoSize = true }, caja, aceptar, new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Left = 280, Top = 70 }]);
        form.AcceptButton = aceptar;
        return form.ShowDialog(owner) == DialogResult.OK ? caja.Text : null;
    }
}

internal sealed class ConfiguracionForm : Form
{
    private readonly TextBox nombre = new();
    private readonly NumericUpDown clusters = new() { Minimum = 8, Maximum = 4096 };
    private readonly NumericUpDown tamano = new() { Minimum = 64, Maximum = 1048576, Increment = 64 };
    private readonly NumericUpDown reservados = new() { Minimum = 1, Maximum = 32 };
    public ConfiguracionDisco Configuracion => new() { Nombre = nombre.Text.Trim(), CantidadClusters = (int)clusters.Value, TamanoClusterBytes = (int)tamano.Value, ClustersReservados = (int)reservados.Value };

    public ConfiguracionForm(ConfiguracionDisco actual)
    {
        Text = "Configuración del disco virtual FAT32"; Size = new Size(500, 355); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog;
        nombre.Text = actual.Nombre; clusters.Value = actual.CantidadClusters; tamano.Value = actual.TamanoClusterBytes; reservados.Value = actual.ClustersReservados;
        var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(15), ColumnCount = 2, RowCount = 8 };
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55)); tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        Agregar("Nombre del disco", nombre); Agregar("Cantidad total de clusters", clusters); Agregar("Tamaño de cluster (bytes)", tamano); Agregar("Clusters reservados", reservados);
        Agregar("Capacidad total calculada", new Label { Text = $"{(long)actual.CantidadClusters * actual.TamanoClusterBytes:N0} bytes", AutoSize = true });
        Agregar("Algoritmo", new Label { Text = "First Fit", AutoSize = true });
        var advertencia = new Label { Text = "ADVERTENCIA: Aplicar esta configuración formateará el disco virtual y eliminará su contenido.", ForeColor = Color.DarkRed, AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) }; tabla.Controls.Add(advertencia, 0, 6); tabla.SetColumnSpan(advertencia, 2);
        var panel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        panel.Controls.Add(new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel }); panel.Controls.Add(new Button { Text = "Crear / Formatear disco", DialogResult = DialogResult.OK, AutoSize = true });
        tabla.Controls.Add(panel, 0, 7); tabla.SetColumnSpan(panel, 2); Controls.Add(tabla);
        void Agregar(string texto, Control control) { int fila = tabla.Controls.Count / 2; tabla.Controls.Add(new Label { Text = texto, AutoSize = true, Anchor = AnchorStyles.Left }, 0, fila); control.Dock = DockStyle.Fill; tabla.Controls.Add(control, 1, fila); }
    }
}

internal sealed class ConfiguracionSistemaForm : Form
{
    private readonly TextBox nombre = new(); private readonly TextBox usuario = new();
    public string Nombre => nombre.Text.Trim(); public string Usuario => usuario.Text.Trim();
    public ConfiguracionSistemaForm(ConfiguracionSistema actual)
    {
        Text = "Configuración del sistema operativo virtual"; Size = new Size(470, 330); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog;
        nombre.Text = actual.Nombre; usuario.Text = actual.Usuario;
        var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 7 };
        tabla.Controls.Add(new Label { Text = "Nombre del SO", AutoSize = true }, 0, 0); tabla.Controls.Add(nombre, 1, 0); tabla.Controls.Add(new Label { Text = "Usuario", AutoSize = true }, 0, 1); tabla.Controls.Add(usuario, 1, 1);
        string[] info = ["Sistema de archivos: FAT32 simulado", "Unidad raíz: C:\\", "Estructura: Directorios jerárquicos", "Archivos soportados: TXT"];
        for (int i = 0; i < info.Length; i++) { var l = new Label { Text = info[i], AutoSize = true, ForeColor = Color.DimGray }; tabla.Controls.Add(l, 0, i + 2); tabla.SetColumnSpan(l, 2); }
        var botones = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill }; botones.Controls.Add(new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel }); botones.Controls.Add(new Button { Text = "Guardar", DialogResult = DialogResult.OK }); tabla.Controls.Add(botones, 0, 6); tabla.SetColumnSpan(botones, 2); Controls.Add(tabla);
    }
}
