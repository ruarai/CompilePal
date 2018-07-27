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
        public bool ValueIsFile { get; set; }
        public string Value2 { get; set; }
		public bool Value2IsFile { get; set; }

		public bool ReadOutput { get; set; }

		public bool CanHaveValue { get; set; }

        public string Warning { get; set; }

        public bool CanBeUsedMoreThanOnce { get; set; }

        public object Clone()
        {
            return new ConfigItem() {Name=Name,Parameter=Parameter,Description = Description,Value=Value, Value2 = Value2, CanHaveValue = CanHaveValue,Warning = Warning,CanBeUsedMoreThanOnce = CanBeUsedMoreThanOnce, ReadOutput = ReadOutput, ValueIsFile = ValueIsFile, Value2IsFile = Value2IsFile};
        }
    }
}
