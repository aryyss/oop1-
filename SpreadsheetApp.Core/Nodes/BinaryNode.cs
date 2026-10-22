using System.Numerics;
using SpreadsheetApp.Core.Model;
using SpreadsheetApp.Core.Parsing;

namespace SpreadsheetApp.Core.Nodes;

public class BinaryNode : Node
{
    private const long MaxResultBits = 10_000_000;

    public string Op { get; }
    public Node Left { get; }
    public Node Right { get; }

    public BinaryNode(string op, Node left, Node right, int position) : base(position)
    {
        Op = op;
        Left = left;
        Right = right;
    }

    public override BigInteger Evaluate(ICellProvider cells)
    {
        var left = Left.Evaluate(cells);
        var right = Right.Evaluate(cells);

        switch (Op)
        {
            case "+":
                return left + right;
            case "-":
                return left - right;
            case "*":
                return left * right;
            case "/":
                EnsureNotZero(right, "Ділення на нуль");
                return BigInteger.Divide(left, right);
            case "div":
                EnsureNotZero(right, "Цілочисельне ділення на нуль");
                return FloorDivide(left, right);
            case "mod":
                EnsureNotZero(right, "Остача від ділення на нуль");
                return FloorModulo(left, right);
            case "^":
                return Power(left, right);
            default:
                throw new InvalidOperationException($"Невідома операція «{Op}»");
        }
    }

    public override IEnumerable<CellAddress> GetReferences() =>
        Left.GetReferences().Concat(Right.GetReferences());

    private void EnsureNotZero(BigInteger divisor, string message)
    {
        if (divisor.IsZero)
        {
            throw new ExpressionError(message, Position);
        }
    }

    private static BigInteger FloorDivide(BigInteger left, BigInteger right)
    {
        var quotient = BigInteger.DivRem(left, right, out var remainder);
        if (!remainder.IsZero && (remainder.Sign < 0) != (right.Sign < 0))
        {
            quotient -= BigInteger.One;
        }
        return quotient;
    }

    private static BigInteger FloorModulo(BigInteger left, BigInteger right)
    {
        var remainder = BigInteger.Remainder(left, right);
        if (!remainder.IsZero && (remainder.Sign < 0) != (right.Sign < 0))
        {
            remainder += right;
        }
        return remainder;
    }

    private BigInteger Power(BigInteger baseValue, BigInteger exponent)
    {
        if (exponent.Sign < 0)
        {
            throw new ExpressionError("Піднесення до від'ємного степеня", Position);
        }

        if (exponent > int.MaxValue || baseValue.GetBitLength() * (long)exponent > MaxResultBits)
        {
            throw new ExpressionError("Результат піднесення до степеня занадто великий", Position);
        }

        return BigInteger.Pow(baseValue, (int)exponent);
    }
}
