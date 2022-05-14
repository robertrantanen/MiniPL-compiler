using System;
using System.Collections.Generic;

//mcs *.cs -out:minipl.exe
//mono minipl.exe

namespace MiniPl
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Enter filename");

            string text = Console.ReadLine();

            if (System.IO.File.Exists(text))
            {

                string file = System.IO.File.ReadAllText(text);


                Scanner scanner = new Scanner();
                List<Token> tokens = scanner.scan(file);
                // Console.WriteLine(tokens.Count + " tokens");

                // foreach (var token in tokens)
                // {
                //     Console.WriteLine(token);
                // }
                // Console.WriteLine("");

                Parser parser = new Parser(tokens);
                Ast ast = parser.parse();
                // ast.printChilds(ast.root);
                Code_generation gen = new Code_generation(ast);
                gen.start();
                Console.WriteLine("Run the command ./program to run the program");
            }
            else
            {
                Console.WriteLine("File not found");
            }
            Console.WriteLine();

        }
    }

}