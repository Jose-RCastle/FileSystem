using System.Drawing.Drawing2D;

namespace FAT32Explorer.UI;

internal static class IconosVisuales
{
    private static readonly Color Azul = Color.FromArgb(40, 116, 166);
    private static readonly Color Amarillo = Color.FromArgb(242, 190, 64);

    public static Bitmap Disco() => Dibujar(g =>
    {
        using var relleno = new SolidBrush(Color.FromArgb(224, 234, 242));
        using var borde = new Pen(Azul, 1.5f);
        g.FillRoundedRectangle(relleno, new RectangleF(2, 4, 16, 12), 2);
        g.DrawRoundedRectangle(borde, new RectangleF(2, 4, 16, 12), 2);
        g.FillEllipse(Brushes.MediumSeaGreen, 14, 11, 2, 2);
    });

    public static Bitmap Carpeta() => Dibujar(g =>
    {
        using var borde = new Pen(Color.FromArgb(190, 132, 22), 1.2f);
        using var relleno = new SolidBrush(Amarillo);
        var forma = new GraphicsPath();
        forma.AddPolygon([new Point(2, 5), new Point(8, 5), new Point(10, 7), new Point(18, 7), new Point(18, 16), new Point(2, 16)]);
        g.FillPath(relleno, forma); g.DrawPath(borde, forma);
    });

    public static Bitmap Texto() => Dibujar(g =>
    {
        using var borde = new Pen(Azul, 1.3f); using var papel = new SolidBrush(Color.White);
        g.FillRectangle(papel, 4, 2, 12, 16); g.DrawRectangle(borde, 4, 2, 12, 16);
        using var linea = new Pen(Azul, 1); g.DrawLine(linea, 7, 8, 13, 8); g.DrawLine(linea, 7, 11, 13, 11); g.DrawLine(linea, 7, 14, 11, 14);
    });

    public static Bitmap Flecha(bool arriba) => Dibujar(g =>
    {
        using var p = new Pen(Azul, 2) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        if (arriba) { g.DrawLine(p, 10, 16, 10, 5); g.DrawLine(p, 10, 5, 5, 10); g.DrawLine(p, 10, 5, 15, 10); }
        else { g.DrawLine(p, 16, 10, 5, 10); g.DrawLine(p, 5, 10, 10, 5); g.DrawLine(p, 5, 10, 10, 15); }
    });

    public static Bitmap Actualizar() => Dibujar(g => { using var p = new Pen(Azul, 2); g.DrawArc(p, 4, 4, 12, 12, 35, 285); g.DrawLine(p, 15, 4, 15, 9); g.DrawLine(p, 15, 4, 10, 4); });
    public static Bitmap Informacion() => Dibujar(g => { using var p = new Pen(Azul, 1.5f); g.DrawEllipse(p, 3, 3, 14, 14); using var f = new Font("Segoe UI", 10, FontStyle.Bold, GraphicsUnit.Pixel); using var b = new SolidBrush(Azul); g.DrawString("i", f, b, 8, 5); });

    private static Bitmap Dibujar(Action<Graphics> accion)
    {
        var bitmap = new Bitmap(20, 20); using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality; accion(g); return bitmap;
    }

    private static void FillRoundedRectangle(this Graphics g, Brush b, RectangleF r, float radio) { using var p = Redondeado(r, radio); g.FillPath(b, p); }
    private static void DrawRoundedRectangle(this Graphics g, Pen pen, RectangleF r, float radio) { using var p = Redondeado(r, radio); g.DrawPath(pen, p); }
    private static GraphicsPath Redondeado(RectangleF r, float radio) { var p = new GraphicsPath(); float d = radio * 2; p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right-d, r.Y, d, d, 270, 90); p.AddArc(r.Right-d, r.Bottom-d, d, d, 0, 90); p.AddArc(r.X, r.Bottom-d, d, d, 90, 90); p.CloseFigure(); return p; }
}
