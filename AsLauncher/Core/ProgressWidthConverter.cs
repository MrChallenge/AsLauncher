using System;
using System.Globalization;
using System.Windows.Data;

namespace AsLauncher.Core
{
    public class ProgressWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3)
                return 0d;

            double width = System.Convert.ToDouble(values[0]);

            double value = System.Convert.ToDouble(values[1]);

            double maximum = System.Convert.ToDouble(values[2]);

            if (maximum <= 0)
                return 0d;

            return width * (value / maximum);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}