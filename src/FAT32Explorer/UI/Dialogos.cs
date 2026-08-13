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
        Text = "Configuración del disco virtual"; Size = new Size(440, 285); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog;
        nombre.Text = actual.Nombre; clusters.Value = actual.CantidadClusters; tamano.Value = actual.TamanoClusterBytes; reservados.Value = actual.ClustersReservados;
        var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(15), ColumnCount = 2, RowCount = 6 };
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55)); tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        Agregar("Nombre del disco", nombre); Agregar("Cantidad total de clusters", clusters); Agregar("Tamaño de cluster (bytes)", tamano); Agregar("Clusters reservados", reservados);
        Agregar("Algoritmo", new Label { Text = "First Fit", AutoSize = true });
        var panel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        panel.Controls.Add(new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel }); panel.Controls.Add(new Button { Text = "Crear disco", DialogResult = DialogResult.OK });
        tabla.Controls.Add(panel, 0, 5); tabla.SetColumnSpan(panel, 2); Controls.Add(tabla);
        void Agregar(string texto, Control control) { int fila = tabla.Controls.Count / 2; tabla.Controls.Add(new Label { Text = texto, AutoSize = true, Anchor = AnchorStyles.Left }, 0, fila); control.Dock = DockStyle.Fill; tabla.Controls.Add(control, 1, fila); }
    }
}
