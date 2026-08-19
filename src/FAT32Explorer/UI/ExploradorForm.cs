using FAT32Explorer.Modelo;
using FAT32Explorer.Persistencia;

namespace FAT32Explorer.UI;

public sealed class ExploradorForm : Form
{
    private DiscoVirtual disco;
    private readonly AlmacenamientoJson almacenamiento;
    private DirectorioVirtual actual;
    private readonly TreeView arbol = new() { Dock = DockStyle.Fill, HideSelection = false, Scrollable = true };
    private readonly ListView contenido = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true };
    private readonly DataGridView fat = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly FlowLayoutPanel mapa = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true, BackColor = Color.White };
    private readonly ToolStripStatusLabel estado = new();
    private readonly ToolStripLabel ruta = new() { BackColor = Color.White, Margin = new Padding(12, 3, 6, 3), Padding = new Padding(10, 4, 10, 4), AutoSize = false, Width = 390, TextAlign = ContentAlignment.MiddleLeft, AutoToolTip = true };
    private readonly ToolStripButton atras = new() { ToolTipText = "Atrás", Image = IconosVisuales.Flecha(false), DisplayStyle = ToolStripItemDisplayStyle.Image };
    private readonly ToolStripButton subir = new() { ToolTipText = "Directorio superior", Image = IconosVisuales.Flecha(true), DisplayStyle = ToolStripItemDisplayStyle.Image };
    private readonly Label detalles = new() { Dock = DockStyle.Fill, Padding = new Padding(14), ForeColor = Color.FromArgb(45, 50, 58) };
    private readonly ProgressBar capacidad = new() { Dock = DockStyle.Top, Height = 8, Maximum = 100 };
    private readonly Label capacidadTexto = new() { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 7, 0, 5), ForeColor = Color.DimGray };
    private readonly Panel panelFat = new() { Dock = DockStyle.Fill };
    private readonly Button alternarFat = new() { Dock = DockStyle.Top, Height = 36, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), Text = "▼  Visualización FAT32" };
    private SplitContainer principal = null!;
    private int alturaFat = 310;
    private bool fatExpandida = true;
    private readonly Stack<DirectorioVirtual> historial = new();
    private bool navegando;
    private readonly ContextMenuStrip contextoVacio = new();
    private readonly ContextMenuStrip contextoArchivo = new();
    private readonly ContextMenuStrip contextoCarpeta = new();
    private readonly ToolTip ayudas = new();

    public ExploradorForm(DiscoVirtual disco, AlmacenamientoJson almacenamiento)
    {
        this.disco = disco; this.almacenamiento = almacenamiento; actual = disco.Raiz;
        Text = "FAT32Explorer — Explorador y simulador FAT32"; WindowState = FormWindowState.Maximized; MinimumSize = new Size(1050, 680); EstiloVisual.Aplicar(this);
        contenido.Columns.Add("Nombre", 260); contenido.Columns.Add("Tipo", 110); contenido.Columns.Add("Tamaño lógico", 110); contenido.Columns.Add("Primer cluster", 110); contenido.Columns.Add("Modificado", 170);
        var iconos = new ImageList { ColorDepth = ColorDepth.Depth32Bit, ImageSize = new Size(20, 20) }; iconos.Images.Add("disco", IconosVisuales.Disco()); iconos.Images.Add("carpeta", IconosVisuales.Carpeta()); iconos.Images.Add("txt", IconosVisuales.Texto()); arbol.ImageList = iconos; contenido.SmallImageList = iconos;
        fat.Columns.Add("numero", "Cluster / Entrada"); fat.Columns.Add("estado", "Estado"); fat.Columns.Add("siguiente", "Valor FAT / Siguiente"); fat.Columns.Add("propietario", "Propietario"); fat.Columns.Add("tipo", "Tipo");
        fat.AllowUserToDeleteRows = false; fat.AllowUserToResizeRows = false; fat.SelectionMode = DataGridViewSelectionMode.FullRowSelect; fat.RowTemplate.Height = 28; fat.ColumnHeadersHeight = 34; fat.BackgroundColor = Color.White; fat.BorderStyle = BorderStyle.None;
        var menu = CrearMenu(); MainMenuStrip = menu;
        var barra = CrearBarraHerramientas();
        var superior = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1, SplitterWidth = 4, BackColor = EstiloVisual.Borde };
        var izquierda = new Panel { Dock = DockStyle.Fill, BackColor = EstiloVisual.Superficie, Padding = new Padding(10) };
        var tituloArbol = new Label { Text = "DIRECTORIOS", Dock = DockStyle.Top, AutoSize = true, MinimumSize = new Size(0, 34), Font = new Font("Segoe UI Semibold", 9), ForeColor = EstiloVisual.TextoSecundario, Padding = new Padding(4, 8, 0, 0) };
        izquierda.Controls.Add(arbol); izquierda.Controls.Add(tituloArbol); superior.Panel1.Controls.Add(izquierda);
        var derecha = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel2, SplitterWidth = 1 };
        derecha.Panel1.Controls.Add(contenido);
        var rapidas = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = EstiloVisual.Superficie, Padding = new Padding(14), ColumnCount = 1, RowCount = 5 };
        rapidas.RowStyles.Add(new RowStyle(SizeType.AutoSize)); rapidas.RowStyles.Add(new RowStyle(SizeType.Absolute, 12)); rapidas.RowStyles.Add(new RowStyle(SizeType.AutoSize)); rapidas.RowStyles.Add(new RowStyle(SizeType.Absolute, 1)); rapidas.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rapidas.Controls.Add(new Label { Text = "DISCO FAT32", AutoSize = true, MinimumSize = new Size(0, 28), Font = new Font("Segoe UI Semibold", 9), ForeColor = EstiloVisual.TextoSecundario }, 0, 0);
        capacidad.Dock = DockStyle.Fill; rapidas.Controls.Add(capacidad, 0, 1); rapidas.Controls.Add(capacidadTexto, 0, 2); rapidas.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = EstiloVisual.Borde, Margin = new Padding(0, 6, 0, 6) }, 0, 3); rapidas.Controls.Add(detalles, 0, 4);
        derecha.Panel2.Controls.Add(rapidas); superior.Panel2.Controls.Add(derecha);
        var vistasFat = new TabControl { Dock = DockStyle.Fill }; var tabTabla = new TabPage("Tabla FAT") { BackColor = Color.White }; var tabMapa = new TabPage("Mapa de clusters") { BackColor = Color.White };
        tabTabla.Controls.Add(fat); tabMapa.Controls.Add(mapa); tabMapa.Controls.Add(CrearLeyenda()); vistasFat.TabPages.AddRange([tabTabla, tabMapa]);
        panelFat.Controls.Add(vistasFat); panelFat.Controls.Add(alternarFat);
        principal = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, Panel2MinSize = 36, SplitterWidth = 4 };
        principal.Panel1.Controls.Add(superior); principal.Panel2.Controls.Add(panelFat);
        var status = new StatusStrip(); status.Items.Add(estado);
        Controls.Add(principal); Controls.Add(status); Controls.Add(barra); Controls.Add(menu);
        Shown += (_, _) => { AjustarDivisor(superior, 260); superior.Panel1MinSize = 220; superior.Panel2MinSize = 500; AjustarDivisor(derecha, derecha.Width - 280); derecha.Panel1MinSize = 320; derecha.Panel2MinSize = 240; AjustarFatInicial(); principal.Panel1MinSize = 220; };
        superior.SizeChanged += (_, _) => AjustarDivisor(superior, Math.Min(260, superior.Width - superior.Panel2MinSize - superior.SplitterWidth));
        derecha.SizeChanged += (_, _) => AjustarDivisor(derecha, derecha.Width - 280);
        contenido.MouseDoubleClick += (_, _) => AbrirSeleccion(); contenido.SelectedIndexChanged += (_, _) => RefrescarFat();
        arbol.AfterSelect += (_, e) => { if (!navegando && e.Node?.Tag is DirectorioVirtual d) NavegarA(d); };
        atras.Click += (_, _) => IrAtras(); subir.Click += (_, _) => Subir(); alternarFat.Click += (_, _) => AlternarFat();
        CrearContextos(); contenido.MouseDown += SeleccionarParaContexto; mapa.Padding = new Padding(4);
        FormClosing += (_, _) => GuardarSilenciosamente();
        RefrescarTodo();
    }

    private ToolStrip CrearBarraHerramientas()
    {
        var barra = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, AutoSize = true, MinimumSize = new Size(0, 42), Padding = new Padding(10, 5, 10, 5), ImageScalingSize = new Size(20, 20), BackColor = Color.FromArgb(249, 250, 252) };
        var actualizar = new ToolStripButton { ToolTipText = "Actualizar", Image = IconosVisuales.Actualizar(), DisplayStyle = ToolStripItemDisplayStyle.Image, Margin = new Padding(4, 1, 4, 2) }; actualizar.Click += (_, _) => RefrescarTodo();
        var carpeta = new ToolStripButton("Carpeta", IconosVisuales.Carpeta()) { ToolTipText = "Nueva carpeta", Margin = new Padding(6, 1, 6, 2) }; carpeta.Click += (_, _) => NuevaCarpeta();
        var archivo = new ToolStripButton("TXT", IconosVisuales.Texto()) { ToolTipText = "Nuevo archivo TXT", Margin = new Padding(6, 1, 6, 2) }; archivo.Click += (_, _) => NuevoArchivo();
        var propiedades = new ToolStripButton("Propiedades", IconosVisuales.Informacion()) { ToolTipText = "Propiedades del elemento seleccionado", Margin = new Padding(6, 1, 6, 2) }; propiedades.Click += (_, _) => PropiedadesSeleccion();
        barra.Items.AddRange([atras, subir, actualizar, new ToolStripSeparator(), carpeta, archivo, new ToolStripSeparator(), propiedades, ruta]);
        barra.SizeChanged += (_, _) => ruta.Width = Math.Max(140, barra.DisplayRectangle.Right - ruta.Bounds.Left - 8);
        return barra;
    }

    private static Control CrearLeyenda()
    {
        var leyenda = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(8, 5, 0, 0), BackColor = Color.White };
        foreach (var (texto, color) in new[] { ("Reservado", Color.SlateGray), ("Libre", Color.LightGreen), ("Ocupado", Color.LightSkyBlue), ("Seleccionado", Color.Gold) })
            leyenda.Controls.Add(new Label { Text = $"■ {texto}", AutoSize = true, ForeColor = color == Color.LightGreen ? Color.SeaGreen : color == Color.LightSkyBlue ? Color.SteelBlue : color == Color.Gold ? Color.DarkGoldenrod : color, Margin = new Padding(8, 2, 8, 2) });
        return leyenda;
    }

    private static void AjustarDivisor(SplitContainer divisor, int deseado)
    {
        if (divisor.Width <= 0 || divisor.Height <= 0) return;
        int total = divisor.Orientation == Orientation.Vertical ? divisor.ClientSize.Width : divisor.ClientSize.Height;
        int minimo = divisor.Panel1MinSize;
        int maximo = total - divisor.Panel2MinSize - divisor.SplitterWidth;
        if (maximo >= minimo) divisor.SplitterDistance = Math.Clamp(deseado, minimo, maximo);
    }

    private void AjustarFatInicial() => AjustarDivisor(principal, principal.Height - alturaFat);

    private void AlternarFat()
    {
        if (principal.ClientSize.Height <= 0) return;
        fatExpandida = !fatExpandida;
        panelFat.Controls[0].Visible = fatExpandida;
        if (fatExpandida) AjustarDivisor(principal, principal.Height - alturaFat);
        else { alturaFat = Math.Max(180, principal.Panel2.Height); AjustarDivisor(principal, principal.Height - alternarFat.Height); }
        alternarFat.Text = fatExpandida ? "▼  Visualización FAT32" : "▶  Visualización FAT32";
    }

    private MenuStrip CrearMenu()
    {
        var menu = new MenuStrip();
        var archivo = new ToolStripMenuItem("Archivo"); archivo.DropDownItems.Add("Guardar disco", null, (_, _) => Guardar()); archivo.DropDownItems.Add("Salir", null, (_, _) => Close());
        var discoMenu = new ToolStripMenuItem("Configuración"); discoMenu.DropDownItems.Add("Sistema operativo virtual...", null, (_, _) => ConfigurarSistema()); discoMenu.DropDownItems.Add("Disco local virtual...", null, (_, _) => ConfigurarDisco());
        var operaciones = new ToolStripMenuItem("Operaciones");
        operaciones.DropDownItems.Add("Nuevo archivo TXT", null, (_, _) => NuevoArchivo()); operaciones.DropDownItems.Add("Nueva carpeta", null, (_, _) => NuevaCarpeta());
        operaciones.DropDownItems.Add("Abrir / Editar", null, (_, _) => AbrirSeleccion()); operaciones.DropDownItems.Add("Renombrar", null, (_, _) => RenombrarSeleccion()); operaciones.DropDownItems.Add("Mover a...", null, (_, _) => MoverSeleccion()); operaciones.DropDownItems.Add("Eliminar", null, (_, _) => EliminarSeleccion()); operaciones.DropDownItems.Add("Propiedades", null, (_, _) => PropiedadesSeleccion());
        var ver = new ToolStripMenuItem("Ver"); ver.DropDownItems.Add("Actualizar", null, (_, _) => RefrescarTodo()); ver.DropDownItems.Add("Estructura FAT32", null, (_, _) => new EstructuraFat32Form(disco, Seleccionado()?.Tag).ShowDialog(this)); ver.DropDownItems.Add("Mostrar / ocultar visualización FAT32", null, (_, _) => AlternarFat()); ver.DropDownItems.Add("Información FAT32", null, (_, _) => MessageBox.Show(this, "0 y 1: RESERVED · clusters de datos desde 2\nFREE: disponible · EOC: fin de cadena\nLa FAT es la fuente de verdad y First Fit permite fragmentación.", "Información FAT32"));
        var ayuda = new ToolStripMenuItem("Ayuda"); ayuda.DropDownItems.Add("Acerca de FAT32Explorer", null, (_, _) => MessageBox.Show(this, "FAT32Explorer\nSimulador didáctico de sistema de archivos FAT32\n\nSistemas Operativos I\n\nFAT · clusters · directorios · fragmentación · First Fit", "Acerca de FAT32Explorer"));
        menu.Items.AddRange([archivo, discoMenu, operaciones, ver, ayuda]); return menu;
    }

    private void CrearContextos()
    {
        contextoVacio.Items.Add("Nuevo archivo TXT", null, (_, _) => NuevoArchivo()); contextoVacio.Items.Add("Nueva carpeta", null, (_, _) => NuevaCarpeta()); contextoVacio.Items.Add("Actualizar", null, (_, _) => RefrescarTodo());
        contextoArchivo.Items.Add("Abrir / Editar", null, (_, _) => AbrirSeleccion()); contextoArchivo.Items.Add(new ToolStripSeparator()); contextoArchivo.Items.Add("Renombrar", null, (_, _) => RenombrarSeleccion()); contextoArchivo.Items.Add("Mover a...", null, (_, _) => MoverSeleccion()); contextoArchivo.Items.Add("Eliminar", null, (_, _) => EliminarSeleccion()); contextoArchivo.Items.Add(new ToolStripSeparator()); contextoArchivo.Items.Add("Propiedades", null, (_, _) => PropiedadesSeleccion());
        contextoCarpeta.Items.Add("Abrir", null, (_, _) => AbrirSeleccion()); contextoCarpeta.Items.Add(new ToolStripSeparator()); contextoCarpeta.Items.Add("Nuevo archivo TXT aquí", null, (_, _) => CrearDentro(true)); contextoCarpeta.Items.Add("Nueva carpeta aquí", null, (_, _) => CrearDentro(false)); contextoCarpeta.Items.Add(new ToolStripSeparator()); contextoCarpeta.Items.Add("Renombrar", null, (_, _) => RenombrarSeleccion()); contextoCarpeta.Items.Add("Mover a...", null, (_, _) => MoverSeleccion()); contextoCarpeta.Items.Add("Eliminar", null, (_, _) => EliminarSeleccion()); contextoCarpeta.Items.Add(new ToolStripSeparator()); contextoCarpeta.Items.Add("Propiedades FAT32", null, (_, _) => PropiedadesSeleccion());
    }

    private void SeleccionarParaContexto(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;
        var item = contenido.GetItemAt(e.X, e.Y); contenido.SelectedItems.Clear();
        if (item is not null) item.Selected = true;
        (item?.Tag switch { ArchivoVirtual => contextoArchivo, DirectorioVirtual => contextoCarpeta, _ => contextoVacio }).Show(contenido, e.Location);
    }

    private void CrearDentro(bool archivo) { if (Seleccionado()?.Tag is not DirectorioVirtual d) return; var previo = actual; actual = d; if (archivo) NuevoArchivo(); else NuevaCarpeta(); actual = previo; RefrescarTodo(); }

    private void NuevoArchivo()
    {
        using var editor = new EditorTextoForm(null, disco.Configuracion.TamanoClusterBytes);
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
        if (elemento is DirectorioVirtual d) { NavegarA(d); return; }
        var archivo = (ArchivoVirtual)elemento;
        using var editor = new EditorTextoForm(archivo, disco.Configuracion.TamanoClusterBytes);
        if (editor.ShowDialog(this) == DialogResult.OK)
        {
            Ejecutar(() => disco.EditarArchivo(actual, archivo, editor.Nombre, editor.Contenido));
        }
    }

    private void RenombrarSeleccion()
    {
        if (Seleccionado()?.Tag is not object elemento) { MessageBox.Show(this, "Seleccione un archivo o carpeta."); return; }
        string inicial = elemento is ArchivoVirtual a ? a.Nombre : ((DirectorioVirtual)elemento).Nombre;
        string? nombre = Entrada.Pedir(this, "Renombrar", "Nuevo nombre:", inicial);
        if (nombre is null) return;
        Ejecutar(() => { if (elemento is ArchivoVirtual archivo) disco.RenombrarArchivo(actual, archivo, nombre); else disco.RenombrarDirectorio(actual, (DirectorioVirtual)elemento, nombre); });
    }

    private void MoverSeleccion()
    {
        if (Seleccionado()?.Tag is not object elemento) { MessageBox.Show(this, "Seleccione un archivo o carpeta."); return; }
        var destinos = disco.TodosLosDirectorios().Where(d => !ReferenceEquals(d, actual)).ToList();
        if (destinos.Count == 0) { MessageBox.Show(this, "No hay otro directorio disponible."); return; }
        using var dialogo = new Form { Text = "Mover a...", ClientSize = new Size(420, 145), MinimumSize = new Size(380, 180), StartPosition = FormStartPosition.CenterParent };
        EstiloVisual.Aplicar(dialogo);
        var combo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = destinos, DisplayMember = "Nombre" };
        var mover = EstiloVisual.Boton("Mover", DialogResult.OK); var cancelar = EstiloVisual.Boton("Cancelar", DialogResult.Cancel);
        var botones = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false }; botones.Controls.Add(cancelar); botones.Controls.Add(mover);
        var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(15), ColumnCount = 1, RowCount = 3 }; tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); tabla.Controls.Add(new Label { Text = "Directorio de destino", AutoSize = true, Margin = new Padding(0, 0, 0, 6) }, 0, 0); tabla.Controls.Add(combo, 0, 1); tabla.Controls.Add(botones, 0, 2); dialogo.Controls.Add(tabla); dialogo.AcceptButton = mover; dialogo.CancelButton = cancelar;
        if (dialogo.ShowDialog(this) == DialogResult.OK && combo.SelectedItem is DirectorioVirtual destino)
            Ejecutar(() => { if (elemento is ArchivoVirtual archivo) disco.MoverArchivo(actual, destino, archivo); else disco.MoverDirectorio(actual, destino, (DirectorioVirtual)elemento); });
    }

    private void EliminarSeleccion()
    {
        if (Seleccionado()?.Tag is not object elemento) return;
        bool recursivo = elemento is DirectorioVirtual d && (d.Archivos.Count > 0 || d.Directorios.Count > 0);
        string mensaje = elemento is DirectorioVirtual carpeta
            ? recursivo ? $"La carpeta \"{carpeta.Nombre}\" contiene:\n\n{disco.ArchivosTotales(carpeta)} archivos\n{disco.SubdirectoriosTotales(carpeta)} subcarpetas\n\nEliminarla también eliminará todo su contenido y liberará sus clusters.\n\n¿Desea continuar?" : $"¿Eliminar \"{carpeta.Nombre}\"?"
            : $"¿Eliminar \"{((ArchivoVirtual)elemento).Nombre}\"?";
        if (MessageBox.Show(this, mensaje, "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        Ejecutar(() => { if (elemento is ArchivoVirtual a) disco.EliminarArchivo(actual, a); else disco.EliminarDirectorio(actual, (DirectorioVirtual)elemento, recursivo); });
    }

    private void PropiedadesSeleccion()
    {
        if (Seleccionado()?.Tag is ArchivoVirtual a) MessageBox.Show(this, $"Archivo: {a.Nombre}\nTipo: Archivo TXT FAT32\n\nUbicación lógica\n{RutaElemento(a.Nombre)}\n\nUbicación física simulada\nPrimer cluster: {a.PrimerCluster?.ToString() ?? "—"}\nCadena FAT: {disco.Cadena(a)}\nFragmentación: {(disco.EstaFragmentado(a) ? "Fragmentado" : "Contiguo")}\n\nTamaño lógico: {a.TamanoBytes:N0} bytes\nEspacio físico: {disco.EspacioFisico(a):N0} bytes\nDesperdicio interno: {disco.DesperdicioInterno(a):N0} bytes\nCreado: {a.Creado:g}\nModificado: {a.Modificado:g}", "Propiedades FAT32");
        else if (Seleccionado()?.Tag is DirectorioVirtual d) MessageBox.Show(this, $"Nombre: {d.Nombre}\nRuta: {RutaDe(d)}\nTipo: Directorio FAT32\nCreado: {d.Creado:g}\nModificado: {d.Modificado:g}\n\nPrimer cluster: {d.PrimerCluster}\nCadena FAT: {disco.Cadena(d)}\n\nTamaño lógico propio: {disco.TamanoLogicoDirectorio(d):N0} bytes\nEspacio físico propio: {disco.EspacioFisicoDirectorio(d):N0} bytes\nArchivos directos: {d.Archivos.Count}\nSubdirectorios directos: {d.Directorios.Count}\nElementos totales: {disco.ArchivosTotales(d) + disco.SubdirectoriosTotales(d)}\nTamaño total del contenido: {disco.TamanoContenido(d):N0} bytes\nEspacio físico total incluyendo contenido: {disco.EspacioTotalFisico(d):N0} bytes", "Propiedades FAT32");
    }

    private void ConfigurarDisco()
    {
        if ((disco.TodosLosArchivos().Any() || disco.Raiz.Directorios.Any()) && MessageBox.Show(this, "ADVERTENCIA:\n\nAplicar esta configuración formateará el disco virtual y eliminará todos sus archivos y carpetas.\n\n¿Continuar?", "Formatear disco virtual", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        using var form = new ConfiguracionForm(disco.Configuracion);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        try { var sistema = disco.SistemaOperativo; disco = DiscoVirtual.Crear(form.Configuracion); disco.SistemaOperativo = sistema; actual = disco.Raiz; historial.Clear(); Guardar(); RefrescarTodo(); } catch (Exception ex) { MostrarError(ex); }
    }

    private void ConfigurarSistema()
    {
        using var form = new ConfiguracionSistemaForm(disco.SistemaOperativo, disco.Configuracion);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        if (form.Nombre.Length == 0 || form.Usuario.Length == 0) { MessageBox.Show(this, "Nombre del SO y usuario son obligatorios."); return; }
        disco.SistemaOperativo.Nombre = form.Nombre; disco.SistemaOperativo.Usuario = form.Usuario;
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
        TreeNode CrearNodo(DirectorioVirtual d) { var n = new TreeNode(d.Nombre) { Tag = d, ImageKey = ReferenceEquals(d, disco.Raiz) ? "disco" : "carpeta", SelectedImageKey = ReferenceEquals(d, disco.Raiz) ? "disco" : "carpeta" }; foreach (var h in d.Directorios.OrderBy(x => x.Nombre)) n.Nodes.Add(CrearNodo(h)); return n; }
    }
    private void SeleccionarNodo(DirectorioVirtual directorio) { foreach (TreeNode n in arbol.Nodes) if (Buscar(n)) break; bool Buscar(TreeNode n) { if (ReferenceEquals(n.Tag, directorio)) { arbol.SelectedNode = n; return true; } return n.Nodes.Cast<TreeNode>().Any(Buscar); } }
    private void RefrescarContenido()
    {
        contenido.BeginUpdate(); contenido.Items.Clear();
        foreach (var d in actual.Directorios.OrderBy(x => x.Nombre)) contenido.Items.Add(new ListViewItem([d.Nombre, "Directorio FAT32", $"{disco.TamanoLogicoDirectorio(d):N0} bytes", d.PrimerCluster?.ToString() ?? "—", d.Creado.ToString("g")], "carpeta") { Tag = d });
        foreach (var a in actual.Archivos.OrderBy(x => x.Nombre)) contenido.Items.Add(new ListViewItem([a.Nombre, "Archivo TXT", $"{a.TamanoBytes:N0} bytes", a.PrimerCluster?.ToString() ?? "—", a.Modificado.ToString("g")], "txt") { Tag = a });
        contenido.EndUpdate(); ruta.Text = RutaDe(actual).Replace("\\", "  ›  ").Replace("C:  ›  ", "C:\\  ›  ").TrimEnd('›', ' '); atras.Enabled = historial.Count > 0; subir.Enabled = !ReferenceEquals(actual, disco.Raiz);
        int porcentaje = disco.Configuracion.CapacidadBytes == 0 ? 0 : (int)Math.Round(disco.EspacioUsado * 100d / disco.Configuracion.CapacidadBytes); capacidad.Value = Math.Clamp(porcentaje, 0, 100); capacidadTexto.Text = $"{porcentaje}% usado\n{Formato(disco.EspacioUsado)} de {Formato(disco.Configuracion.CapacidadBytes)}";
        estado.Text = $"{disco.SistemaOperativo.Nombre} · {disco.SistemaOperativo.Usuario} | {disco.Configuracion.Nombre} | Sector: {Formato(disco.Configuracion.BytesPorSector)} | Cluster: {Formato(disco.Configuracion.TamanoClusterBytes)} | Datos: {Formato(disco.Configuracion.CapacidadBytes)} | Usado: {Formato(disco.EspacioUsado)} | Libre: {Formato(disco.EspacioLibre)}";
    }
    private string RutaDe(DirectorioVirtual objetivo) { if (ReferenceEquals(objetivo, disco.Raiz)) return "C:\\"; var partes = new List<string>(); Buscar(disco.Raiz); return "C:\\" + string.Join("\\", partes.AsEnumerable().Reverse().Where(p => p != "C:")); bool Buscar(DirectorioVirtual d) { if (ReferenceEquals(d, objetivo)) { partes.Add(d.Nombre.TrimEnd('\\')); return true; } foreach (var h in d.Directorios) if (Buscar(h)) { partes.Add(d.Nombre.TrimEnd('\\')); return true; } return false; } }
    private string RutaElemento(string nombre) => RutaDe(actual).TrimEnd('\\') + "\\" + nombre;
    private void NavegarA(DirectorioVirtual destino, bool registrar = true) { if (ReferenceEquals(actual, destino)) return; if (registrar) historial.Push(actual); actual = destino; navegando = true; SeleccionarNodo(destino); navegando = false; RefrescarContenido(); RefrescarFat(); }
    private void IrAtras() { if (historial.Count > 0) NavegarA(historial.Pop(), false); }
    private void Subir() { var padre = disco.TodosLosDirectorios().FirstOrDefault(d => d.Directorios.Contains(actual)); if (padre is not null) NavegarA(padre); }
    private void RefrescarFat()
    {
        object? elementoSeleccionado = Seleccionado()?.Tag ?? arbol.SelectedNode?.Tag;
        string? resaltado = elementoSeleccionado switch { ArchivoVirtual a => a.Id, DirectorioVirtual d => d.Id, _ => null };
        var propietarios = disco.TodosLosArchivos().Select(a => new { a.Id, a.Nombre, Tipo = "Archivo" }).Concat(disco.TodosLosDirectorios().Select(d => new { d.Id, d.Nombre, Tipo = "Directorio" })).ToDictionary(x => x.Id);
        fat.Rows.Clear(); mapa.Controls.Clear(); ActualizarDetalles(elementoSeleccionado);
        foreach (var c in disco.Clusters)
        {
            int entrada = disco.Fat.Entradas[c.Numero]; string siguiente = entrada switch { TablaFat.Eof => "EOC", TablaFat.Libre => "FREE", TablaFat.Reservado => "RESERVED", _ => entrada.ToString() };
            var propietario = c.PropietarioId is null ? null : propietarios.GetValueOrDefault(c.PropietarioId);
            string nombrePropietario = propietario?.Nombre ?? "—"; string tipo = propietario?.Tipo ?? "—";
            bool seleccionado = resaltado is not null && c.PropietarioId == resaltado;
            Color colorEstado = c.Estado switch { EstadoCluster.Reservado => Color.SlateGray, EstadoCluster.Libre => Color.LightGreen, _ => Color.LightSkyBlue };
            int fila = fat.Rows.Add(c.Numero, c.Estado, siguiente, nombrePropietario, tipo);
            fat.Rows[fila].DefaultCellStyle.BackColor = seleccionado ? Color.Gold : colorEstado;
            var etiqueta = new Label { Text = c.Estado == EstadoCluster.Reservado ? $"FAT[{c.Numero}]\nRESERVED" : c.Estado == EstadoCluster.Libre ? $"{c.Numero}\nFREE" : $"{c.Numero}\n{nombrePropietario}", Size = new Size(98, 54), Margin = new Padding(4), TextAlign = ContentAlignment.MiddleCenter, BorderStyle = BorderStyle.FixedSingle, BackColor = seleccionado ? Color.Gold : colorEstado };
            ayudas.SetToolTip(etiqueta, $"Cluster: {c.Numero}\nEstado: {c.Estado}\nPropietario: {nombrePropietario}\nTipo: {tipo}\nFAT: {siguiente}");
            mapa.Controls.Add(etiqueta);
        }
    }
    private void ActualizarDetalles(object? elemento)
    {
        detalles.Text = elemento switch
        {
            ArchivoVirtual a => $"{a.Nombre}\nArchivo TXT\n\nTamaño:  {Formato(a.TamanoBytes)}\nPrimer cluster:  {a.PrimerCluster?.ToString() ?? "—"}\nClusters:  {disco.ClustersUtilizados(a)}\nFragmentación:  {(disco.EstaFragmentado(a) ? "Fragmentado" : "Contiguo")}",
            DirectorioVirtual d => $"{d.Nombre}\nDirectorio FAT32\n\nPrimer cluster:  {d.PrimerCluster}\nElementos:  {d.Archivos.Count + d.Directorios.Count}\nEspacio propio:  {Formato(disco.EspacioFisicoDirectorio(d))}",
            _ => "Seleccione un elemento para ver sus propiedades rápidas."
        };
    }
    private static string Formato(long bytes) => bytes < 1024 ? $"{bytes} B" : bytes < 1048576 ? $"{bytes / 1024d:0.##} KiB" : $"{bytes / 1048576d:0.##} MiB";
}
