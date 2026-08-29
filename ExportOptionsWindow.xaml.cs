using System;
using System.Windows;
using System.Windows.Controls;

namespace BatchImageCropper
{
    public partial class ExportOptionsWindow : Window
    {
        public ExportOptions Result { get; private set; }

        private readonly bool _isTurkish;

        public ExportOptionsWindow(ExportOptions current, bool isTurkish = true)
        {
            InitializeComponent();
            _isTurkish = isTurkish;
            ApplyLanguage();

            SelectComboItem(FormatCombo, current.Format);

            QualitySlider.Value = Math.Max(50, Math.Min(100, current.Quality));
            QualityValueLabel.Text = current.Quality.ToString();

            SuffixBox.Text = current.Suffix ?? "_kirpilmis";
            SourceFolderCheck.IsChecked = current.UseSourceFolder;
            FolderBox.Text = current.OutputFolder ?? string.Empty;

            UpdateFolderControls();
            FormatCombo_SelectionChanged(null, null);
        }

        private void ApplyLanguage()
        {
            Title = _isTurkish ? "Dışa Aktarma Ayarları" : "Export Settings";
            TitleText.Text = _isTurkish ? "Dışa Aktarma Ayarları" : "Export Settings";
            FormatLabel.Text = _isTurkish ? "Format:" : "Format:";
            QualityLabelTitle.Text = _isTurkish ? "Kalite:" : "Quality:";
            SuffixLabel.Text = _isTurkish ? "Son ek:" : "Suffix:";
            SourceFolderCheck.Content = _isTurkish ? "Kaynak klasörünü kullan" : "Use source folder";
            BrowseButton.Content = _isTurkish ? "Seç..." : "Browse...";
            FolderHintText.Text = _isTurkish
                ? "Klasör seçilmezse kaynak klasörü kullanılır."
                : "If no folder is chosen, the source folder is used.";
            OkButton.Content = _isTurkish ? "Tamam" : "OK";
            CancelButton.Content = _isTurkish ? "İptal" : "Cancel";

            if (FormatCombo.Items.Count > 0)
            {
                ((ComboBoxItem)FormatCombo.Items[0]).Content = _isTurkish ? "Kaynak formatını koru" : "Keep source format";
            }
        }

        private static void SelectComboItem(ComboBox combo, string tag)
        {
            foreach (ComboBoxItem item in combo.Items)
            {
                if (string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
            combo.SelectedIndex = 0;
        }

        private void UpdateFolderControls()
        {
            var useSource = SourceFolderCheck.IsChecked == true;
            FolderBox.IsEnabled = !useSource;
            BrowseButton.IsEnabled = !useSource;
        }

        private void FormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = FormatCombo.SelectedItem as ComboBoxItem;
            var isJpeg = string.Equals(selected?.Tag?.ToString(), "jpg", StringComparison.OrdinalIgnoreCase);
            QualitySlider.IsEnabled = isJpeg;
            QualityValueLabel.IsEnabled = isJpeg;
        }

        private void QualitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            QualityValueLabel.Text = ((int)e.NewValue).ToString();
        }

        private void SourceFolder_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateFolderControls();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = _isTurkish ? "Çıktı klasörünü seçin" : "Select output folder"
            };

            if (!string.IsNullOrWhiteSpace(FolderBox.Text) && System.IO.Directory.Exists(FolderBox.Text))
            {
                dialog.SelectedPath = FolderBox.Text;
            }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FolderBox.Text = dialog.SelectedPath;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var selected = FormatCombo.SelectedItem as ComboBoxItem;
            Result = new ExportOptions
            {
                Format = selected?.Tag?.ToString() ?? "source",
                Quality = (int)QualitySlider.Value,
                Suffix = string.IsNullOrWhiteSpace(SuffixBox.Text) ? "_kirpilmis" : SuffixBox.Text.Trim(),
                UseSourceFolder = SourceFolderCheck.IsChecked == true,
                OutputFolder = FolderBox.Text?.Trim() ?? string.Empty
            };

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}