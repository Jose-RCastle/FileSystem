namespace FAT32Explorer.Modelo;

public sealed class TablaFat
{
    public const int Eof = -1;
    public const int Libre = -2;
    public const int Reservado = -3;
    public List<int> Entradas { get; set; } = [];

    public static TablaFat Crear(int clustersDatos)
    {
        if (clustersDatos <= 0) throw new ArgumentOutOfRangeException(nameof(clustersDatos), "Debe existir al menos un cluster de datos.");
        return new TablaFat { Entradas = Enumerable.Range(0, clustersDatos + 2).Select(i => i < 2 ? Reservado : Libre).ToList() };
    }

    public List<int> BuscarLibres(int cantidad) => Entradas
        .Select((valor, indice) => (valor, indice))
        .Where(x => x.valor == Libre).Take(cantidad).Select(x => x.indice).ToList();

    public void Enlazar(IReadOnlyList<int> cadena)
    {
        for (int i = 0; i < cadena.Count; i++) Entradas[cadena[i]] = i == cadena.Count - 1 ? Eof : cadena[i + 1];
    }

    public IReadOnlyList<int> Recorrer(int? primero)
    {
        if (primero is null) return [];
        var cadena = new List<int>();
        var visitados = new HashSet<int>();
        int actual = primero.Value;
        while (actual != Eof)
        {
            if (actual < 0 || actual >= Entradas.Count || !visitados.Add(actual) || Entradas[actual] is Libre or Reservado)
                throw new InvalidDataException("La cadena FAT está dañada.");
            cadena.Add(actual);
            actual = Entradas[actual];
        }
        return cadena;
    }

    public void Liberar(int? primero)
    {
        foreach (int numero in Recorrer(primero)) Entradas[numero] = Libre;
    }
}
