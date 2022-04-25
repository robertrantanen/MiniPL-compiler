using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniPl
{

    class Scope
    {

        public Dictionary<string, Element> variables { get; set; }

        public Scope parent { get; set; }

        public Scope(Dictionary<string, Element> variables_, Scope parent_)
        {
            variables = variables_;
            parent = parent_;
        }

        public void add(Node iden, Node typeNode)
        {
            string key = iden.token.value;
            string type = typeNode.token.value;
            if (!variables.ContainsKey(key))
            {
                if (type.Equals("array"))
                {
                    int arrVal = Convert.ToInt32(typeNode.childs[0].token.value);
                    string arrType = typeNode.childs[1].token.value;
                    switch (arrType)
                    {
                        case "integer":
                            variables.Add(key, new Element(new int[arrVal], type));
                            break;
                        case "real":
                            variables.Add(key, new Element(new float[arrVal], type));
                            break;
                        case "string":
                            variables.Add(key, new Element(new string[arrVal], type));
                            break;
                        case "boolean":
                            variables.Add(key, new Element(new bool[arrVal], type));
                            break;
                    }
                }
                else
                {
                    variables.Add(key, new Element(null, type));
                }
            }
            else
            {
                Error e = new Error("SEMANTIC ERROR: variable " + key + " already defined in scope", iden.token.line);
                Console.WriteLine(e);
            }
        }

        public void edit(string key, Object value)
        {
            if (variables.ContainsKey(key))
            {
                if (variables[key].type.Equals("array"))
                {

                }
                else
                {
                    variables[key].value = value;
                    return;
                }
            }
            else if (parent == null)
            {
                Error e = new Error("SEMANTIC ERROR: variable " + key + " not found", 0);
                Console.WriteLine(e);
            }
            else
            {
                parent.edit(key, value);
            }
        }

        public Object get(string key)
        {
            if (variables.ContainsKey(key))
            {
                return variables[key].value;
            }
            else
            {
                parent.get(key);
            }
            return null;
        }

        public void printVariables()
        {
            foreach (KeyValuePair<string, Element> k in variables)
            {
                Console.WriteLine("Key: {0}, Value: {1}, Type: {2}", k.Key, k.Value.value, k.Value.type);
            }
        }

    }

    class CustomFunction
    {

        public List<Node> parameters { get; set; }

        public string type { get; set; }

        public Node node { get; set; }


        public CustomFunction(List<Node> parameters_, string type_, Node node_)
        {
            parameters = parameters_;
            type = type_;
            node = node_;
        }

    }

    class Element
    {

        public Object value { get; set; }
        public string type { get; set; }


        public Element(Object value_, string type_)
        {
            value = value_;
            type = type_;
        }
    }

    class Semantic
    {

        static private Ast ast;
        Dictionary<string, Element> variables;

        Dictionary<string, CustomFunction> functions;

        static private List<TokenType> operators = new List<TokenType>() { TokenType.PLUS, TokenType.MINUS, TokenType.STAR, TokenType.SLASH, TokenType.MODULO, TokenType.AND, TokenType.OR, TokenType.EQUAL, TokenType.LESS, TokenType.GREATER, TokenType.EQUALGREATER, TokenType.EQUALLESS, TokenType.NOTEQUAL };

        public Semantic(Ast ast_)
        {
            ast = ast_;
        }

        public void start()
        {
            variables = new Dictionary<string, Element>();
            functions = new Dictionary<string, CustomFunction>();
            Scope global = new Scope(new Dictionary<string, Element>(), null);
            Node program = ast.root.childs[0];
            foreach (Node node in program.childs)
            {
                startBlock(node, global);
            }
            global.printVariables();
            foreach (KeyValuePair<string, CustomFunction> k in functions)
            {
                List<Node> param = k.Value.parameters;
                Console.WriteLine("Key: {0}, Type: {1}", k.Key, k.Value.type);
                Console.WriteLine("parameters:");
                foreach (Node n in param)
                {
                    Console.WriteLine(n.token.value);
                }
            }
        }

        private void startBlock(Node node, Scope scope)
        {
            switch (node.token.type)
            {
                case TokenType.BEGIN:
                    block(node, scope);
                    return;
                case TokenType.FUNCTION:
                    function_declaration(node, scope);
                    return;
                case TokenType.PROCEDURE:
                    procedure_declaration(node, scope);
                    return;
            }
        }

        private void block(Node node, Scope scope)
        {
            foreach (Node n in node.childs)
            {
                statement(n, scope);
            }
        }

        private void function_declaration(Node node, Scope scope)
        {
            Node name = node.childs[0];
            List<Node> parameters = name.childs.GetRange(0, name.childs.Count - 2);
            Node type = name.childs[name.childs.Count - 2];
            functions.Add(name.token.value, new CustomFunction(parameters, type.token.value, node));
            //Node begin = name.childs[name.childs.Count - 1];
            //block(begin, scope);
        }

        private void procedure_declaration(Node node, Scope scope)
        {
            Node name = node.childs[0];
            List<Node> parameters = name.childs.GetRange(0, name.childs.Count - 1);
            functions.Add(name.token.value, new CustomFunction(parameters, "null", node));
            //Node begin = name.childs[name.childs.Count - 1];
            //block(begin, scope);
        }

        private void statement(Node node, Scope scope)
        {
            switch (node.token.type)
            {
                case TokenType.VAR:
                    defineVariable(node, scope);
                    return;
                case TokenType.IDENTIFIER:
                    if (node.childs[0].token.type == TokenType.STATEMENT)
                    {
                        editVariable(node, scope);
                    }
                    else
                    {
                        call(node, scope);
                    }
                    return;
                case TokenType.READ:
                    read(node, scope);
                    return;
                case TokenType.RETURN:
                    return_statement(node, scope);
                    return;
                case TokenType.WRITELN:
                    print(node, scope);
                    return;
                case TokenType.ASSERT:
                    assert(node, scope);
                    return;
                case TokenType.BEGIN:
                    block(node, scope);
                    return;
                case TokenType.WHILE:
                    while_statement(node, scope);
                    return;
                case TokenType.IF:
                    if_statement(node, scope);
                    return;
            }
        }

        private void return_statement(Node node, Scope scope)
        {

        }

        private void while_statement(Node node, Scope scope)
        {

        }

        private void if_statement(Node node, Scope scope)
        {

        }

        private void call(Node node, Scope scope)
        {

        }

        private void defineVariable(Node node, Scope scope)
        {
            Node type = node.childs[node.childs.Count - 1];
            foreach (int i in Enumerable.Range(0, node.childs.Count - 1))
            {
                Node iden = node.childs[i];
                scope.add(iden, type);
            }
        }

        private void editVariable(Node node, Scope scope)
        {
            if (scope.variables.ContainsKey(node.token.value))
            {
                Element element = scope.variables[node.token.value];
                Node value = node.childs[0].childs[0];
                scope.edit(node.token.value, expression(value, scope));
            }
            else
            {
                Error e = new Error("SEMANTIC ERROR: undeclared variable " + node.token.value, node.token.line);
                Console.WriteLine(e);
            }
        }


        private Object expression(Node node, Scope scope)
        {
            switch (node.token.type)
            {
                case TokenType.INT:
                    return integerOperation(node, scope);
                case TokenType.REAL:
                    return realOperation(node, scope);
                case TokenType.STRING:
                    return stringOperation(node, scope);
                case TokenType.BOOLEAN:
                    return booleanOperation(node, scope);
                default:
                    if (operators.Contains(node.token.type))
                    {
                        Node first = node.childs[0];
                        switch (first.token.type)
                        {
                            case TokenType.INT:
                                return integerOperation(node, scope);
                            case TokenType.REAL:
                                return realOperation(node, scope);
                            case TokenType.STRING:
                                return stringOperation(node, scope);
                            case TokenType.BOOLEAN:
                                return booleanOperation(node, scope);
                        }
                    }
                    return null;
            }
        }


        private bool isArithmeticOperation(TokenType type)
        {
            return (type == TokenType.PLUS | type == TokenType.MINUS | type == TokenType.STAR | type == TokenType.SLASH | type == TokenType.MODULO);
        }

        private int integerOperation(Node node, Scope scope)
        {
            if (node.token.type == TokenType.INT)
            {
                return Convert.ToInt32(node.token.value);
            }
            else if (node.token.type == TokenType.IDENTIFIER)
            {
                try
                {
                    return Convert.ToInt32(scope.get(node.token.value));
                }
                catch (Exception)
                {
                    Error e = new Error("SEMANTIC ERROR: undeclared variable " + node.token.value, node.token.line);
                    Console.WriteLine(e);
                }
            }
            else if (isArithmeticOperation(node.token.type))
            {
                TokenType operation = node.token.type;
                Node left = node.childs[0];
                Node right = node.childs[1];
                int leftint = integerOperation(left, scope);
                int rightint = integerOperation(right, scope);
                if (operation == TokenType.PLUS)
                {
                    return leftint + rightint;
                }
                else if (operation == TokenType.MINUS)
                {
                    return leftint - rightint;
                }
                else if (operation == TokenType.STAR)
                {
                    return leftint * rightint;
                }
                else if (operation == TokenType.SLASH)
                {
                    return leftint / rightint;
                }
                else if (operation == TokenType.MODULO)
                {
                    return leftint % rightint;
                }
            }
            else
            {
                Error e = new Error("SEMANTIC ERROR: invalid type " + node.token.type + " ,expected int", node.token.line);
                Console.WriteLine(e);
            }
            return 0;
        }

        private float realOperation(Node node, Scope scope)
        {
            if (node.token.type == TokenType.REAL)
            {
                return float.Parse(node.token.value);
            }
            else if (node.token.type == TokenType.IDENTIFIER)
            {
                try
                {
                    return float.Parse(Convert.ToString(scope.get(node.token.value)));
                }
                catch (Exception)
                {
                    Error e = new Error("SEMANTIC ERROR: undeclared variable " + node.token.value, node.token.line);
                    Console.WriteLine(e);
                }
            }
            else if (isArithmeticOperation(node.token.type))
            {
                TokenType operation = node.token.type;
                Node left = node.childs[0];
                Node right = node.childs[1];
                float leftint = integerOperation(left, scope);
                float rightint = integerOperation(right, scope);
                if (operation == TokenType.PLUS)
                {
                    return leftint + rightint;
                }
                else if (operation == TokenType.MINUS)
                {
                    return leftint - rightint;
                }
                else if (operation == TokenType.STAR)
                {
                    return leftint * rightint;
                }
                else if (operation == TokenType.SLASH)
                {
                    return leftint / rightint;
                }
            }
            else
            {
                Error e = new Error("SEMANTIC ERROR: invalid type " + node.token.type + " ,expected int", node.token.line);
                Console.WriteLine(e);
            }
            return 0;
        }

        private string stringOperation(Node node, Scope scope)
        {
            if (node.token.type == TokenType.STRING)
            {
                return Convert.ToString(node.token.value);
            }
            else if (node.token.type == TokenType.IDENTIFIER)
            {
                try
                {
                    return Convert.ToString(scope.get(node.token.value));
                }
                catch (Exception)
                {
                    Error e = new Error("SEMANTIC ERROR: undeclared variable " + node.token.value, node.token.line);
                    Console.WriteLine(e);
                }
            }
            else if (node.token.type == TokenType.PLUS)
            {
                Node left = node.childs[0];
                Node right = node.childs[1];
                return stringOperation(left, scope) + stringOperation(right, scope);
            }
            else
            {
                Error e = new Error("SEMANTIC ERROR: invalid type " + node.token.type + " ,excpected string", node.token.line);
                Console.WriteLine(e);
            }
            return "";
        }

        private bool booleanOperation(Node node, Scope scope)
        {
            Node left = null;
            Node right = null;
            if (node.token.type == TokenType.IDENTIFIER)
            {
                try
                {
                    return Convert.ToBoolean(scope.get(node.token.value));
                }
                catch (Exception)
                {
                    Error e = new Error("SEMANTIC ERROR: undeclared variable " + node.token.value, node.token.line);
                    Console.WriteLine(e);
                }
            }
            else if (node.token.type == TokenType.EQUAL)
            {
                left = node.childs[0];
                right = node.childs[1];
                return (expression(left, scope) == expression(right, scope));
            }
            else if (node.token.type == TokenType.LESS)
            {
                Object left2 = expression(node.childs[0], scope);
                Object right2 = expression(node.childs[1], scope);
                if (left2.GetType() == typeof(int))
                {
                    return (int)left2 < (int)right2;
                }
                else if (left2.GetType() == typeof(float))
                {
                    return (float)left2 < (float)right2;
                }
            }
            else if (node.token.type == TokenType.GREATER)
            {
                Object left2 = expression(node.childs[0], scope);
                Object right2 = expression(node.childs[1], scope);
                if (left2.GetType() == typeof(int))
                {
                    return (int)left2 > (int)right2;
                }
                else if (left2.GetType() == typeof(float))
                {
                    return (float)left2 > (float)right2;
                }
            }
            else if (node.token.type == TokenType.EQUALLESS)
            {
                Object left2 = expression(node.childs[0], scope);
                Object right2 = expression(node.childs[1], scope);
                if (left2.GetType() == typeof(int))
                {
                    return (int)left2 <= (int)right2;
                }
                else if (left2.GetType() == typeof(float))
                {
                    return (float)left2 <= (float)right2;
                }
            }
            else if (node.token.type == TokenType.EQUALGREATER)
            {
                Object left2 = expression(node.childs[0], scope);
                Object right2 = expression(node.childs[1], scope);
                if (left2.GetType() == typeof(int))
                {
                    return (int)left2 >= (int)right2;
                }
                else if (left2.GetType() == typeof(float))
                {
                    return (float)left2 >= (float)right2;
                }
            }
            else if (node.token.type == TokenType.NOTEQUAL)
            {
                left = node.childs[0];
                right = node.childs[1];
                return (expression(left, scope) != expression(right, scope));
            }
            else if (node.token.type == TokenType.NOT)
            {
                left = node.childs[0];
                return !booleanOperation(left, scope);
            }
            else if (node.token.type == TokenType.AND)
            {
                left = node.childs[0];
                right = node.childs[1];
                return (booleanOperation(left, scope) && booleanOperation(right, scope));
            }
            else if (node.token.type == TokenType.OR)
            {
                left = node.childs[0];
                right = node.childs[1];
                return (booleanOperation(left, scope) | booleanOperation(right, scope));
            }
            else
            {
                Error e = new Error("SEMANTIC ERROR: invalid type " + node.token.type + " ,expected boolean operation", node.token.line);
                Console.WriteLine(e);
            }
            return false;
        }



        private void print(Node node, Scope scope)
        {
            // Node printable = node.childs[0];
            // if (printable.token.type == TokenType.IDENTIFIER)
            // {
            //     if (variables.ContainsKey(printable.token.value))
            //     {
            //         if (variables[printable.token.value].value == null)
            //         {
            //             Error e = new Error("SEMANTIC ERROR: null variable " + printable.token.value, node.token.line);
            //             Console.WriteLine(e);
            //         }
            //         else
            //         {
            //             Console.Write(variables[printable.token.value].value);
            //         }
            //     }
            //     else
            //     {
            //         Error e = new Error("SEMANTIC ERROR: undeclared variable " + node.token.value, node.token.line);
            //         Console.WriteLine(e);
            //     }
            // }
            // else
            // {
            //     Console.Write(printable.token.value);
            // }
        }

        private void read(Node node, Scope scope)
        {
            // Node readable = node.childs[0];
            // Console.WriteLine();
            // if (readable.token.type == TokenType.IDENTIFIER)
            // {
            //     if (variables.ContainsKey(readable.token.value))
            //     {
            //         Element element = variables[readable.token.value];
            //         if (element.type.Equals("string"))
            //         {
            //             string x = Console.ReadLine();
            //             variables[readable.token.value].value = x;
            //         }
            //         else if (element.type.Equals("int"))
            //         {
            //             try
            //             {
            //                 int x = Convert.ToInt32(Console.ReadLine());
            //                 variables[readable.token.value].value = x;
            //             }
            //             catch
            //             {
            //                 Error e = new Error("SEMANTIC ERROR: expected to read int", node.token.line);
            //                 Console.WriteLine(e);
            //             }

            //         }
            //     }
            //     else
            //     {
            //         Error e = new Error("SEMANTIC ERROR: undeclared variable " + node.token.value, node.token.line);
            //         Console.WriteLine(e);
            //     }
            // }
            // else
            // {
            //     Error e = new Error("SEMANTIC ERROR: expected to read a variable", node.token.line);
            //     Console.WriteLine(e);
            // }
        }


        private void assert(Node node, Scope scope)
        {
            // Node oper = node.childs[0];
            // if (!booleanOperation(oper, scope))
            // {
            //     Console.WriteLine("Assertion failed!");
            // }
        }

        // private void forLoop(Node node, Scope scope)
        // {
        //     Node var = node.childs[0];
        //     Node in_ = var.childs[0];
        //     Node range = in_.childs[0];

        //     Node start = range.childs[0];
        //     Node end = range.childs[1];
        //     int startVar = integerOperation(start);
        //     int endVar = integerOperation(end);

        //     Node do_ = in_.childs[1];

        //     if (variables.ContainsKey(var.token.value))
        //     {
        //         if (variables[var.token.value].type.Equals("int"))
        //         {
        //             if (startVar <= endVar)
        //             {
        //                 variables[var.token.value].type = "control variable";
        //                 variables[var.token.value].value = startVar;
        //                 while (Convert.ToInt32(variables[var.token.value].value) <= endVar)
        //                 {
        //                     foreach (Node n in do_.childs)
        //                     {
        //                         statement(n);
        //                     }
        //                     variables[var.token.value].value = Convert.ToInt32(variables[var.token.value].value) + 1;
        //                 }
        //                 variables[var.token.value].type = "int";
        //             }
        //             else
        //             {
        //                 Error e = new Error("SEMANTIC ERROR: for loop start value should be smaller than end value", node.token.line);
        //                 Console.WriteLine(e);
        //             }
        //         }
        //         else
        //         {
        //             Error e = new Error("SEMANTIC ERROR: expected integer for loop variable", node.token.line);
        //             Console.WriteLine(e);
        //         }
        //     }
        //     else
        //     {
        //         Error e = new Error("SEMANTIC ERROR: undeclared variable " + node.token.value, node.token.line);
        //         Console.WriteLine(e);
        //     }
        // }

    }
}