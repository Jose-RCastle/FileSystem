namespace FAT32Explorer.UI;

internal static class EstiloVisual
{
    public static readonly Color Fondo = Color.FromArgb(246, 248, 251);
    public static readonly Color Superficie = Color.White;
    public static readonly Color Borde = Color.FromArgb(218, 223, 230);
    public static readonly Color TextoSecundario = Color.FromArgb(92, 101, 114);
    public static readonly Color Acento = Color.FromArgb(0, 103, 192);

    public static void Aplicar(Form form)
    {
        form.Font = new Font("Segoe UI", 9F);
        form.BackColor = Fondo;
        form.AutoScaleMode = AutoScaleMode.Dpi;
    }

    public static Label Titulo(string texto) => new()
    {
        Text = texto, AutoSize = true, Font = new Font("Segoe UI Semibold", 11F), ForeColor = Color.FromArgb(32, 36, 42)
    };

    public static Label Secundario(string texto) => new()
    {
        Text = texto, AutoSize = true, ForeColor = TextoSecundario
    };
}
