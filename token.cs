using System;

namespace MiniPl
{
    class Token
    {

        public TokenType type { get; set; }
        public string value { get; set; }
        public int line { get; set; }


        public Token(TokenType type_, string value_, int line_)
        {
            type = type_;
            value = value_;
            line = line_;
        }

        public override string ToString()
        {
            return type + " " + value + " " + line;
        }
    }

    enum TokenType
    {
        LEFT_PAREN, RIGHT_PAREN, LEFT_BRACKET, RIGHT_BRACKET, MINUS, PLUS, COLON, SEMICOLON, SLASH, STAR, MODULO, DOT, COMMA,
        STATEMENT, EQUAL, GREATER, LESS, NOTEQUAL, EQUALLESS, EQUALGREATER, AND, OR, NOT, IDENTIFIER, STRINGTYPE, INTTYPE, BOOLTYPE, REALTYPE,
        IF, THEN, ELSE, OF, WHILE, DO, BEGIN, END, VAR, ARRAY, PROCEDURE, FUNCTION, PROGRAM, ASSERT, RETURN, CALL,
        FALSE, READ, SIZE, TRUE, WRITELN, INT, STRING, REAL, BOOLEAN, EOF,
        PRINT, FOR, IN, DOUBLEDOT
        
    }
}