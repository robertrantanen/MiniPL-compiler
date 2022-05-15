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
                if (variables[key].type.Equals("array"))
                {
                    int i = Convert.ToInt32(iden.childs[0].token.value);
                    object[] array = ((Array)variables[key].value).Cast<object>().ToArray();
                    int[] ints = Array.ConvertAll(array, item => Convert.ToInt32(item));
                    ints[i] = Convert.ToInt32(value);
                    variables[key].value = ints;
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


    class Code_generation
    {

        static private Ast ast;

        static private List<TokenType> operators = new List<TokenType>() { TokenType.PLUS, TokenType.MINUS, TokenType.STAR, TokenType.SLASH, TokenType.MODULO, TokenType.AND, TokenType.OR, TokenType.EQUAL, TokenType.LESS, TokenType.GREATER, TokenType.EQUALGREATER, TokenType.EQUALLESS, TokenType.NOTEQUAL, TokenType.NOT };

        static private List<TokenType> booleanOperators = new List<TokenType>() { TokenType.AND, TokenType.OR, TokenType.EQUAL, TokenType.LESS, TokenType.GREATER, TokenType.EQUALGREATER, TokenType.EQUALLESS, TokenType.NOTEQUAL, TokenType.NOT };
        string text = "#include <stdio.h>\n#include <stdbool.h>\n";

        int currentR = 0;
        int currentL = 0;

        public Code_generation(Ast ast_)
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
                if (n.token.value.Equals("var"))
                {
                    Node val = n.childs[0];
                    switch (val.childs[0].token.value)
                    {
                        case "integer":
                            text += "int *" + val.token.value + ", ";
                            break;
                        case "real":
                            text += "float *" + val.token.value + ", ";
                            break;
                        case "string":
                            text += "char *" + val.token.value + "[], ";
                            break;
                        case "boolean":
                            text += "bool *" + val.token.value + ", ";
                            break;
                    }
                    newScope.add(val, val.childs[0]);
                }
                else
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
                            string arrtype = n.childs[0].childs[0].token.value;
                            if (arrtype.Equals("integer"))
                            {
                                text += "int " + n.token.value + "[], ";
                            }
                            else if (arrtype.Equals("real"))
                            {
                                text += "float " + n.token.value + "[], ";
                            }
                            else if (arrtype.Equals("string"))
                            {
                                text += "char *" + n.token.value + "[], ";
                            }
                            else if (arrtype.Equals("boolean"))
                            {
                                text += "bool " + n.token.value + "[], ";
                            }
                            break;
                    }
                    newScope.add(n, n.childs[0]);
                }
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
            Node begin = name.childs[name.childs.Count - 1];
            text += "void " + name.token.value + "(";
            foreach (Node n in parameters)
            {
                if (n.token.value.Equals("var"))
                {
                    Node val = n.childs[0];
                    switch (val.childs[0].token.value)
                    {
                        case "integer":
                            text += "int *" + val.token.value + ", ";
                            break;
                        case "real":
                            text += "float *" + val.token.value + ", ";
                            break;
                        case "string":
                            text += "char *" + val.token.value + "[], ";
                            break;
                        case "boolean":
                            text += "bool *" + val.token.value + ", ";
                            break;

                    }
                    newScope.add(val, val.childs[0]);
                }
                else
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
                            string arrtype = n.childs[0].childs[0].token.value;
                            if (arrtype.Equals("integer"))
                            {
                                text += "int " + n.token.value + "[], ";
                            }
                            else if (arrtype.Equals("real"))
                            {
                                text += "float " + n.token.value + "[], ";
                            }
                            else if (arrtype.Equals("string"))
                            {
                                text += "char *" + n.token.value + "[], ";
                            }
                            else if (arrtype.Equals("boolean"))
                            {
                                text += "bool " + n.token.value + "[], ";
                            }
                            break;
                    }
                    newScope.add(n, n.childs[0]);
                }
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
            text += ") goto " + "L" + i + ";\n";
        }

        private void if_statement(Node node, Scope scope)
        {
            Node stat = node.childs[0];
            Node then = node.childs[1];

            text += "if (";
            printOperation(stat, scope);
            text += ") goto " + nextL() + ";\n";

            if (node.childs.Count() > 2)
            {
                Node els = node.childs[2];
                statement(els.childs[0], scope);
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
                                text += "char *" + iden.token.value + "[" + arrVal + "];\n";
                                break;
                            case "boolean":
                                text += "bool " + iden.token.value + "[" + arrVal + "];\n";
                                break;
                        }
                        break;
                }
            }
        }

        private void editVariable(Node node, Scope scope)
        {
            Node value = node.childs[0].childs[0];
            scope.edit(node, expression(value, scope));
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
            string i = node.childs[0].token.value;
            text += node.token.value + "[" + i + "] = " + getCurrentR() + ";\n";

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
                case TokenType.IDENTIFIER:
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
                            int[] test1 = new int[1];
                            float[] test2 = new float[1];
                            string[] test3 = new string[1];
                            bool[] test4 = new bool[1];
                            if (e2.value.GetType().Equals(test1.GetType()))
                            {
                                return integerOperation(node, scope);
                            }
                            else if (e2.value.GetType().Equals(test2.GetType()))
                            {
                                return realOperation(node, scope);
                            }
                            else if (e2.value.GetType().Equals(test3.GetType()))
                            {
                                return stringOperation(node, scope);
                            }
                            else if (e2.value.GetType().Equals(test4.GetType()))
                            {
                                return booleanOperation(node, scope);
                            }
                            return null;
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

                            string t = operatorType(node, scope);
                            switch (t)
                            {
                                case "integer":
                                    return integerOperation(node, scope);
                                case "real":
                                    return realOperation(node, scope);
                                case "string":
                                    return stringOperation(node, scope);
                                case "array":
                                    return integerOperation(node, scope);
                            }
                        }
                    }
                    return null;
            }
        }

        private string operatorType(Node node, Scope scope)
        {
            if (node.childs.Count > 0)
            {
                Node n = node.childs[0];
                if (operators.Contains(n.token.type))
                {
                    return operatorType(n, scope);
                }
                else
                {
                    switch (n.token.type)
                    {
                        case TokenType.INT:
                            return "integer";
                        case TokenType.REAL:
                            return "real";
                        case TokenType.STRING:
                            return "string";
                        case TokenType.IDENTIFIER:
                            Element e = scope.get(n.token.value);
                            if (e.type.Equals("integer"))
                            {
                                return "integer";
                            }
                            else if (e.type.Equals("real"))
                            {
                                return "real";
                            }
                            else if (e.type.Equals("string"))
                            {
                                return "string";
                            }
                            else if (e.type.Equals("array"))
                            {
                                int[] test1 = new int[1];
                                float[] test2 = new float[1];
                                string[] test3 = new string[1];
                                if (e.value.GetType().Equals(test1.GetType()))
                                {
                                    return "integer";
                                }
                                else if (e.value.GetType().Equals(test2.GetType()))
                                {
                                    return "real";
                                }
                                else if (e.value.GetType().Equals(test3.GetType()))
                                {
                                    return "string";
                                }
                                return "null";
                            }
                            return "null";
                    }
                }
            }
            return "null";
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
                string first = getCurrentR();
                int rightint = integerOperation(right, scope);
                string second = getCurrentR();
                if (operation == TokenType.PLUS)
                {
                    text += "int " + nextR() + " = " + first + " + " + second + ";\n";
                    return leftint + rightint;
                }
                else if (operation == TokenType.MINUS)
                {
                    text += "int " + nextR() + " = " + first + " - " + second + ";\n";
                    return leftint - rightint;
                }
                else if (operation == TokenType.STAR)
                {
                    text += "int " + nextR() + " = " + first + " * " + second + ";\n";
                    return leftint * rightint;
                }
                else if (operation == TokenType.SLASH)
                {
                    text += "int " + nextR() + " = " + first + " / " + second + ";\n";
                    return leftint / rightint;
                }
                else if (operation == TokenType.MODULO)
                {
                    text += "int " + nextR() + " = " + first + " % " + second + ";\n";
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
                    if (node.childs.Count > 0)
                    {
                        string i = node.childs[0].token.value;
                        text += "float " + nextR() + " = " + node.token.value + "[" + i + "];\n";
                        return 0;
                    }
                    else
                    {
                        text += "float " + nextR() + " = " + node.token.value + ";\n";
                        return float.Parse(Convert.ToString(scope.get(node.token.value).value));
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
                float leftint = realOperation(left, scope);
                string first = getCurrentR();
                float rightint = realOperation(right, scope);
                string second = getCurrentR();
                if (operation == TokenType.PLUS)
                {
                    text += "float " + nextR() + " = " + first + " + " + second + ";\n";
                    return leftint + rightint;
                }
                else if (operation == TokenType.MINUS)
                {
                    text += "float " + nextR() + " = " + first + " - " + second + ";\n";
                    return leftint - rightint;
                }
                else if (operation == TokenType.STAR)
                {
                    text += "float " + nextR() + " = " + first + " * " + second + ";\n";
                    return leftint * rightint;
                }
                else if (operation == TokenType.SLASH)
                {
                    text += "float " + nextR() + " = " + first + " / " + second + ";\n";
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
                text += "strcat(" + lastR() + ", " + getCurrentR() + ");\n";
                text += "char " + nextR() + "[99];\n";
                text += "strcpy(" + getCurrentR() + ", " + secondLastR() + ");\n";
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
                                int[] test1 = new int[1];
                                float[] test2 = new float[1];
                                string[] test3 = new string[1];
                                bool[] test4 = new bool[1];
                                string i = printable.childs[0].token.value;
                                if (e.value.GetType().Equals(test1.GetType()))
                                {
                                    text += "printf(\"%d\", " + printable.token.value + "[" + i + "]);\n";
                                }
                                else if (e.value.GetType().Equals(test2.GetType()))
                                {
                                    text += "printf(\"%f\", " + printable.token.value + "[" + i + "]);\n";
                                }
                                else if (e.value.GetType().Equals(test3.GetType()))
                                {
                                    text += "printf(\"%s\", " + printable.token.value + "[" + i + "]);\n";
                                }
                                else if (e.value.GetType().Equals(test4.GetType()))
                                {
                                    text += "printf(\"%d\", " + printable.token.value + "[" + i + "]);\n";
                                }
                                break;
                            case "null":
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
                        case TokenType.BOOLEAN:
                            text += "printf(\"%d\", " + printable.token.value + ");\n";
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
                        if (e.type.Equals("integer"))
                        {
                            text += "scanf(\"%d\", &" + readable.token.value + ");\n";
                        }
                        else if (e.type.Equals("real"))
                        {
                            text += "scanf(\"%f\", &" + readable.token.value + ");\n";
                        }
                        else if (e.type.Equals("string"))
                        {
                            text += "scanf(\"%s\", &" + readable.token.value + ");\n";
                        }
                        else if (e.type.Equals("boolean"))
                        {
                            text += "scanf(\"%d\", &" + readable.token.value + ");\n";
                        }
                        else if (e.type.Equals("array"))
                        {
                            string i = readable.childs[0].token.value;
                            int[] test1 = new int[1];
                            float[] test2 = new float[1];
                            string[] test3 = new string[1];
                            bool[] test4 = new bool[1];
                            if (e.value.GetType().Equals(test1.GetType()))
                            {
                                text += "scanf(\"%d\", &" + readable.token.value + "[" + i + "]);\n";
                            }
                            else if (e.value.GetType().Equals(test2.GetType()))
                            {
                                text += "scanf(\"%f\", &" + readable.token.value + "[" + i + "]);\n";
                            }
                            else if (e.value.GetType().Equals(test3.GetType()))
                            {
                                text += "scanf(\"%s\", &" + readable.token.value + "[" + i + "]);\n";
                            }
                            else if (e.value.GetType().Equals(test4.GetType()))
                            {
                                text += "scanf(\"%d\", &" + readable.token.value + "[" + i + "]);\n";
                            }
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
            Node stat = node.childs[0];
            text += "if (";
            printOperation(stat, scope);
            text += ") goto " + nextL() + ";\n";

            text += "else {\n";
            text += "printf(\"Assertion ";
            printOperation(stat, scope);
            text += " failed!\");\n";
            text += "}\n";

            text += "goto " + getFollowingL() + ";\n";
            text += getCurrentL() + ": ;\n";
            text += "printf(\"Assertion ";
            printOperation(stat, scope);
            text += " success!\");\n";
            text += nextL() + ": ;\n";
        }



    }
}