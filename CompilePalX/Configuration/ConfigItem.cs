using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilePalX
{
    public class ConfigItem
    {
        public string Name { get; set; }
        public string Parameter { get; set; }
        public string Description { get; set; }

        public string Value { get; set; }


        public bool CanHaveValue { get; set; }

        public string Warning { get; set; }
    }
}
