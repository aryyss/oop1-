using System.Numerics;

namespace SpreadsheetApp.Core.Model;

public interface ICellProvider
{
    BigInteger GetValue(CellAddress address);
}
