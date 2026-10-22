using System.Globalization;
using System.Numerics;
using SpreadsheetApp.Core.Model;
using SpreadsheetApp.Core.Nodes;

namespace SpreadsheetApp.Core.Parsing;

public class Parser
{
    private readonly Lexer _lexer = new();
    private List<Token> _tokens = new();
    private int _index;

    public Node Parse(string text)
    {
        _tokens = _lexer.Tokenize(text);
        _index = 0;

        var node = ParseExpression();

        if (Current.Type != TokenType.End)
        {
            throw new ExpressionError($"Неочікуваний елемент {Describe(Current)}", Current.Position);
        }

        return node;
    }

    private Token Current => _tokens[_index];

    private Token Advance() => _tokens[_index++];

    private bool IsOperator(params string[] operators) =>
        Current.Type == TokenType.Operator && operators.Contains(Current.Text);

    private Node ParseExpression()
    {
        var left = ParseTerm();
        while (IsOperator("+", "-"))
        {
            var op = Advance();
            var right = ParseTerm();
            left = new BinaryNode(op.Text, left, right, op.Position);
        }
        return left;
    }

    private Node ParseTerm()
    {
        var left = ParsePower();
        while (IsOperator("*", "/", "div", "mod"))
        {
            var op = Advance();
            var right = ParsePower();
            left = new BinaryNode(op.Text, left, right, op.Position);
        }
        return left;
    }

    private Node ParsePower()
    {
        var left = ParsePrimary();
        if (IsOperator("^"))
        {
            var op = Advance();
            var right = ParsePower();
            return new BinaryNode(op.Text, left, right, op.Position);
        }
        return left;
    }

    private Node ParsePrimary()
    {
        var token = Current;

        switch (token.Type)
        {
            case TokenType.Number:
                Advance();
                return new NumberNode(BigInteger.Parse(token.Text, CultureInfo.InvariantCulture), token.Position);

            case TokenType.CellRef:
                Advance();
                return new CellRefNode(CellAddress.Parse(token.Text), token.Position);

            case TokenType.Function:
                Advance();
                Expect(TokenType.LParen, $"Очікувалась «(» після «{token.Text}»");
                var argument = ParseExpression();
                Expect(TokenType.RParen, "Очікувалась «)»");
                return new FunctionNode(token.Text, argument, token.Position);

            case TokenType.LParen:
                Advance();
                var inner = ParseExpression();
                Expect(TokenType.RParen, "Очікувалась «)»");
                return inner;

            default:
                throw new ExpressionError(
                    $"Очікувалось число, посилання на клітинку, функція inc/dec або «(», знайдено {Describe(token)}",
                    token.Position);
        }
    }

    private void Expect(TokenType type, string message)
    {
        if (Current.Type != type)
        {
            throw new ExpressionError($"{message}, знайдено {Describe(Current)}", Current.Position);
        }
        Advance();
    }

    private static string Describe(Token token) =>
        token.Type == TokenType.End ? "кінець виразу" : $"«{token.Text}»";
}
