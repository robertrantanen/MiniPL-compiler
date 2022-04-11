using System;
using System.Collections.Generic;

namespace MiniPl
{
    class Scanner
    {

        static private int start = 0;
        static private int current = 0;
        static private int end = 0;
        static private int line = 1;
        static private string text = "";
        List<Token> tokens;
        Dictionary<string, TokenType> keywords;

        public List<Token> scan(string file)
        {
            start = 0;
            current = 0;
            line = 1;
            text = file;
            end = text.Length;
            tokens = new List<Token>();

            keywords = new Dictionary<string, TokenType>();
            keywords["var"] = TokenType.VAR;
            keywords["end"] = TokenType.END;
            keywords["do"] = TokenType.DO;
            keywords["read"] = TokenType.READ;
            keywords["assert"] = TokenType.ASSERT;
            keywords["bool"] = TokenType.BOOLTYPE;
            keywords["string"] = TokenType.STRINGTYPE;
            keywords["integer"] = TokenType.INTTYPE;
            keywords["and"] = TokenType.AND;
            keywords["not"] = TokenType.NOT;
            keywords["or"] = TokenType.OR;
            keywords["if"] = TokenType.IF;
            keywords["then"] = TokenType.THEN;
            keywords["else"] = TokenType.ELSE;
            keywords["of"] = TokenType.OF;
            keywords["while"] = TokenType.WHILE;
            keywords["begin"] = TokenType.BEGIN;
            keywords["array"] = TokenType.ARRAY;
            keywords["procedure"] = TokenType.PROCEDURE;
            keywords["function"] = TokenType.FUNCTION;
            keywords["program"] = TokenType.PROGRAM;
            keywords["return"] = TokenType.RETURN;
            keywords["false"] = TokenType.FALSE;
            keywords["real"] = TokenType.REALTYPE;
            keywords["size"] = TokenType.SIZE;
            keywords["true"] = TokenType.TRUE;
            keywords["writeln"] = TokenType.WRITELN;

            //Console.WriteLine(text);
            while (start < end)
            {
                char c = text[start];
                //Console.WriteLine(c);
                switch (c)
                {
                    case ';':
                        tokens.Add(new Token(TokenType.SEMICOLON, c.ToString(), line));
                        break;
                    case '+':
                        tokens.Add(new Token(TokenType.PLUS, c.ToString(), line));
                        break;
                    case '-':
                        tokens.Add(new Token(TokenType.MINUS, c.ToString(), line));
                        break;
                    case '(':
                        tokens.Add(new Token(TokenType.LEFT_PAREN, c.ToString(), line));
                        break;
                    case ')':
                        tokens.Add(new Token(TokenType.RIGHT_PAREN, c.ToString(), line));
                        break;
                    case '[':
                        tokens.Add(new Token(TokenType.LEFT_BRACKET, c.ToString(), line));
                        break;
                    case ']':
                        tokens.Add(new Token(TokenType.RIGHT_BRACKET, c.ToString(), line));
                        break;
                    case '%':
                        tokens.Add(new Token(TokenType.MODULO, c.ToString(), line));
                        break;
                    case '=':
                        tokens.Add(new Token(TokenType.EQUAL, c.ToString(), line));
                        break;
                    case '/':
                        tokens.Add(new Token(TokenType.SLASH, c.ToString(), line));
                        break;
                    case '*':
                        tokens.Add(new Token(TokenType.STAR, c.ToString(), line));
                        break;
                    case '.':
                        tokens.Add(new Token(TokenType.DOT, c.ToString(), line));
                        break;
                    case ',':
                        tokens.Add(new Token(TokenType.COMMA, c.ToString(), line));
                        break;
                    case '<':
                        if (peek(start).Equals('='))
                        {
                            tokens.Add(new Token(TokenType.EQUALLESS, text.Substring(start, 2), line));
                            start++;
                        }
                        else if (peek(start).Equals('>'))
                        {
                            tokens.Add(new Token(TokenType.NOTEQUAL, text.Substring(start, 2), line));
                            start++;
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.LESS, c.ToString(), line));
                        }
                        break;
                    case '>':
                        if (peek(start).Equals('='))
                        {
                            tokens.Add(new Token(TokenType.EQUALGREATER, text.Substring(start, 2), line));
                            start++;
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.GREATER, c.ToString(), line));
                        }
                        break;
                    case ':':
                        if (peek(start).Equals('='))
                        {
                            tokens.Add(new Token(TokenType.STATEMENT, text.Substring(start, 2), line));
                            start++;
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.COLON, c.ToString(), line));
                        }
                        break;
                    case '{':
                        if (peek(start).Equals('*'))
                        {
                            start++;
                            skipMultiComment();
                        }
                        break;
                    case '\"':
                        tokens.Add(new Token(TokenType.STRING, scanString(), line));
                        break;
                    case ' ':
                    case '\r':
                    case '\t':
                        break;
                    case '\n':
                        line++;
                        break;
                    default:
                        if (isDigit(c))
                        {
                            scanNumber();
                        }
                        else if (isAlphabetic(c))
                        {
                            string val = scanIdentifier();
                            if (keywords.ContainsKey(val))
                            {
                                TokenType type = keywords[val];
                                tokens.Add(new Token(type, val, line));
                            }
                            else
                            {
                                tokens.Add(new Token(TokenType.IDENTIFIER, val, line));
                            }
                        }
                        else
                        {
                            Error e = new Error("LEXICAL ERROR: invalid token " + c, line);
                            Console.WriteLine(e);
                        }
                        break;
                }



                start++;
            }
            tokens.Add(new Token(TokenType.EOF, "EOF", line));
            return tokens;

        }

        private Char peek(int i)
        {
            if (i < end)
            {
                return text[i + 1];
            }
            else
            {
                Error e = new Error("LEXICAL ERROR: end of file", line);
                Console.WriteLine(e);
                return '\0';
            }
        }

        private bool isDigit(char c)
        {
            return (c >= '0' & c <= '9');
        }

        private bool isAlphabetic(char c)
        {
            return (c >= 'a' & c <= 'z') ||
                   (c >= 'A' & c <= 'Z') ||
                    c == '_';
        }

        private bool isAlphabeticOrNumeric(char c)
        {
            return isAlphabetic(c) || isDigit(c);
        }

        private string scanString()
        {
            string s = "";
            current = start;
            while (true)
            {
                current++;
                if (current == end)
                {
                    Error e = new Error("LEXICAL ERROR: unclosed string", line);
                    Console.WriteLine(e);
                    break;
                }
                else if (text[current].Equals('\"'))
                {
                    start = current;
                    break;
                }
                else if (text[current].Equals('\\'))
                {
                    char next = peek(current);
                    if (next.Equals('\"'))
                    {
                        s = s + '\"';
                        current++;
                    }
                    else if (next.Equals('\''))
                    {
                        s = s + '\'';
                        current++;
                    }
                    else if (next.Equals('\\'))
                    {
                        s = s + '\\';
                        current++;
                    }
                    else if (next.Equals('n'))
                    {
                        s = s + Environment.NewLine;
                        current++;
                    }
                }
                else
                {
                    s = s + text[current];
                }

            }
            return s;
        }

        private void scanNumber()
        {
            string s = "";
            current = start;
            bool isReal = false;
            while (current < end)
            {
                if (isDigit(text[current]))
                {
                    s = s + text[current];
                }
                else if (text[current] == '.')
                {
                    if (!isReal)
                    {
                        s = s + text[current];
                        isReal = true;
                    }
                    else
                    {
                        Error e = new Error("LEXICAL ERROR: too many dots in real number", line);
                        Console.WriteLine(e);
                    }
                }
                else
                {
                    start = current - 1;
                    break;
                }
                current++;
            }
            if (isReal)
            {
                tokens.Add(new Token(TokenType.REAL, s, line));
            }
            else
            {
                tokens.Add(new Token(TokenType.INT, s, line));
            }
        }

        private string scanIdentifier()
        {
            string s = "";
            current = start;
            while (current < end)
            {
                if (isAlphabeticOrNumeric(text[current]))
                {
                    s = s + text[current];
                }
                else
                {
                    start = current - 1;
                    break;
                }
                current++;
            }
            return s;
        }

        private void skipComment()
        {
            current = start;
            while (current < end - 1)
            {
                current++;
                char c = text[current];
                if (c.Equals('\n'))
                {
                    line++;
                    start = current;
                    break;
                }
            }
        }

        private void skipMultiComment()
        {
            current = start;
            int tempLine = line;
            while (current < end - 1)
            {
                current++;
                if (current == end)
                {
                    Error e = new Error("LEXICAL ERROR: unclosed comment", line);
                    Console.WriteLine(e);
                    break;
                }
                char c = text[current];
                if (c.Equals('\n'))
                {
                    tempLine++;
                }
                if (c.Equals('*'))
                {
                    if (peek(current).Equals('}'))
                    {
                        current++;
                        start = current;
                        line = tempLine;
                        break;
                    }
                }
            }
        }
    }
}