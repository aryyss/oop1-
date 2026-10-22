using System.Numerics;
using SpreadsheetApp.Core.Parsing;

namespace SpreadsheetApp.Core.Model;

public class Spreadsheet : ICellProvider
{
    public const int MaxRows = 1_000_000;
    public const int MaxColumns = 18_278;

    private readonly Dictionary<CellAddress, Cell> _cells = new();
    private readonly DependencyGraph _graph = new();
    private readonly Parser _parser = new();

    public int RowCount { get; private set; }
    public int ColumnCount { get; private set; }

    public IReadOnlyDictionary<CellAddress, Cell> Cells => _cells;

    public Spreadsheet(int rowCount = 10, int columnCount = 10)
    {
        if (rowCount < 1 || rowCount > MaxRows)
        {
            throw new ArgumentOutOfRangeException(nameof(rowCount));
        }

        if (columnCount < 1 || columnCount > MaxColumns)
        {
            throw new ArgumentOutOfRangeException(nameof(columnCount));
        }

        RowCount = rowCount;
        ColumnCount = columnCount;
    }

    public bool Contains(CellAddress address) =>
        address.Column >= 0 && address.Column < ColumnCount &&
        address.Row >= 0 && address.Row < RowCount;

    public Cell GetCell(CellAddress address)
    {
        EnsureInRange(address);
        return _cells.TryGetValue(address, out var cell) ? cell : new Cell();
    }

    public void SetExpression(CellAddress address, string text)
    {
        EnsureInRange(address);
        text = (text ?? string.Empty).Trim();

        if (text.Length == 0)
        {
            _cells.Remove(address);
            _graph.Update(address, Array.Empty<CellAddress>());
            Recalculate(new[] { address });
            return;
        }

        var tree = _parser.Parse(text);

        var previous = _graph.GetReferences(address);
        _graph.Update(address, tree.GetReferences());
        if (_graph.HasCycle(address))
        {
            _graph.Update(address, previous);
            throw new ExpressionError("Циклічне посилання між клітинками", 0);
        }

        if (!_cells.TryGetValue(address, out var cell))
        {
            cell = new Cell();
            _cells[address] = cell;
        }

        cell.Expression = text;
        cell.Tree = tree;
        Recalculate(new[] { address });
    }

    public void AddRow()
    {
        if (RowCount >= MaxRows)
        {
            throw new InvalidOperationException("Досягнуто максимальну кількість рядків");
        }

        var row = RowCount;
        RowCount++;
        Recalculate(Enumerable.Range(0, ColumnCount).Select(column => new CellAddress(column, row)).ToList());
    }

    public void AddColumn()
    {
        if (ColumnCount >= MaxColumns)
        {
            throw new InvalidOperationException("Досягнуто максимальну кількість стовпчиків");
        }

        var column = ColumnCount;
        ColumnCount++;
        Recalculate(Enumerable.Range(0, RowCount).Select(row => new CellAddress(column, row)).ToList());
    }

    public void RemoveLastRow()
    {
        if (RowCount == 1)
        {
            throw new InvalidOperationException("Таблиця повинна містити хоча б один рядок");
        }

        var row = RowCount - 1;
        var removed = Enumerable.Range(0, ColumnCount).Select(column => new CellAddress(column, row)).ToList();
        RowCount--;
        DropCells(removed);
    }

    public void RemoveLastColumn()
    {
        if (ColumnCount == 1)
        {
            throw new InvalidOperationException("Таблиця повинна містити хоча б один стовпчик");
        }

        var column = ColumnCount - 1;
        var removed = Enumerable.Range(0, RowCount).Select(row => new CellAddress(column, row)).ToList();
        ColumnCount--;
        DropCells(removed);
    }

    public BigInteger GetValue(CellAddress address)
    {
        if (!Contains(address))
        {
            throw new ExpressionError($"Клітинка {address} знаходиться поза межами таблиці", 0);
        }

        if (!_cells.TryGetValue(address, out var cell))
        {
            return BigInteger.Zero;
        }

        if (cell.Error != null)
        {
            throw new ExpressionError($"Помилка в клітинці {address}: {cell.Error.Message}", 0);
        }

        return cell.Value ?? BigInteger.Zero;
    }

    private void DropCells(List<CellAddress> removed)
    {
        foreach (var address in removed)
        {
            _cells.Remove(address);
            _graph.Update(address, Array.Empty<CellAddress>());
        }
        Recalculate(removed);
    }

    private void Recalculate(IEnumerable<CellAddress> roots)
    {
        foreach (var address in _graph.GetRecalcOrder(roots))
        {
            if (_cells.TryGetValue(address, out var cell))
            {
                Evaluate(cell);
            }
        }
    }

    private void Evaluate(Cell cell)
    {
        if (cell.Tree == null)
        {
            return;
        }

        try
        {
            cell.Value = cell.Tree.Evaluate(this);
            cell.Error = null;
        }
        catch (ExpressionError error)
        {
            cell.Value = null;
            cell.Error = error;
        }
    }

    private void EnsureInRange(CellAddress address)
    {
        if (!Contains(address))
        {
            throw new ArgumentOutOfRangeException(nameof(address), $"Клітинка {address} знаходиться поза межами таблиці");
        }
    }
}
