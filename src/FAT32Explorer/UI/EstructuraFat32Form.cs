using FAT32Explorer.Modelo;

namespace FAT32Explorer.UI;

/// <summary>Representación conceptual: no contiene BPB, FSInfo ni sectores binarios.</summary>
internal sealed class EstructuraFat32Form : Form
{
    public EstructuraFat32Form(DiscoVirtual disco, object? seleccionado)
    {
        Text = "Estructura FAT32 — vista didáctica"; Size = new Size(1120, 720); MinimumSize = new Size(900, 600); StartPosition = FormStartPosition.CenterParent;
        EstiloVisual.Aplicar(this);
        var cabecera = new Panel { Dock = DockStyle.Top, Height = 72, Padding = new Padding(22, 13, 0, 0), BackColor = Color.White };
        cabecera.Controls.Add(new Label { Text = "Estructura del volumen FAT32", Dock = DockStyle.Top, Height = 29, Font = new Font("Segoe UI Semibold", 16), ForeColor = Color.FromArgb(28, 34, 42) });
        cabecera.Controls.Add(new Label { Text = "Simulación didáctica · la información se deriva del modelo actual", Dock = DockStyle.Bottom, Height = 25, ForeColor = EstiloVisual.TextoSecundario });
        var regiones = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, Padding = new Padding(16), BackColor = EstiloVisual.Fondo };
        regiones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); regiones.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30)); regiones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); regiones.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30)); regiones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        regiones.Controls.Add(CrearBoot(disco), 0, 0); regiones.Controls.Add(Flecha("→"), 1, 0); regiones.Controls.Add(CrearFat(disco, seleccionado), 2, 0); regiones.Controls.Add(Flecha("→"), 3, 0); regiones.Controls.Add(CrearDatos(disco, seleccionado), 4, 0);
        var pie = new Panel { Dock = DockStyle.Bottom, Height = 105, Padding = new Padding(20, 12, 20, 12), BackColor = Color.White };
        pie.Controls.Add(new Label { Text = "FLUJO DE ACCESO", Dock = DockStyle.Top, Height = 22, Font = new Font("Segoe UI Semibold", 9), ForeColor = EstiloVisual.TextoSecundario });
        pie.Controls.Add(new Label { Text = "Entrada del directorio   →   PrimerCluster   →   FAT   →   Cadena de clusters   →   Datos", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(232, 243, 253), Font = new Font("Segoe UI Semibold", 11), ForeColor = EstiloVisual.Acento });
        Controls.Add(regiones); Controls.Add(pie); Controls.Add(cabecera);
    }

    private static Label Flecha(string texto) => new() { Text = texto, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 18), ForeColor = EstiloVisual.Acento };
    private static Panel Tarjeta(string titulo, string subtitulo)
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16), Margin = new Padding(3) };
        p.Controls.Add(new Label { Text = subtitulo, Dock = DockStyle.Top, Height = 30, ForeColor = EstiloVisual.TextoSecundario });
        p.Controls.Add(new Label { Text = titulo, Dock = DockStyle.Top, Height = 34, Font = new Font("Segoe UI Semibold", 13), ForeColor = Color.FromArgb(35, 42, 50) });
        return p;
    }
    private static TableLayoutPanel Pares(params (string Etiqueta, string Valor)[] filas)
    {
        var t = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 8, 0, 0) };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62)); t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        foreach (var (etiqueta, valor) in filas) { int r = t.RowCount++; t.RowStyles.Add(new RowStyle(SizeType.Absolute, 27)); t.Controls.Add(new Label { Text = etiqueta, Dock = DockStyle.Fill, ForeColor = EstiloVisual.TextoSecundario }, 0, r); t.Controls.Add(new Label { Text = valor, Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 9) }, 1, r); }
        return t;
    }
    private static Panel CrearBoot(DiscoVirtual d)
    {
        var p = Tarjeta("Área reservada / Boot", "Boot Sector / BPB conceptual");
        var fs = Pares(("Clusters libres", d.ClustersLibres.ToString()), ("Clusters ocupados", d.ClustersOcupados.ToString()), ("Próximo libre sugerido", d.PrimerClusterLibreSugerido?.ToString() ?? "—"));
        var fsCaja = new GroupBox { Text = "FSInfo conceptual · representación conceptual", Dock = DockStyle.Bottom, Height = 125, Padding = new Padding(8) }; fsCaja.Controls.Add(fs);
        p.Controls.Add(fsCaja); p.Controls.Add(Pares(("Bytes por sector", d.Configuracion.BytesPorSector.ToString("N0")), ("Sectores por cluster", d.Configuracion.SectoresPorCluster.ToString()), ("Tamaño cluster", Formato(d.Configuracion.TamanoClusterBytes)), ("Sectores reservados", d.Configuracion.SectoresReservados.ToString()), ("FAT configuradas", d.Configuracion.NumeroDeFat.ToString()), ("Cluster raíz", d.Configuracion.ClusterRaiz.ToString()))); return p;
    }
    private static Panel CrearFat(DiscoVirtual d, object? seleccionado)
    {
        var p = Tarjeta("File Allocation Table", "Fuente de verdad de las cadenas");
        string cadena = seleccionado switch { ArchivoVirtual a => a.PrimerCluster is null ? $"{a.Nombre}\nArchivo vacío · sin cluster" : $"{a.Nombre}\n{d.Cadena(a).Replace(" -> ", " → ")}", DirectorioVirtual x => $"{x.Nombre}\n{d.Cadena(x).Replace(" -> ", " → ")}", _ => "Seleccione un archivo o directorio\npara visualizar su cadena." };
        var cadenaCaja = new GroupBox { Text = "Cadena seleccionada", Dock = DockStyle.Bottom, Height = 115, Padding = new Padding(10) }; cadenaCaja.Controls.Add(new Label { Text = cadena, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = EstiloVisual.Acento, Font = new Font("Segoe UI Semibold", 10) });
        p.Controls.Add(cadenaCaja); p.Controls.Add(Pares(("FAT[0]", "RESERVED"), ("FAT[1]", "RESERVED"), ("Primer cluster de datos", "2"), ("Clusters gestionados", d.Configuracion.CantidadClusters.ToString()), ("Algoritmo", "First Fit"), ("Copias FAT", d.Configuracion.NumeroDeFat.ToString()))); return p;
    }
    private static Panel CrearDatos(DiscoVirtual d, object? seleccionado)
    {
        var p = Tarjeta("Área de datos", "Clusters físicos simulados");
        var lista = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = false, HeaderStyle = ColumnHeaderStyle.Nonclickable };
        lista.Columns.Add("Cluster", 65); lista.Columns.Add("Propietario", 150); lista.Columns.Add("Tipo / estado", 105);
        var nombres = d.TodosLosArchivos().Select(a => (a.Id, a.Nombre, Tipo: "Archivo")).Concat(d.TodosLosDirectorios().Select(x => (x.Id, x.Nombre, Tipo: "Directorio"))).ToDictionary(x => x.Id);
        string? id = seleccionado switch { ArchivoVirtual a => a.Id, DirectorioVirtual x => x.Id, _ => null };
        foreach (var c in d.Clusters.Skip(2)) { var propietario = c.PropietarioId is null ? default : nombres.GetValueOrDefault(c.PropietarioId); var item = new ListViewItem([c.Numero.ToString(), propietario.Nombre ?? "FREE", propietario.Tipo ?? "Libre"]); if (c.PropietarioId == id) item.BackColor = Color.Gold; else if (c.Estado == EstadoCluster.Libre) item.ForeColor = Color.SeaGreen; lista.Items.Add(item); }
        p.Controls.Add(lista); lista.BringToFront(); return p;
    }
    private static string Formato(long bytes) => bytes < 1024 ? $"{bytes} B" : $"{bytes / 1024d:0.##} KiB";
}
