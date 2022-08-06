using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CompilePalX
{
    /// <summary>
    /// Converts text to Title case
    /// </summary>
    public class CaseConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TextInfo textInfo = culture.TextInfo;

            return value != null ? textInfo.ToTitleCase(value.ToString()) : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException(); ;
        }
    }
}
