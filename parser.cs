using System;
using System.Collections.Generic;

namespace MiniPl
{
    class Parser
    {
        static private List<Token> tokens;
        static private int current = 0;

        static private bool errors = false;

        static private Ast ast;

        static private Node root;

        static private List<TokenType> operators = new List<TokenType>() { TokenType.PLUS, TokenType.MINUS, TokenType.STAR, TokenType.SLASH, TokenType.MODULO, TokenType.AND, TokenType.OR, TokenType.EQUAL, TokenType.LESS, TokenType.GREATER, TokenType.EQUALGREATER, TokenType.EQUALLESS, TokenType.NOTEQUAL };

        public Parser(List<Token> tokens_)
        {
            tokens = tokens_;
        }

        private void match(params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (check(type))
                {
                    current++;
                    return;
                }
            }
            Error e = new Error("SYNTAX ERROR: excepted token type " + types[0] + " but was " + tokens[current].type, tokens[current].line);
            Console.WriteLine(e);
            errors = true;
        }

        private Node matchAddNode(Node parent, params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (check(type))
                {
                    Node node = ast.add(tokens[current], parent);
                    current++;
                    return node;
                }
            }
            Error e = new Error("SYNTAX ERROR: excepted token type " + types[0] + " but was " + tokens[current].type, tokens[current].line);
            Console.WriteLine(e);
            errors = true;
            return null;
        }

        private Node matchAddNextNodeWithoutAdvancing(Node parent, params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (tokens[current + 1].type == type)
                {
                    Node node = ast.add(tokens[current + 1], parent);
                    return node;
                }
            }
            Error e = new Error("SYNTAX ERROR: excepted token type " + types[0] + " but was " + tokens[current].type, tokens[current].line);
            Console.WriteLine(e);
            errors = true;
            return null;
        }


        private bool check(TokenType type)
        {
            return tokens[current].type == type;
        }

        private TokenType currentToken()
        {
            return tokens[current].type;
        }

        private TokenType peek()
        {
            return tokens[current + 1].type;
        }

        public Ast parse()
        {
            current = 0;
            errors = false;
            ast = new Ast();
            root = new Node(new Token(TokenType.EOF, "root", 0));
            ast.root = root;
            program(root);
            return ast;
        }

        private void program(Node parent)
        {
            match(TokenType.PROGRAM);
            Node program = matchAddNode(parent, TokenType.IDENTIFIER);
            match(TokenType.SEMICOLON);
            while ((check(TokenType.PROCEDURE) | check(TokenType.FUNCTION)))
            {
                if (check(TokenType.PROCEDURE))
                {
                    procedure(program);
                }
                else if (check(TokenType.FUNCTION))
                {
                    function(program);
                }
            }
            block(program);
            match(TokenType.DOT);
        }

        private void procedure(Node parent)
        {
            Node p = matchAddNode(parent, TokenType.PROCEDURE);
            Node id = matchAddNode(p, TokenType.IDENTIFIER);
            match(TokenType.LEFT_PAREN);
            parameters(id);
            match(TokenType.RIGHT_PAREN);
            match(TokenType.SEMICOLON);
            block(id);
            match(TokenType.SEMICOLON);
        }

        private void function(Node parent)
        {
            Node p = matchAddNode(parent, TokenType.FUNCTION);
            Node id = matchAddNode(p, TokenType.IDENTIFIER);
            match(TokenType.LEFT_PAREN);
            parameters(id);
            match(TokenType.RIGHT_PAREN);
            match(TokenType.COLON);
            type(id);
            match(TokenType.SEMICOLON);
            block(id);
            match(TokenType.SEMICOLON);
        }

        private void parameters(Node parent)
        {
            if (check(TokenType.VAR) | check(TokenType.IDENTIFIER))
            {
                Node p = parent;
                if (check(TokenType.VAR))
                {
                    p = matchAddNode(parent, TokenType.VAR);
                }
                Node id = matchAddNode(p, TokenType.IDENTIFIER);
                match(TokenType.COLON);
                type(id);

                while (check(TokenType.COMMA))
                {
                    match(TokenType.COMMA);
                    p = parent;
                    if (check(TokenType.VAR))
                    {
                        p = matchAddNode(parent, TokenType.VAR);
                    }
                    id = matchAddNode(p, TokenType.IDENTIFIER);
                    match(TokenType.COLON);
                    type(id);
                }
            }
        }

        private void type(Node parent)
        {
            if (check(TokenType.ARRAY))
            {
                Node p = matchAddNode(parent, TokenType.ARRAY);
                match(TokenType.LEFT_BRACKET);
                if (!check(TokenType.RIGHT_BRACKET))
                {
                    integer_expression(p);
                }
                match(TokenType.RIGHT_BRACKET);
                match(TokenType.OF);
                matchAddNode(p, TokenType.STRINGTYPE, TokenType.INTTYPE, TokenType.REALTYPE, TokenType.BOOLTYPE);
            }
            else
            {
                matchAddNode(parent, TokenType.STRINGTYPE, TokenType.INTTYPE, TokenType.REALTYPE, TokenType.BOOLTYPE);
            }
        }

        private void block(Node parent)
        {
            Node p = matchAddNode(parent, TokenType.BEGIN);
            statement(p);
            while (check(TokenType.SEMICOLON))
            {
                match(TokenType.SEMICOLON);
                statement(p);
            }
            if (check(TokenType.SEMICOLON))
            {
                match(TokenType.SEMICOLON);
            }
            match(TokenType.END);
        }


        private void statement(Node parent)
        {
            switch (tokens[current].type)
            {
                case TokenType.VAR:
                    var_declaration(parent);
                    return;
                case TokenType.IDENTIFIER:
                    if (peek() == TokenType.LEFT_PAREN)
                    {
                        call_statement(parent);
                    }
                    else
                    {
                        assignment(parent);
                    }
                    return;
                case TokenType.READ:
                    read_statement(parent);
                    return;
                case TokenType.RETURN:
                    return_statement(parent);
                    return;
                case TokenType.WRITELN:
                    writeln(parent);
                    return;
                case TokenType.ASSERT:
                    assert_statement(parent);
                    return;
                case TokenType.BEGIN:
                    block(parent);
                    return;
                case TokenType.WHILE:
                    while_statement(parent);
                    return;
                case TokenType.IF:
                    if_statement(parent);
                    return;
            }
        }

        private void var_declaration(Node parent)
        {
            Node p = matchAddNode(parent, TokenType.VAR);
            Node id = matchAddNode(p, TokenType.IDENTIFIER);
            while (check(TokenType.COMMA))
            {
                match(TokenType.COMMA);
                matchAddNode(p, TokenType.IDENTIFIER);
            }
            match(TokenType.COLON);
            type(p);
        }

        private void assignment(Node parent)
        {
            Node id = variable(parent);
            Node assign = matchAddNode(id, TokenType.STATEMENT);
            expression(assign);
        }

        private Node variable(Node parent)
        {
            Node id = matchAddNode(parent, TokenType.IDENTIFIER);
            if (check(TokenType.LEFT_BRACKET))
            {
                match(TokenType.LEFT_BRACKET);
                integer_expression(id);
                match(TokenType.RIGHT_BRACKET);
            }
            return id;
        }

        private void call_statement(Node parent)
        {
            Node id = matchAddNode(parent, TokenType.IDENTIFIER);
            match(TokenType.LEFT_PAREN);
            arguments(id);
            match(TokenType.RIGHT_PAREN);
        }

        private void arguments(Node parent)
        {
            if (!check(TokenType.RIGHT_PAREN))
            {
                expression(parent);
                while (check(TokenType.COMMA))
                {
                    match(TokenType.COMMA);
                    expression(parent);
                }
            }
        }

        private void return_statement(Node parent)
        {
            Node p = matchAddNode(parent, TokenType.RETURN);
            if (!check(TokenType.SEMICOLON))
            {
                expression(p);
            }
        }

        private void read_statement(Node parent)
        {
            Node p = matchAddNode(parent, TokenType.READ);
            match(TokenType.LEFT_PAREN);
            variable(p);
            while (check(TokenType.COMMA))
            {
                match(TokenType.COMMA);
                variable(p);
            }
            match(TokenType.RIGHT_PAREN);
        }


        private void writeln(Node parent)
        {
            Node p = matchAddNode(parent, TokenType.WRITELN);
            match(TokenType.LEFT_PAREN);
            arguments(p);
            match(TokenType.RIGHT_PAREN);
        }

        private void assert_statement(Node parent)
        {
            Node p = matchAddNode(parent, TokenType.ASSERT);
            match(TokenType.LEFT_BRACKET);
            boolean_expression(p);
            match(TokenType.RIGHT_BRACKET);
        }


        private void if_statement(Node parent)
        {
            Node p = matchAddNode(parent, TokenType.IF);
            boolean_expression(p);
            Node then = matchAddNode(p, TokenType.THEN);
            statement(then);
            if (check(TokenType.ELSE) | peek() == TokenType.ELSE)
            {
                if (check(TokenType.SEMICOLON))
                {
                    match(TokenType.SEMICOLON);
                }
                Node els = matchAddNode(p, TokenType.ELSE);
                statement(els);
            }
        }

        private void while_statement(Node parent)
        {
            Node p = matchAddNode(parent, TokenType.WHILE);
            boolean_expression(p);
            Node do_ = matchAddNode(p, TokenType.DO);
            statement(do_);
        }

        private void integer_expression(Node parent)
        {
            expression(parent);
        }


        private void boolean_expression(Node parent)
        {
            expression(parent);
        }



        private void expression(Node parent)
        {
            if (check(TokenType.MINUS))
            {
                match(TokenType.MINUS);
                tokens[current].value = "-" + tokens[current].value;
            }
            if (check(TokenType.PLUS))
            {
                match(TokenType.PLUS);
            }
            if (check(TokenType.NOT))
            {
                Node n = matchAddNode(parent, TokenType.NOT);
                operand(n);
            }
            if (operators.Contains(peek()))
            {
                binaryExpression(parent);
            }
            else if (check(TokenType.IDENTIFIER))
            {
                operand(parent);
            }
            else
            {
                matchAddNode(parent, TokenType.INT, TokenType.STRING, TokenType.IDENTIFIER, TokenType.REAL, TokenType.BOOLEAN);
            }
        }

        private void binaryExpression(Node parent)
        {
            Node n = matchAddNextNodeWithoutAdvancing(parent, operators.ToArray());
            operand(n);
            current++;
            operand(n);
        }

        private void operand(Node parent)
        {
            if (check(TokenType.LEFT_PAREN))
            {
                match(TokenType.LEFT_PAREN);
                expression(parent);
                match(TokenType.RIGHT_PAREN);
            }
            else if (check(TokenType.IDENTIFIER))
            {
                if (peek() == TokenType.LEFT_PAREN)
                {
                    call_statement(parent);
                }
                else
                {
                    Node p = variable(parent);
                    if (check(TokenType.DOT))
                    {
                        match(TokenType.DOT);
                        matchAddNode(p, TokenType.SIZE);
                    } else if (check(TokenType.LEFT_BRACKET)) {
                        match(TokenType.LEFT_BRACKET);
                        expression(p);
                        match(TokenType.RIGHT_BRACKET);
                    }
                }
            }
            else
            {
                matchAddNode(parent, TokenType.INT, TokenType.STRING, TokenType.IDENTIFIER, TokenType.REAL, TokenType.BOOLEAN);
            }
        }




    }
}