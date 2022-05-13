using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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
                    int arrVal = 99;
                    string arrType = "";
                    if (typeNode.childs.Count > 1)
                    {
                        arrVal = Convert.ToInt32(typeNode.childs[0].token.value);
                        arrType = typeNode.childs[1].token.value;
                    }
                    else
                    {
                        arrType = typeNode.childs[0].token.value;
                    }
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

        public void edit(Node iden, Object value)
        {
            string key = iden.token.value;
            if (variables.ContainsKey(key))
            {
                //Console.WriteLine(variables[key].type);
                if (variables[key].type.Equals("array"))
                {
                    int i = Convert.ToInt32(iden.childs[0].token.value);
                    //Console.WriteLine(variables[key].value.GetType());
                    object[] array = ((Array)variables[key].value).Cast<object>().ToArray();
                    int[] ints = Array.ConvertAll(array, item => Convert.ToInt32(item));
                    ints[i] = Convert.ToInt32(value);
                    variables[key].value = ints;
                    // switch(variables[key].value.GetType()) {

                    // }

                }
                else
                {
                    variables[key].value = value;
                    return;
                }
            }
            else if (parent == null)
            {
                Error e = new Error("SEMANTIC ERROR: variable " + key + " not found", iden.token.line);
                Console.WriteLine(e);
            }
            else
            {
                parent.edit(iden, value);
            }
        }

        public Element get(string key)
        {
            if (variables.ContainsKey(key))
            {
                return variables[key];
            }
            else if (parent != null)
            {
                return parent.get(key);
            }
            return new Element(null, "null");
        }


        public void printVariables()
        {
            foreach (KeyValuePair<string, Element> k in variables)
            {
                Console.WriteLine("Key: {0}, Value: {1}, Type: {2}", k.Key, k.Value.value, k.Value.type);
                // if (k.Value.value.GetType().IsArray)
                // {
                //     object[] array = ((Array)k.Value.value).Cast<object>().ToArray();
                //     int[] ints = Array.ConvertAll(array, item => Convert.ToInt32(item));
                //     Console.WriteLine(ints[0]);
                // }
            }
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

        static private List<TokenType> operators = new List<TokenType>() { TokenType.PLUS, TokenType.MINUS, TokenType.STAR, TokenType.SLASH, TokenType.MODULO, TokenType.AND, TokenType.OR, TokenType.EQUAL, TokenType.LESS, TokenType.GREATER, TokenType.EQUALGREATER, TokenType.EQUALLESS, TokenType.NOTEQUAL, TokenType.NOT };

        static private List<TokenType> booleanOperators = new List<TokenType>() { TokenType.AND, TokenType.OR, TokenType.EQUAL, TokenType.LESS, TokenType.GREATER, TokenType.EQUALGREATER, TokenType.EQUALLESS, TokenType.NOTEQUAL, TokenType.NOT };
        string text = "#include <stdio.h>\n#include <stdbool.h>\n";

        int currentR = 0;
        int currentL = 0;

        public Semantic(Ast ast_)
        {
            ast = ast_;
        }

        public string nextR()
        {
            currentR++;
            return "r" + currentR;
        }

        public string getCurrentR()
        {
            return "r" + currentR;
        }

        public string lastR()
        {
            int i = currentR - 1;
            return "r" + i;
        }

        public string secondLastR()
        {
            int i = currentR - 2;
            return "r" + i;
        }

        public string nextL()
        {
            currentL++;
            return "L" + currentL;
        }

        public string getCurrentL()
        {
            return "L" + currentL;
        }

        public string getFollowingL()
        {
            int i = currentL + 1;
            return "L" + i;
        }


        public void start()
        {
            Scope global = new Scope(new Dictionary<string, Element>(), null);
            Node program = ast.root.childs[0];
            foreach (Node node in program.childs)
            {
                startBlock(node, global);
            }
            // global.printVariables();

            //gcc -o program program.c
            //./program
            text += "\n}";
            File.WriteAllText("program.c", text);
        }

        private void startBlock(Node node, Scope scope)
        {
            switch (node.token.type)
            {
                case TokenType.BEGIN:
                    text += "int main() {\n";
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
            Scope newScope = new Scope(new Dictionary<string, Element>(), scope);
            Node name = node.childs[0];
            List<Node> parameters = name.childs.GetRange(0, name.childs.Count - 2);
            Node type = name.childs[name.childs.Count - 2];
            //functions.Add(name.token.value, new CustomFunction(parameters, type.token.value, node));
            Node begin = name.childs[name.childs.Count - 1];
            string t = "";
            switch (type.token.value)
            {
                case "integer":
                    t = "int ";
                    break;
                case "real":
                    t = "float ";
                    break;
                case "string":
                    t = "const char* ";
                    break;
                case "boolean":
                    t = "bool ";
                    break;
            }
            text += t + name.token.value + "(";
            foreach (Node n in parameters)
            {
                switch (n.childs[0].token.value)
                {
                    case "integer":
                        text += "int " + n.token.value + ", ";
                        break;
                    case "real":
                        text += "float " + n.token.value + ", ";
                        break;
                    case "string":
                        text += "char " + n.token.value + "[], ";
                        break;
                    case "boolean":
                        text += "bool " + n.token.value + ", ";
                        break;
                    case "array":
                        text += "int " + n.token.value + "[], ";
                        break;

                        //array
                }
                newScope.add(n, n.childs[0]);
            }
            text = text.Substring(0, text.Length - 2);
            text += ") {\n";
            block(begin, newScope);
            text += "}\n";
        }

        private void procedure_declaration(Node node, Scope scope)
        {
            Scope newScope = new Scope(new Dictionary<string, Element>(), scope);
            Node name = node.childs[0];
            List<Node> parameters = name.childs.GetRange(0, name.childs.Count - 1);
            //functions.Add(name.token.value, new CustomFunction(parameters, "null", node));
            Node begin = name.childs[name.childs.Count - 1];
            text += "void " + name.token.value + "(";
            foreach (Node n in parameters)
            {
                switch (n.childs[0].token.value)
                {
                    case "integer":
                        text += "int " + n.token.value + ", ";
                        break;
                    case "real":
                        text += "float " + n.token.value + ", ";
                        break;
                    case "string":
                        text += "char " + n.token.value + "[], ";
                        break;
                    case "boolean":
                        text += "bool " + n.token.value + ", ";
                        break;
                    case "array":
                        text += "int " + n.token.value + "[], ";
                        break;
                }
                newScope.add(n, n.childs[0]);
            }
            text = text.Substring(0, text.Length - 2);
            text += ") {\n";
            block(begin, newScope);
            text += "}\n";
        }

        private void statement(Node node, Scope scope)
        {
            switch (node.token.type)
            {
                case TokenType.VAR:
                    defineVariable(node, scope);
                    return;
                case TokenType.IDENTIFIER:
                    try
                    {
                        if (node.childs[0].token.type == TokenType.STATEMENT)
                        {
                            editVariable(node, scope);
                        }
                        else if (node.childs.Count > 1 && node.childs[1].token.type == TokenType.STATEMENT)
                        {
                            editArrayVariable(node, scope);
                        }
                        else
                        {
                            call(node, scope);
                            text += ";\n";
                        }
                    }
                    catch
                    {
                        call(node, scope);
                        text += ";\n";
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
            text += "return ";
            if (node.childs.Count > 0)
            {
                printOperation(node.childs[0], scope);
            }
            text += ";\n";
        }

        private void while_statement(Node node, Scope scope)
        {
            Node stat = node.childs[0];
            Node do_ = node.childs[1];
            text += nextL() + ": ;\n";
            int i = currentL;
            block(do_, scope);
            text += "if (";
            printOperation(stat, scope);
            //text += stat.childs[0].token.value + s + stat.childs[1].token.value;
            text += ") goto " + "L" + i + ";\n";
        }

        private void if_statement(Node node, Scope scope)
        {
            Node stat = node.childs[0];
            Node then = node.childs[1];


            text += "if (";
            printOperation(stat, scope);
            //text += stat.childs[0].token.value + stat.token.value + stat.childs[1].token.value;
            text += ") goto " + nextL() + ";\n";

            if (node.childs.Count() > 2)
            {
                Node els = node.childs[2];
                text += "else {\n";
                statement(els.childs[0], scope);
                text += "}\n";
            }
            text += "goto " + getFollowingL() + ";\n";
            text += getCurrentL() + ": ;\n";
            statement(then.childs[0], scope);
            text += nextL() + ": ;\n";
        }

        private void call(Node node, Scope scope)
        {
            text += node.token.value + "(";
            if (node.childs.Count > 0)
            {
                foreach (Node n in node.childs)
                {
                    if (n.token.type == TokenType.STRING)
                    {
                        text += "\"" + n.token.value + "\", ";
                    }
                    else
                    {
                        printOperation(n, scope);
                        text += ", ";
                    }
                }
                text = text.Substring(0, text.Length - 2);
            }
            text += ")";
        }

        private void defineVariable(Node node, Scope scope)
        {
            Node type = node.childs[node.childs.Count - 1];
            //Console.WriteLine(type.token.value);
            for (int i = 0; i < node.childs.Count - 1; i++)
            {
                Node iden = node.childs[i];
                scope.add(iden, type);
                switch (type.token.value)
                {
                    case "integer":
                        text += "int " + iden.token.value + ";\n";
                        break;
                    case "real":
                        text += "float " + iden.token.value + ";\n";
                        break;
                    case "string":
                        text += "char " + iden.token.value + "[99];\n";
                        break;
                    case "boolean":
                        text += "bool " + iden.token.value + ";\n";
                        break;
                    case "array":
                        int arrVal = Convert.ToInt32(type.childs[0].token.value);
                        string arrType = type.childs[1].token.value;
                        switch (arrType)
                        {
                            case "integer":
                                text += "int " + iden.token.value + "[" + arrVal + "];\n";
                                break;
                            case "real":
                                text += "float " + iden.token.value + "[" + arrVal + "];\n";
                                break;
                            case "string":
                                break;
                            case "boolean":
                                break;
                        }
                        break;
                }
            }
        }

        private void editVariable(Node node, Scope scope)
        {
            //Element element = scope.variables[node.token.value];
            Node value = node.childs[0].childs[0];
            scope.edit(node, expression(value, scope));
            //text += node.token.value + " = " + getCurrentR() + ";\n";
            Element e = scope.get(node.token.value);
            switch (e.type)
            {
                case "string":
                    text += "strcpy(" + node.token.value + ", " + getCurrentR() + ");\n";
                    return;
                default:
                    text += node.token.value + " = " + getCurrentR() + ";\n";
                    return;
            }
        }

        private void editArrayVariable(Node node, Scope scope)
        {

            Node value = node.childs[1].childs[0];
            scope.edit(node, expression(value, scope));
            int i = Convert.ToInt32(node.childs[0].token.value);
            text += node.token.value + "[" + i + "] = " + getCurrentR() + ";\n";

        }


        private Object expression(Node node, Scope scope)
        {
            //Console.WriteLine("TYPE " +node.token.type);
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
                case TokenType.IDENTIFIER:
                    //edit
                    Element e2 = scope.get(node.token.value);
                    if (node.childs.Count > 0)
                    {
                        if (node.childs[0].token.type == TokenType.SIZE)
                        {
                            object[] array = ((Array)scope.get(node.token.value).value).Cast<object>().ToArray();
                            text += "int " + nextR() + " = sizeof(" + node.token.value + ") / sizeof(" + node.token.value + "[0]);\n";
                            return array.Length;
                        }
                    }
                    switch (e2.type)
                    {
                        case "integer":
                            return integerOperation(node, scope);
                        case "real":
                            return realOperation(node, scope);
                        case "string":
                            return stringOperation(node, scope);
                        case "boolean":
                            return booleanOperation(node, scope);
                        case "array":
                            //edit for others
                            return integerOperation(node, scope);
                        case "null":
                            text += "int " + nextR() + " = ";
                            call(node, scope);
                            text += ";\n";
                            return null;
                        default:
                            return null;
                    }
                default:
                    if (operators.Contains(node.token.type))
                    {
                        if (booleanOperators.Contains(node.token.type))
                        {
                            return booleanOperation(node, scope);
                        }
                        else
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
                                case TokenType.IDENTIFIER:
                                    Element e = scope.get(first.token.value);
                                    if (e.type.Equals("integer"))
                                    {
                                        return integerOperation(node, scope);
                                    }
                                    else if (e.type.Equals("real"))
                                    {
                                        return realOperation(node, scope);
                                    }
                                    else if (e.type.Equals("string"))
                                    {
                                        return stringOperation(node, scope);
                                    }
                                    return null;
                            }
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
                text += "int " + nextR() + " = " + node.token.value + ";\n";
                return Convert.ToInt32(node.token.value);
            }
            else if (node.token.type == TokenType.IDENTIFIER)
            {
                try
                {
                    if (node.childs.Count > 0)
                    {
                        string i = node.childs[0].token.value;
                        text += "int " + nextR() + " = " + node.token.value + "[" + i + "];\n";
                        // object[] array = ((Array)scope.get(node.token.value).value).Cast<object>().ToArray();
                        // int[] ints = Array.ConvertAll(array, item => Convert.ToInt32(item));
                        // return ints[i];
                        return 0;
                    }
                    else
                    {
                        text += "int " + nextR() + " = " + node.token.value + ";\n";
                        return Convert.ToInt32(scope.get(node.token.value).value);
                    }
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
                    text += "int " + nextR() + " = " + secondLastR() + " + " + lastR() + ";\n";
                    return leftint + rightint;
                }
                else if (operation == TokenType.MINUS)
                {
                    text += "int " + nextR() + " = " + secondLastR() + " - " + lastR() + ";\n";
                    return leftint - rightint;
                }
                else if (operation == TokenType.STAR)
                {
                    text += "int " + nextR() + " = " + secondLastR() + " * " + lastR() + ";\n";
                    return leftint * rightint;
                }
                else if (operation == TokenType.SLASH)
                {
                    text += "int " + nextR() + " = " + secondLastR() + " / " + lastR() + ";\n";
                    return leftint / rightint;
                }
                else if (operation == TokenType.MODULO)
                {
                    text += "int " + nextR() + " = " + secondLastR() + " % " + lastR() + ";\n";
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
                text += "float " + nextR() + " = " + node.token.value + ";\n";
                return float.Parse(node.token.value);
            }
            else if (node.token.type == TokenType.IDENTIFIER)
            {
                try
                {
                    text += "float " + nextR() + " = " + node.token.value + ";\n";
                    return float.Parse(Convert.ToString(scope.get(node.token.value).value));
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
                float leftint = realOperation(left, scope);
                float rightint = realOperation(right, scope);
                if (operation == TokenType.PLUS)
                {
                    text += "float " + nextR() + " = " + secondLastR() + " + " + lastR() + ";\n";
                    return leftint + rightint;
                }
                else if (operation == TokenType.MINUS)
                {
                    text += "float " + nextR() + " = " + secondLastR() + " - " + lastR() + ";\n";
                    return leftint - rightint;
                }
                else if (operation == TokenType.STAR)
                {
                    text += "float " + nextR() + " = " + secondLastR() + " * " + lastR() + ";\n";
                    return leftint * rightint;
                }
                else if (operation == TokenType.SLASH)
                {
                    text += "float " + nextR() + " = " + secondLastR() + " / " + lastR() + ";\n";
                    return leftint / rightint;
                }
            }
            else
            {
                Error e = new Error("SEMANTIC ERROR: invalid type " + node.token.type + " ,expected real", node.token.line);
                Console.WriteLine(e);
            }
            return 0;
        }

        private string stringOperation(Node node, Scope scope)
        {
            if (node.token.type == TokenType.STRING)
            {
                text += "char " + nextR() + "[99] = \"" + node.token.value + "\";\n";
                return Convert.ToString(node.token.value);
            }
            else if (node.token.type == TokenType.IDENTIFIER)
            {
                try
                {
                    text += "char " + nextR() + "[99];\n";
                    text += "strcpy(" + getCurrentR() + ", " + node.token.value + ");\n";
                    return Convert.ToString(scope.get(node.token.value).value);
                }
                catch (Exception)
                {
                    Error e = new Error("SEMANTIC ERROR: undeclared variable " + node.token.value, node.token.line);
                    Console.WriteLine(e);
                }
            }
            else if (node.token.type == TokenType.PLUS)
            {
                Node leftNode = node.childs[0];
                Node rightNode = node.childs[1];
                string left = stringOperation(leftNode, scope);
                string right = stringOperation(rightNode, scope);
                text += "strcat(" + getCurrentR() + ", " + lastR() + ");\n";
                //text += "char " + nextR() + "[99] = " + secondLastR() + " + " + lastR() + ";\n";
                return left + right;
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
            Console.WriteLine(node.token.value);
            if (node.token.type == TokenType.BOOLEAN)
            {
                text += "bool " + nextR() + " = " + node.token.value + ";\n";
                return Convert.ToBoolean(node.token.value);
            }
            if (node.token.type == TokenType.IDENTIFIER)
            {
                try
                {
                    text += "bool " + nextR() + " = " + node.token.value + ";\n";
                    return Convert.ToBoolean(scope.get(node.token.value).value);
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
                Console.WriteLine(left.token.value);
                bool b = (expression(left, scope) == expression(right, scope));
                text += "bool " + nextR() + " = " + secondLastR() + " == " + lastR() + ";\n";
                return b;
            }
            else if (node.token.type == TokenType.LESS)
            {
                Object left2 = expression(node.childs[0], scope);
                Object right2 = expression(node.childs[1], scope);
                if (left2.GetType() == typeof(int))
                {
                    text += "bool " + nextR() + " < " + secondLastR() + " == " + lastR() + ";\n";
                    return (int)left2 < (int)right2;
                }
                else if (left2.GetType() == typeof(float))
                {
                    text += "bool " + nextR() + " < " + secondLastR() + " == " + lastR() + ";\n";
                    return (float)left2 < (float)right2;
                }
            }
            else if (node.token.type == TokenType.GREATER)
            {
                Object left2 = expression(node.childs[0], scope);
                Object right2 = expression(node.childs[1], scope);
                if (left2.GetType() == typeof(int))
                {
                    text += "bool " + nextR() + " > " + secondLastR() + " == " + lastR() + ";\n";
                    return (int)left2 > (int)right2;
                }
                else if (left2.GetType() == typeof(float))
                {
                    text += "bool " + nextR() + " > " + secondLastR() + " == " + lastR() + ";\n";
                    return (float)left2 > (float)right2;
                }
            }
            else if (node.token.type == TokenType.EQUALLESS)
            {
                Object left2 = expression(node.childs[0], scope);
                Object right2 = expression(node.childs[1], scope);
                if (left2.GetType() == typeof(int))
                {
                    text += "bool " + nextR() + " <= " + secondLastR() + " == " + lastR() + ";\n";
                    return (int)left2 <= (int)right2;
                }
                else if (left2.GetType() == typeof(float))
                {
                    text += "bool " + nextR() + " <= " + secondLastR() + " == " + lastR() + ";\n";
                    return (float)left2 <= (float)right2;
                }
            }
            else if (node.token.type == TokenType.EQUALGREATER)
            {
                Object left2 = expression(node.childs[0], scope);
                Object right2 = expression(node.childs[1], scope);
                if (left2.GetType() == typeof(int))
                {
                    text += "bool " + nextR() + " >= " + secondLastR() + " == " + lastR() + ";\n";
                    return (int)left2 >= (int)right2;
                }
                else if (left2.GetType() == typeof(float))
                {
                    text += "bool " + nextR() + " >= " + secondLastR() + " == " + lastR() + ";\n";
                    return (float)left2 >= (float)right2;
                }
            }
            else if (node.token.type == TokenType.NOTEQUAL)
            {
                left = node.childs[0];
                right = node.childs[1];
                bool b = (expression(left, scope) != expression(right, scope));
                text += "bool " + nextR() + " = " + secondLastR() + " != " + lastR() + ";\n";
                return b;
            }
            else if (node.token.type == TokenType.NOT)
            {
                left = node.childs[0];
                bool b = !booleanOperation(left, scope);
                text += "bool " + nextR() + " = !" + lastR() + ";\n";
                return b;
            }
            else if (node.token.type == TokenType.AND)
            {
                left = node.childs[0];
                right = node.childs[1];
                bool b = (booleanOperation(left, scope) && booleanOperation(right, scope));
                text += "bool " + nextR() + " = " + secondLastR() + " && " + lastR() + ";\n";
                return b;
            }
            else if (node.token.type == TokenType.OR)
            {
                left = node.childs[0];
                right = node.childs[1];
                bool b = (booleanOperation(left, scope) | booleanOperation(right, scope));
                text += "bool " + nextR() + " = " + secondLastR() + " || " + lastR() + ";\n";
                return b;
            }
            else
            {
                Error e = new Error("SEMANTIC ERROR: invalid type " + node.token.type + " ,expected boolean operation", node.token.line);
                Console.WriteLine(e);
            }
            return false;
        }

        private void printOperation(Node node, Scope scope)
        {
            if (operators.Contains(node.token.type))
            {
                if (node.token.type == TokenType.NOT)
                {
                    text += "!";
                    printOperation(node.childs[0], scope);
                }
                else
                {
                    printOperation(node.childs[0], scope);
                    string s = node.token.value;
                    if (node.token.type == TokenType.OR)
                    {
                        s = "||";
                    }
                    else if (node.token.type == TokenType.AND)
                    {
                        s = "&&";
                    }
                    else if (node.token.type == TokenType.NOTEQUAL)
                    {
                        s = "!=";
                    }
                    else if (node.token.type == TokenType.EQUAL)
                    {
                        s = "==";
                    }
                    text += s;
                    printOperation(node.childs[1], scope);
                }
            }
            else
            {
                if (node.token.type == TokenType.IDENTIFIER)
                {
                    Element e = scope.get(node.token.value);
                    if (e.type.Equals("null"))
                    {
                        call(node, scope);
                    }
                    else if (e.type.Equals("array"))
                    {
                        if (node.childs.Count > 0)
                        {
                            if (node.childs[0].token.type == TokenType.SIZE)
                            {
                                text += "sizeof(" + node.token.value + ") / sizeof(" + node.token.value + "[0])";
                            }
                            else
                            {
                                string i = node.childs[0].token.value;
                                text += node.token.value + "[" + i + "]";
                            }
                        } else {
                            text += node.token.value;
                        }

                    }
                    else
                    {
                        text += node.token.value;
                    }
                }
                else
                {
                    text += node.token.value;
                }
            }
        }



        private void print(Node node, Scope scope)
        {
            foreach (Node printable in node.childs)
            {
                //text += "printf(\"%s\", " + printable.token.value + ");\n";
                if (printable.token.type == TokenType.IDENTIFIER)
                {
                    try
                    {
                        Element e = scope.get(printable.token.value);

                        switch (e.type)
                        {
                            case "integer":
                                text += "printf(\"%d\", " + printable.token.value + ");\n";
                                break;
                            case "real":
                                text += "printf(\"%f\", " + printable.token.value + ");\n";
                                break;
                            case "string":
                                text += "printf(\"%s\", " + printable.token.value + ");\n";
                                break;
                            case "boolean":
                                text += "printf(\"%d\", " + printable.token.value + ");\n";
                                break;
                            case "array":
                                string i = printable.childs[0].token.value;
                                text += "printf(\"%d\", " + printable.token.value + "[" + i + "]);\n";
                                break;
                            case "null":
                                Console.WriteLine(printable.token.value);
                                text += "printf(\"%d\", ";
                                call(printable, scope);
                                text += ");\n";
                                break;
                        }
                    }
                    catch
                    {
                        text += "printf(\"%s\", " + printable.token.value + ");\n";
                    }

                }
                else
                {
                    switch (printable.token.type)
                    {
                        case TokenType.INT:
                            text += "printf(\"%d\", " + printable.token.value + ");\n";
                            break;
                        case TokenType.REAL:
                            text += "printf(\"%f\", " + printable.token.value + ");\n";
                            break;
                        case TokenType.STRING:
                            text += "printf(\"" + printable.token.value + "\");\n";
                            break;
                    }
                }
            }
        }

        private void read(Node node, Scope scope)
        {
            foreach (Node readable in node.childs)
            {

                if (readable.token.type == TokenType.IDENTIFIER)
                {
                    try
                    {
                        Element e = scope.get(readable.token.value);

                        if (e.type.Equals("string"))
                        {
                            text += "scanf(\"%s\", &" + readable.token.value + ");\n";
                        }
                        else if (e.type.Equals("integer"))
                        {
                            text += "scanf(\"%d\", &" + readable.token.value + ");\n";
                        }
                        else if (e.type.Equals("array"))
                        {
                            string i = readable.childs[0].token.value;
                            text += "scanf(\"%d\", &" + readable.token.value + "[" + i + "]);\n";
                        }
                    }
                    catch
                    {
                        Error er = new Error("SEMANTIC ERROR: undeclared variable " + node.token.value, node.token.line);
                        Console.WriteLine(er);
                    }
                }
                else
                {
                    Error e = new Error("SEMANTIC ERROR: expected to read a variable", node.token.line);
                    Console.WriteLine(e);
                }
            }
        }


        private void assert(Node node, Scope scope)
        {
            // Node oper = node.childs[0];
            // if (!booleanOperation(oper, scope))
            // {
            //     Console.WriteLine("Assertion failed!");
            // }
        }



    }
}