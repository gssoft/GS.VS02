// Core/Model/CellAddress.cs

// CellAddress.cs - адрес ячейки во фрактале
public record CellAddress(int Level, string CellId, CellAddress? Parent = null)
{
    public string FullPath => Parent != null
        ? $"{Parent.FullPath}/{CellId}"
        : CellId;
}
