using SpreadsheetApp.Core.Model;

namespace SpreadsheetApp.Core.Parsing;

public class Lexer
{
    public List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        var i = 0;

        while (i < text.Length)
        {
            var c = text[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (char.IsAsciiDigit(c))
            {
                var start = i;
                while (i < text.Length && char.IsAsciiDigit(text[i]))
                {
                    i++;
                }
                tokens.Add(new Token(TokenType.Number, text[start..i], start));
                continue;
            }

            if (char.IsAsciiLetter(c))
            {
                var start = i;
                while (i < text.Length && char.IsAsciiLetterOrDigit(text[i]))
                {
                    i++;
                }
                tokens.Add(ClassifyWord(text[start..i], start));
                continue;
            }

            switch (c)
            {
                case '+':
                case '-':
                case '*':
                case '/':
                case '^':
                    tokens.Add(new Token(TokenType.Operator, c.ToString(), i));
                    break;
                case '(':
                    tokens.Add(new Token(TokenType.LParen, "(", i));
                    break;
                case ')':
                    tokens.Add(new Token(TokenType.RParen, ")", i));
                    break;
                default:
                    throw new ExpressionError($"Недопустимий символ «{c}»", i);
            }
            i++;
        }

        tokens.Add(new Token(TokenType.End, string.Empty, text.Length));
        return tokens;
    }

    private static Token ClassifyWord(string word, int position)
    {
        var lower = word.ToLowerInvariant();

        if (lower is "mod" or "div")
        {
            return new Token(TokenType.Operator, lower, position);
        }

        if (lower is "inc" or "dec")
        {
            return new Token(TokenType.Function, lower, position);
        }

        if (CellAddress.TryParse(word, out _))
        {
            return new Token(TokenType.CellRef, word.ToUpperInvariant(), position);
        }

        throw new ExpressionError($"Невідомий ідентифікатор «{word}»", position);
    }
}
