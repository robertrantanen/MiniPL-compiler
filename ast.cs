using System;
using System.Collections.Generic;

namespace MiniPl
{

    class Node
    {
        public List<Node> childs { get; set; }
        public Token token { get; set; }

        public Node(Token token_)
        {
            childs = new List<Node>();
            token = token_;
        }

        public Node()
        {
            childs = new List<Node>();
        }
    }

    class Ast
    {
        public Node root { get; set; }

        public Node add(Token t, Node parent)
        {
            Node node = new Node(t);
            parent.childs.Add(node);
            return node;
        }

        public void printChilds(Node node)
        {
            Console.WriteLine("node: " + node.token.value);
            foreach (Node n in node.childs)
            {
                Console.WriteLine(n.token.value);
            }
            foreach (Node n in node.childs)
            {
                printChilds(n);
            }
        }


        public void traverse(Node parent)
        {
            if (parent != null)
            {
                foreach (Node node in parent.childs)
                {
                    traverse(node);
                }
                Console.WriteLine(parent.token.value);
            }
        }
    }
}