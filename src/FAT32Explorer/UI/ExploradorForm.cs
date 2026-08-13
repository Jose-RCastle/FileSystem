using FAT32Explorer.Modelo;
using FAT32Explorer.Persistencia;

namespace FAT32Explorer.UI;

public sealed class ExploradorForm : Form
{
    private DiscoVirtual disco;
    private readonly AlmacenamientoJson almacenamiento;
    private DirectorioVirtual actual;
    private readonly TreeView arbol = new() { Dock = DockStyle.Fill, HideSelection = false };
    private readonly ListView contenido = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true };
    private readonly DataGridView fat = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly FlowLayoutPanel mapa = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true };
    private readonly ToolStripStatusLabel estado = new();
    private readonly Label ruta = new() { Dock = DockStyle.Top, Height = 28, BorderStyle = BorderStyle.Fixed3D, Padding = new Padding(5) };

    public ExploradorForm(DiscoVirtual disco, AlmacenamientoJson almacenamiento)
    {
        this.disco = disco; this.almacenamiento = almacenamiento; actual = disco.Raiz;
        Text = "FAT32Explorer — Sistema de archivos virtual"; WindowState = FormWindowState.Maximized; MinimumSize = new Size(950, 650);
        contenido.Columns.Add("Nombre", 260); contenido.Columns.Add("Tipo", 100); contenido.Columns.Add("Tamaño", 110); contenido.Columns.Add("Modificado", 170);
        fat.Columns.Add("numero", "Cluster"); fat.Columns.Add("estado", "Estado"); fat.Columns.Add("siguiente", "Siguiente"); fat.Columns.Add("archivo", "Archivo");
        var menu = CrearMenu(); MainMenuStrip = menu;
        var superior = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 260 };
        superior.Panel1.Controls.Add(arbol); superior.Panel2.Controls.Add(contenido); superior.Panel2.Controls.Add(ruta);
        var inferior = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 215 };
        inferior.Panel1.Controls.Add(fat); inferior.Panel2.Controls.Add(mapa);
        var principal = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 360 };
        principal.Panel1.Controls.Add(superior); principal.Panel2.Controls.Add(inferior);
        var status = new StatusStrip(); status.Items.Add(estado);
        Controls.Add(principal); Controls.Add(status); Controls.Add(menu);
        contenido.MouseDoubleClick += (_, _) => AbrirSeleccion(); contenido.SelectedIndexChanged += (_, _) => RefrescarFat();
        arbol.AfterSelect += (_, e) => { if (e.Node.Tag is DirectorioVirtual d) { actual = d; RefrescarContenido(); } };
        contenido.ContextMenuStrip = CrearContexto(); mapa.Padding = new Padding(4);
        FormClosing += (_, _) => GuardarSilenciosamente();
        RefrescarTodo();
    }

    private MenuStrip CrearMenu()
    {
        var menu = new MenuStrip();
        var archivo = new ToolStripMenuItem("Archivo");
        archivo.DropDownItems.Add("Nuevo TXT", null, (_, _) => NuevoArchivo()); archivo.DropDownItems.Add("Nueva carpeta", null, (_, _) => NuevaCarpeta());
        archivo.DropDownItems.Add("Guardar disco", null, (_, _) => Guardar()); archivo.DropDownItems.Add("Salir", null, (_, _) => Close());
        var discoMenu = new ToolStripMenuItem("Configuración"); discoMenu.DropDownItems.Add("Disco virtual...", null, (_, _) => ConfigurarDisco());
        discoMenu.DropDownItems.Add("Sistema operativo...", null, (_, _) => ConfigurarSistema());
        var ayuda = new ToolStripMenuItem("Ayuda"); ayuda.DropDownItems.Add("Acerca de", null, (_, _) => MessageBox.Show(this, "Simulador didáctico FAT32\n.NET 8 + Windows Forms\nNo modifica archivos ni discos reales.", "FAT32Explorer"));
        menu.Items.AddRange([archivo, discoMenu, ayuda]); return menu;
    }

    private ContextMenuStrip CrearContexto()
    {
        var c = new ContextMenuStrip();
        c.Items.Add("Abrir / Editar", null, (_, _) => AbrirSeleccion()); c.Items.Add("Mover a...", null, (_, _) => MoverSeleccion());
        c.Items.Add("Eliminar", null, (_, _) => EliminarSeleccion()); c.Items.Add("Propiedades / Ver clusters", null, (_, _) => PropiedadesSeleccion());
        c.Items.Add(new ToolStripSeparator()); c.Items.Add("Nuevo archivo TXT", null, (_, _) => NuevoArchivo()); c.Items.Add("Nueva carpeta", null, (_, _) => NuevaCarpeta()); c.Items.Add("Actualizar", null, (_, _) => RefrescarTodo());
        return c;
    }

    private void NuevoArchivo()
    {
        using var editor = new EditorTextoForm(null);
        if (editor.ShowDialog(this) != DialogResult.OK) return;
        Ejecutar(() => disco.CrearArchivo(actual, editor.Nombre, editor.Contenido));
    }

    private void NuevaCarpeta()
    {
        string? nombre = Entrada.Pedir(this, "Nueva carpeta", "Nombre de la carpeta:", "Nueva carpeta");
        if (nombre is not null) Ejecutar(() => disco.CrearDirectorio(actual, nombre));
    }

    private void AbrirSeleccion()
    {
        if (Seleccionado() is not { Tag: object elemento }) return;
        if (elemento is DirectorioVirtual d) { actual = d; SeleccionarNodo(d); RefrescarContenido(); return; }
        var archivo = (ArchivoVirtual)elemento;
        using var editor = new EditorTextoForm(archivo);
        if (editor.ShowDialog(this) == DialogResult.OK)
        {
            if (!editor.Nombre.Equals(archivo.Nombre, StringComparison.OrdinalIgnoreCase)) { MessageBox.Show(this, "El nombre no se cambia al editar. Use mover para cambiar su ubicación."); return; }
            Ejecutar(() => disco.ReemplazarContenido(archivo, editor.Contenido));
        }
    }

    private void MoverSeleccion()
    {
        if (Seleccionado()?.Tag is not ArchivoVirtual archivo) return;
        var destinos = disco.TodosLosDirectorios().Where(d => !ReferenceEquals(d, actual)).ToList();
        if (destinos.Count == 0) { MessageBox.Show(this, "No hay otro directorio disponible."); return; }
        using var dialogo = new Form { Text = "Mover a...", Size = new Size(380, 170), StartPosition = FormStartPosition.CenterParent };
        var combo = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = destinos, DisplayMember = "Nombre" };
        dialogo.Controls.Add(new Button { Text = "Mover", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom }); dialogo.Controls.Add(combo);
        if (dialogo.ShowDialog(this) == DialogResult.OK && combo.SelectedItem is DirectorioVirtual destino) Ejecutar(() => disco.MoverArchivo(actual, destino, archivo));
    }

    private void EliminarSeleccion()
    {
        if (Seleccionado()?.Tag is not object elemento || MessageBox.Show(this, "¿Eliminar el elemento seleccionado?", "Confirmar", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        Ejecutar(() => { if (elemento is ArchivoVirtual a) disco.EliminarArchivo(actual, a); else disco.EliminarDirectorio(actual, (DirectorioVirtual)elemento); });
    }

    private void PropiedadesSeleccion()
    {
        if (Seleccionado()?.Tag is ArchivoVirtual a) MessageBox.Show(this, $"Archivo: {a.Nombre}\nTamaño UTF-8: {a.TamanoBytes} bytes\nPrimer cluster: {a.PrimerCluster?.ToString() ?? "Ninguno"}\nCadena FAT: {disco.Cadena(a)}\nCreado: {a.Creado:g}\nModificado: {a.Modificado:g}", "Propiedades");
        else if (Seleccionado()?.Tag is DirectorioVirtual d) MessageBox.Show(this, $"Carpeta: {d.Nombre}\nSubcarpetas: {d.Directorios.Count}\nArchivos: {d.Archivos.Count}\nCreada: {d.Creado:g}", "Propiedades");
    }

    private void ConfigurarDisco()
    {
        if (disco.TodosLosArchivos().Any() && MessageBox.Show(this, "Crear otro disco eliminará el estado virtual actual. ¿Continuar?", "Advertencia", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        using var form = new ConfiguracionForm(disco.Configuracion);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        try { disco = DiscoVirtual.Crear(form.Configuracion); actual = disco.Raiz; Guardar(); RefrescarTodo(); } catch (Exception ex) { MostrarError(ex); }
    }

    private void ConfigurarSistema()
    {
        string? nombre = Entrada.Pedir(this, "Sistema operativo virtual", "Nombre:", disco.SistemaOperativo.Nombre);
        if (nombre is null) return;
        string? usuario = Entrada.Pedir(this, "Sistema operativo virtual", "Usuario:", disco.SistemaOperativo.Usuario);
        if (usuario is null) return;
        disco.SistemaOperativo.Nombre = nombre.Trim(); disco.SistemaOperativo.Usuario = usuario.Trim();
        GuardarSilenciosamente(); RefrescarContenido();
    }

    private void Ejecutar(Action accion) { try { accion(); GuardarSilenciosamente(); RefrescarTodo(); } catch (Exception ex) { MostrarError(ex); } }
    private void Guardar() { try { almacenamiento.Guardar(disco); MessageBox.Show(this, "Disco virtual guardado."); } catch (Exception ex) { MostrarError(ex); } }
    private void GuardarSilenciosamente() { try { almacenamiento.Guardar(disco); } catch { } }
    private void MostrarError(Exception ex) => MessageBox.Show(this, ex.Message, "No se pudo completar la operación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    private ListViewItem? Seleccionado() => contenido.SelectedItems.Count == 0 ? null : contenido.SelectedItems[0];

    private void RefrescarTodo() { RefrescarArbol(); RefrescarContenido(); RefrescarFat(); }
    private void RefrescarArbol()
    {
        arbol.BeginUpdate(); arbol.Nodes.Clear(); arbol.Nodes.Add(CrearNodo(disco.Raiz)); arbol.ExpandAll(); SeleccionarNodo(actual); arbol.EndUpdate();
        TreeNode CrearNodo(DirectorioVirtual d) { var n = new TreeNode(d.Nombre) { Tag = d }; foreach (var h in d.Directorios.OrderBy(x => x.Nombre)) n.Nodes.Add(CrearNodo(h)); return n; }
    }
    private void SeleccionarNodo(DirectorioVirtual directorio) { foreach (TreeNode n in arbol.Nodes) if (Buscar(n)) break; bool Buscar(TreeNode n) { if (ReferenceEquals(n.Tag, directorio)) { arbol.SelectedNode = n; return true; } return n.Nodes.Cast<TreeNode>().Any(Buscar); } }
    private void RefrescarContenido()
    {
        contenido.BeginUpdate(); contenido.Items.Clear();
        foreach (var d in actual.Directorios.OrderBy(x => x.Nombre)) contenido.Items.Add(new ListViewItem([d.Nombre, "Carpeta", "—", d.Creado.ToString("g")]) { Tag = d });
        foreach (var a in actual.Archivos.OrderBy(x => x.Nombre)) contenido.Items.Add(new ListViewItem([a.Nombre, "Archivo TXT", $"{a.TamanoBytes} bytes", a.Modificado.ToString("g")]) { Tag = a });
        contenido.EndUpdate(); ruta.Text = $"Ubicación: {RutaDe(actual)}";
        estado.Text = $"{disco.SistemaOperativo.Nombre} · {disco.SistemaOperativo.Usuario}  |  {disco.Configuracion.Nombre}  |  Total: {Formato(disco.Configuracion.CapacidadBytes)}  |  Usado (incluye reservados): {Formato(disco.EspacioUsado)}  |  Libre: {Formato(disco.EspacioLibre)}  |  Cluster: {disco.Configuracion.TamanoClusterBytes} bytes";
    }
    private string RutaDe(DirectorioVirtual objetivo) { var partes = new List<string>(); Buscar(disco.Raiz); return string.Join("\\", partes.AsEnumerable().Reverse()); bool Buscar(DirectorioVirtual d) { if (ReferenceEquals(d, objetivo)) { partes.Add(d.Nombre.TrimEnd('\\')); return true; } foreach (var h in d.Directorios) if (Buscar(h)) { partes.Add(d.Nombre.TrimEnd('\\')); return true; } return false; } }
    private void RefrescarFat()
    {
        string? resaltado = Seleccionado()?.Tag is ArchivoVirtual a ? a.Id : null;
        var nombres = disco.TodosLosArchivos().ToDictionary(a => a.Id, a => a.Nombre);
        fat.Rows.Clear(); mapa.Controls.Clear();
        foreach (var c in disco.Clusters)
        {
            int entrada = disco.Fat.Entradas[c.Numero]; string siguiente = entrada switch { TablaFat.Eof => "EOF", TablaFat.Libre or TablaFat.Reservado => "—", _ => entrada.ToString() };
            string archivo = c.ArchivoId is not null && nombres.TryGetValue(c.ArchivoId, out var n) ? n : "—";
            int fila = fat.Rows.Add(c.Numero, c.Estado, siguiente, archivo);
            if (c.ArchivoId == resaltado) fat.Rows[fila].DefaultCellStyle.BackColor = Color.Gold;
            var etiqueta = new Label { Text = c.Estado == EstadoCluster.Reservado ? $"{c.Numero}\nR" : c.Estado == EstadoCluster.Libre ? $"{c.Numero}\nLIBRE" : $"{c.Numero}\n{archivo}", Size = new Size(82, 48), TextAlign = ContentAlignment.MiddleCenter, BorderStyle = BorderStyle.FixedSingle, BackColor = c.ArchivoId == resaltado ? Color.Gold : c.Estado switch { EstadoCluster.Reservado => Color.SlateGray, EstadoCluster.Libre => Color.LightGreen, _ => Color.LightSkyBlue } };
            mapa.Controls.Add(etiqueta);
        }
    }
    private static string Formato(long bytes) => bytes < 1024 ? $"{bytes} B" : bytes < 1048576 ? $"{bytes / 1024d:0.##} KiB" : $"{bytes / 1048576d:0.##} MiB";
}
