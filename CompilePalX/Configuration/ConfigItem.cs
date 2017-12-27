using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilePalX
{
    public class ConfigItem : ICloneable
    {
        public string Name { get; set; }
        public string Parameter { get; set; }
        public string Description { get; set; }

        public string Value { get; set; }

        public bool CanHaveValue { get; set; }

        public string Warning { get; set; }

        public bool CanBeUsedMoreThanOnce { get; set; }

        public object Clone()
        {
            return new ConfigItem() {Name=Name,Parameter=Parameter,Description = Description,Value=Value,CanHaveValue = CanHaveValue,Warning = Warning,CanBeUsedMoreThanOnce = CanBeUsedMoreThanOnce};
        }
    }
}
