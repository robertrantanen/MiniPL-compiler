using System;

namespace MiniPl
{
    class Error
    {
        private string message;
        private int line;

        public Error(string message_, int line_)
        {
            message = message_;
            line = line_;
        }

        public override string ToString()
        {
            return message + " on line " + line;
        }
    }

}