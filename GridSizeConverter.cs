using System;
using System.Globalization;
using System.Windows.Data;

namespace BatchImageCropper
{
    public class GridSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 300.0;

            double totalWidth = (double)value;
            
            // Calculate optimal grid size based on window width
            // Target: 3-4 columns with reasonable spacing
            double minItemSize = 250;
            double maxItemSize = 300;
            
            // Calculate how many columns can fit
            int columns = Math.Max(1, (int)(totalWidth / minItemSize));
            columns = Math.Min(columns, 6); // Max 6 columns
            
            // Calculate item size with margins (5px left + 5px right = 10px per item)
            double itemSize = (totalWidth - (columns * 10)) / columns;
            itemSize = Math.Max(minItemSize, Math.Min(maxItemSize, itemSize));
            
            return itemSize;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
