using FAT32Explorer.Modelo;

namespace FAT32Explorer.UI;

/// <summary>Representación conceptual: no contiene BPB, FSInfo ni sectores binarios.</summary>
internal sealed class EstructuraFat32Form : Form
{
    public EstructuraFat32Form(DiscoVirtual disco, object? seleccionado)
    {
        Text = "Estructura del volumen FAT32 simulado"; Size = new Size(1050, 680); StartPosition = FormStartPosition.CenterParent;
        var titulo = new Label { Text = "VOLUMEN FAT32 SIMULADO", Dock = DockStyle.Top, Height = 48, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(SystemFonts.DefaultFont.FontFamily, 16, FontStyle.Bold) };
        var regiones = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(12) };
        regiones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31)); regiones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31)); regiones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        regiones.Controls.Add(CrearRegion("Área reservada / Boot", Boot(disco)), 0, 0);
        regiones.Controls.Add(CrearRegion("FAT", Fat(disco, seleccionado)), 1, 0);
        regiones.Controls.Add(CrearRegion("Área de datos", Datos(disco, seleccionado)), 2, 0);
        var flujo = new Label { Dock = DockStyle.Bottom, Height = 65, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.AliceBlue, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold), Text = "Entrada del directorio  ↓  PrimerCluster  ↓  FAT  ↓  cadena de clusters  ↓  datos" };
        Controls.Add(regiones); Controls.Add(flujo); Controls.Add(titulo);
    }

    private static GroupBox CrearRegion(string titulo, string texto)
    {
        var caja = new GroupBox { Text = titulo, Dock = DockStyle.Fill, Padding = new Padding(10), Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) };
        caja.Controls.Add(new TextBox { Text = texto, Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.White, Font = new Font("Consolas", 10) });
        return caja;
    }

    private static string Boot(DiscoVirtual d) => $"""
        Boot Sector / BPB conceptual

        Contiene parámetros que describen
        cómo está organizado el volumen FAT32.

        Sistema: FAT32 simulado
        Bytes por sector: {d.Configuracion.BytesPorSector}
        Sectores por cluster: {d.Configuracion.SectoresPorCluster}
        Tamaño de cluster: {d.Configuracion.TamanoClusterBytes} B
        Sectores reservados: {d.Configuracion.SectoresReservados}
        Número de FAT: {d.Configuracion.NumeroDeFat}
        Cluster inicial del root: {d.Configuracion.ClusterRaiz}
        Clusters de datos: {d.Configuracion.CantidadClusters}

        FSInfo — representación conceptual
        Libres: {d.ClustersLibres}
        Ocupados: {d.ClustersOcupados}
        Primer libre sugerido: {d.PrimerClusterLibreSugerido?.ToString() ?? "—"}

        [Simulación didáctica — no ejecutable]
        """;

    private static string Fat(DiscoVirtual d, object? seleccionado)
    {
        string cadena = seleccionado switch { ArchivoVirtual a => $"{a.Nombre}\nPrimer cluster: {a.PrimerCluster?.ToString() ?? "—"}\nCadena: {d.Cadena(a)}", DirectorioVirtual x => $"{x.Nombre}\nPrimer cluster: {x.PrimerCluster}\nCadena: {d.Cadena(x)}", _ => "Seleccione un elemento en el explorador para seguir su cadena." };
        return $"""
        FAT[0] → RESERVED
        FAT[1] → RESERVED

        Primer cluster de datos: 2
        Clusters gestionados: {d.Configuracion.CantidadClusters}
        Algoritmo: First Fit
        Copias configuradas: {d.Configuracion.NumeroDeFat}

        Una FAT activa es la fuente de verdad.
        FAT 2, si se configura, es un espejo
        conceptual sin recuperación automática.

        {cadena}
        """;
    }

    private static string Datos(DiscoVirtual d, object? seleccionado)
    {
        var nombres = d.TodosLosArchivos().Select(a => (a.Id, a.Nombre, Tipo: "Archivo"))
            .Concat(d.TodosLosDirectorios().Select(x => (x.Id, x.Nombre, Tipo: "Directorio"))).ToDictionary(x => x.Id);
        string? id = seleccionado switch { ArchivoVirtual a => a.Id, DirectorioVirtual x => x.Id, _ => null };
        return string.Join(Environment.NewLine, d.Clusters.Skip(2).Select(c =>
        {
            string marca = c.PropietarioId == id ? "▶ " : "  ";
            if (c.Estado == EstadoCluster.Libre) return $"{marca}Cluster {c.Numero} → FREE";
            var p = c.PropietarioId is null ? default : nombres.GetValueOrDefault(c.PropietarioId);
            return $"{marca}Cluster {c.Numero} → {p.Nombre ?? "?"}  {p.Tipo ?? ""}";
        }));
    }
}
