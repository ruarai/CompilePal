using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilePal
{
    public class Parameter
    {
        public string Command { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Option { get; set; }
        public bool Enabled { get; set; }
    }

}
