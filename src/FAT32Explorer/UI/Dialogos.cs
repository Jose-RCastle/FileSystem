using FAT32Explorer.Modelo;

namespace FAT32Explorer.UI;

internal sealed class EditorTextoForm : Form
{
    private readonly TextBox nombre = new() { Dock = DockStyle.Top };
    private readonly TextBox contenido = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, AcceptsTab = true, Font = new Font("Consolas", 11) };
    public string Nombre => nombre.Text.Trim();
    public string Contenido => contenido.Text;

    private readonly Label metricas = new() { Dock = DockStyle.Bottom, Height = 28, ForeColor = Color.DimGray, Padding = new Padding(10, 5, 0, 0) };
    public EditorTextoForm(ArchivoVirtual? archivo, int tamanoCluster)
    {
        Text = archivo is null ? "Nuevo archivo TXT" : $"Editar - {archivo.Nombre}";
        Size = new Size(760, 560); MinimumSize = new Size(560, 420); StartPosition = FormStartPosition.CenterParent; EstiloVisual.Aplicar(this);
        nombre.Text = archivo?.Nombre ?? "nuevo.txt"; contenido.Text = archivo?.Contenido ?? "";
        nombre.Margin = new Padding(10); contenido.BorderStyle = BorderStyle.FixedSingle;
        var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10, 8, 10, 8), BackColor = EstiloVisual.Fondo };
        botones.Controls.Add(new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, AutoSize = true });
        botones.Controls.Add(new Button { Text = "Guardar", DialogResult = DialogResult.OK, AutoSize = true });
        Controls.Add(contenido); Controls.Add(metricas); Controls.Add(new Label { Text = "Nombre del archivo", Dock = DockStyle.Top, Height = 28, Padding = new Padding(0, 7, 0, 0), Font = new Font("Segoe UI Semibold", 9) }); Controls.Add(nombre); Controls.Add(new Label { Text = "ARCHIVO TXT FAT32", Dock = DockStyle.Top, Height = 38, Padding = new Padding(0, 10, 0, 0), ForeColor = EstiloVisual.TextoSecundario }); Controls.Add(botones); Padding = new Padding(12, 0, 12, 0);
        AcceptButton = (Button)botones.Controls[1]; CancelButton = (Button)botones.Controls[0];
        contenido.TextChanged += (_, _) => ActualizarMetricas(); ActualizarMetricas();
        void ActualizarMetricas() { int bytes = System.Text.Encoding.UTF8.GetByteCount(contenido.Text); int clusters = bytes == 0 ? 0 : (int)Math.Ceiling(bytes / (double)tamanoCluster); metricas.Text = $"Tamaño UTF-8 actual: {bytes:N0} bytes    ·    Clusters necesarios: {clusters}"; }
    }
}

internal static class Entrada
{
    public static string? Pedir(IWin32Window owner, string titulo, string etiqueta, string inicial = "")
    {
        using var form = new Form { Text = titulo, Size = new Size(410, 175), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Font = new Font("Segoe UI", 9), BackColor = EstiloVisual.Fondo };
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
    private readonly ComboBox bytesSector = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox sectoresCluster = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox numeroFat = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox tamano = new() { ReadOnly = true };
    private readonly Label capacidad = new() { AutoSize = true };
    public ConfiguracionDisco Configuracion => new() { Nombre = nombre.Text.Trim(), CantidadClusters = (int)clusters.Value, BytesPorSector = (int)bytesSector.SelectedItem!, SectoresPorCluster = (int)sectoresCluster.SelectedItem!, NumeroDeFat = (int)numeroFat.SelectedItem! };

    public ConfiguracionForm(ConfiguracionDisco actual)
    {
        Text = "Configuración del volumen FAT32"; Size = new Size(560, 520); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; EstiloVisual.Aplicar(this);
        nombre.Text = actual.Nombre; clusters.Value = actual.CantidadClusters;
        bytesSector.Items.Add(512); bytesSector.SelectedItem = actual.BytesPorSector;
        sectoresCluster.Items.AddRange(ConfiguracionDisco.SectoresPorClusterPermitidos.Cast<object>().ToArray()); sectoresCluster.SelectedItem = actual.SectoresPorCluster;
        numeroFat.Items.AddRange([1, 2]); numeroFat.SelectedItem = actual.NumeroDeFat;
        var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(15), ColumnCount = 2, RowCount = 14 };
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55)); tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        Encabezado("GEOMETRÍA"); Agregar("Nombre del volumen", nombre); Agregar("Bytes por sector", bytesSector); Agregar("Sectores por cluster", sectoresCluster); Agregar("Tamaño de cluster (calculado)", tamano);
        Encabezado("ESTRUCTURA FAT32"); Agregar("Entradas especiales", new Label { Text = "FAT[0], FAT[1]", AutoSize = true }); Agregar("Número de FAT", numeroFat); Agregar("Cluster raíz", new Label { Text = "2", AutoSize = true }); Agregar("Clusters de datos", clusters);
        Agregar("Capacidad área de datos", capacidad);
        Agregar("Algoritmo", new Label { Text = "First Fit", AutoSize = true });
        var advertencia = new Label { Text = "La segunda FAT es un espejo conceptual; no hay recuperación automática.\nAplicar la configuración formateará el volumen.", ForeColor = Color.DarkRed, AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) }; tabla.Controls.Add(advertencia, 0, 12); tabla.SetColumnSpan(advertencia, 2);
        var panel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        panel.Controls.Add(new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel }); panel.Controls.Add(new Button { Text = "Crear / Formatear disco", DialogResult = DialogResult.OK, AutoSize = true });
        tabla.Controls.Add(panel, 0, 13); tabla.SetColumnSpan(panel, 2); Controls.Add(tabla);
        sectoresCluster.SelectedIndexChanged += (_, _) => Actualizar(); clusters.ValueChanged += (_, _) => Actualizar(); Actualizar();
        void Agregar(string texto, Control control) { int fila = tabla.Controls.Count / 2; tabla.Controls.Add(new Label { Text = texto, AutoSize = true, Anchor = AnchorStyles.Left }, 0, fila); control.Dock = DockStyle.Fill; tabla.Controls.Add(control, 1, fila); }
        void Encabezado(string texto) { int fila = tabla.Controls.Count / 2; var l = new Label { Text = texto, AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) }; tabla.Controls.Add(l, 0, fila); tabla.SetColumnSpan(l, 2); tabla.Controls.Add(new Label(), 1, fila); }
        void Actualizar() { if (bytesSector.SelectedItem is int b && sectoresCluster.SelectedItem is int s) { tamano.Text = $"{b * s:N0} bytes"; capacidad.Text = $"{(long)b * s * (int)clusters.Value:N0} bytes"; } }
    }
}

internal sealed class ConfiguracionSistemaForm : Form
{
    private readonly TextBox nombre = new(); private readonly TextBox usuario = new();
    public string Nombre => nombre.Text.Trim(); public string Usuario => usuario.Text.Trim();
    public ConfiguracionSistemaForm(ConfiguracionSistema actual)
    {
        Text = "Sistema operativo simulado"; Size = new Size(490, 350); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; EstiloVisual.Aplicar(this);
        nombre.Text = actual.Nombre; usuario.Text = actual.Usuario;
        var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 7 };
        tabla.Controls.Add(new Label { Text = "Nombre del SO", AutoSize = true }, 0, 0); tabla.Controls.Add(nombre, 1, 0); tabla.Controls.Add(new Label { Text = "Usuario", AutoSize = true }, 0, 1); tabla.Controls.Add(usuario, 1, 1);
        string[] info = ["Sistema de archivos: FAT32", "Modo: Simulación", "Unidad raíz: C:\\", "Archivos soportados: TXT"];
        for (int i = 0; i < info.Length; i++) { var l = new Label { Text = info[i], AutoSize = true, ForeColor = Color.DimGray }; tabla.Controls.Add(l, 0, i + 2); tabla.SetColumnSpan(l, 2); }
        var botones = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill }; botones.Controls.Add(new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel }); botones.Controls.Add(new Button { Text = "Guardar", DialogResult = DialogResult.OK }); tabla.Controls.Add(botones, 0, 6); tabla.SetColumnSpan(botones, 2); Controls.Add(tabla);
    }
}
