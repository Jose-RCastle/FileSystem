using FAT32Explorer.Modelo;

namespace FAT32Explorer.UI;

internal sealed class EditorTextoForm : Form
{
    private readonly TextBox nombre = new() { Dock = DockStyle.Top };
    private readonly TextBox contenido = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, AcceptsTab = true, Font = new Font("Consolas", 11) };
    public string Nombre => nombre.Text.Trim();
    public string Contenido => contenido.Text;

    private readonly Label metricas = new() { AutoSize = true, Dock = DockStyle.Fill, ForeColor = Color.DimGray, Padding = new Padding(0, 6, 0, 6) };
    public EditorTextoForm(ArchivoVirtual? archivo, int tamanoCluster)
    {
        Text = archivo is null ? "Nuevo archivo TXT" : $"Editar - {archivo.Nombre}";
        Size = new Size(760, 560); MinimumSize = new Size(560, 420); StartPosition = FormStartPosition.CenterParent; EstiloVisual.Aplicar(this);
        nombre.Text = archivo?.Nombre ?? "nuevo.txt"; contenido.Text = archivo?.Contenido ?? "";
        nombre.Margin = new Padding(10); contenido.BorderStyle = BorderStyle.FixedSingle;
        var botones = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 6, 0, 6), BackColor = EstiloVisual.Fondo };
        botones.Controls.Add(EstiloVisual.Boton("Cancelar", DialogResult.Cancel));
        botones.Controls.Add(EstiloVisual.Boton("Guardar", DialogResult.OK));
        var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(12, 4, 12, 4) };
        tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabla.Controls.Add(new Label { Text = "ARCHIVO TXT FAT32", AutoSize = true, Padding = new Padding(0, 8, 0, 8), ForeColor = EstiloVisual.TextoSecundario }, 0, 0);
        tabla.Controls.Add(new Label { Text = "Nombre del archivo", AutoSize = true, Padding = new Padding(0, 4, 0, 4), Font = new Font("Segoe UI Semibold", 9) }, 0, 1); tabla.Controls.Add(nombre, 0, 2); tabla.Controls.Add(contenido, 0, 3); tabla.Controls.Add(metricas, 0, 4); tabla.Controls.Add(botones, 0, 5); Controls.Add(tabla);
        AcceptButton = (Button)botones.Controls[1]; CancelButton = (Button)botones.Controls[0];
        contenido.TextChanged += (_, _) => ActualizarMetricas(); ActualizarMetricas();
        void ActualizarMetricas() { int bytes = System.Text.Encoding.UTF8.GetByteCount(contenido.Text); int clusters = bytes == 0 ? 0 : (int)Math.Ceiling(bytes / (double)tamanoCluster); metricas.Text = $"Tamaño UTF-8 actual: {bytes:N0} bytes    ·    Clusters necesarios: {clusters}"; }
    }
}

internal static class Entrada
{
    public static string? Pedir(IWin32Window owner, string titulo, string etiqueta, string inicial = "")
    {
        using var form = new Form { Text = titulo, ClientSize = new Size(420, 150), MinimumSize = new Size(380, 180), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        EstiloVisual.Aplicar(form);
        var caja = new TextBox { Text = inicial, Dock = DockStyle.Fill };
        var aceptar = EstiloVisual.Boton("Aceptar", DialogResult.OK); var cancelar = EstiloVisual.Boton("Cancelar", DialogResult.Cancel);
        var botones = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false }; botones.Controls.Add(cancelar); botones.Controls.Add(aceptar);
        var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(15), ColumnCount = 1, RowCount = 3 }; tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tabla.Controls.Add(new Label { Text = etiqueta, AutoSize = true, Margin = new Padding(0, 0, 0, 6) }, 0, 0); tabla.Controls.Add(caja, 0, 1); tabla.Controls.Add(botones, 0, 2); form.Controls.Add(tabla);
        form.AcceptButton = aceptar; form.CancelButton = cancelar;
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
        var panel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Dock = DockStyle.Fill };
        panel.Controls.Add(EstiloVisual.Boton("Cancelar", DialogResult.Cancel)); panel.Controls.Add(EstiloVisual.Boton("Crear / Formatear disco", DialogResult.OK));
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

    /// <param name="configuracionDisco">
    /// Geometría del disco local virtual. El sistema operativo no define estos valores
    /// (eso ocurre en Configuración → Disco Local); aquí solo los consulta para mostrar
    /// las capacidades físicas y la política de distribución lógica que administra.
    /// </param>
    public ConfiguracionSistemaForm(ConfiguracionSistema actual, ConfiguracionDisco configuracionDisco)
    {
        Text = "Sistema operativo simulado"; Size = new Size(540, 680); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; EstiloVisual.Aplicar(this);
        nombre.Text = actual.Nombre; usuario.Text = actual.Usuario;
        var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, AutoScroll = true };
        void Agregar(string texto, Control control) { int fila = tabla.Controls.Count / 2; tabla.Controls.Add(new Label { Text = texto, AutoSize = true, Anchor = AnchorStyles.Left }, 0, fila); control.Dock = DockStyle.Fill; tabla.Controls.Add(control, 1, fila); }
        void Encabezado(string texto) { int fila = tabla.Controls.Count / 2; var l = new Label { Text = texto, AutoSize = true, Padding = new Padding(0, fila == 0 ? 0 : 10, 0, 2), Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) }; tabla.Controls.Add(l, 0, fila); tabla.SetColumnSpan(l, 2); tabla.Controls.Add(new Label(), 1, fila); }
        void Info(string etiqueta, string valor) => Agregar(etiqueta, new Label { Text = valor, AutoSize = true, ForeColor = EstiloVisual.TextoSecundario });

        Encabezado("IDENTIDAD DEL SISTEMA");
        Agregar("Nombre del SO", nombre); Agregar("Usuario", usuario);

        Encabezado("CAPACIDADES FÍSICAS");
        Info("Capacidad total del volumen", Formato(configuracionDisco.CapacidadBytes));
        Info("Tamaño de cluster", Formato(configuracionDisco.TamanoClusterBytes));
        Info("Clusters de datos disponibles", configuracionDisco.CantidadClusters.ToString("N0"));
        Info("Bytes por sector", configuracionDisco.BytesPorSector.ToString("N0"));

        Encabezado("DISTRIBUCIÓN LÓGICA DE ARCHIVOS");
        Info("Algoritmo de asignación", configuracionDisco.AlgoritmoAsignacion);
        Info("Cluster raíz (C:\\)", configuracionDisco.ClusterRaiz.ToString());
        Info("Copias de la FAT", configuracionDisco.NumeroDeFat.ToString());

        var nota = new Label { Text = "Estos valores físicos se definen en Configuración → Disco Local.\nAquí el sistema operativo virtual los consulta para administrar\ncómo se distribuyen lógicamente los archivos sobre el volumen.", AutoSize = true, ForeColor = Color.DimGray, Font = new Font(SystemFonts.DefaultFont.FontFamily, 8F, FontStyle.Italic), Padding = new Padding(0, 8, 0, 4) };
        { int fila = tabla.Controls.Count / 2; tabla.Controls.Add(nota, 0, fila); tabla.SetColumnSpan(nota, 2); tabla.Controls.Add(new Label(), 1, fila); }

        string[] info = ["Sistema de archivos: FAT32", "Modo: Simulación", "Unidad raíz: C:\\", "Archivos soportados: TXT"];
        foreach (var linea in info) { int fila = tabla.Controls.Count / 2; var l = new Label { Text = linea, AutoSize = true, ForeColor = Color.DimGray }; tabla.Controls.Add(l, 0, fila); tabla.SetColumnSpan(l, 2); tabla.Controls.Add(new Label(), 1, fila); }

        var botones = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Dock = DockStyle.Fill }; botones.Controls.Add(EstiloVisual.Boton("Cancelar", DialogResult.Cancel)); botones.Controls.Add(EstiloVisual.Boton("Guardar", DialogResult.OK));
        { int fila = tabla.Controls.Count / 2; tabla.Controls.Add(botones, 0, fila); tabla.SetColumnSpan(botones, 2); }
        Controls.Add(tabla);
    }

    private static string Formato(long bytes) => bytes < 1024 ? $"{bytes} B" : bytes < 1048576 ? $"{bytes / 1024d:0.##} KiB" : $"{bytes / 1048576d:0.##} MiB";
}
