namespace BatchImageCropper
{
    public class ExportOptions
    {
        public string Format { get; set; } = "source"; // source, jpg, png, bmp, gif
        public int Quality { get; set; } = 95;         // 0-100 (yalnızca JPEG)
        public string Suffix { get; set; } = "_kirpilmis";
        public bool UseSourceFolder { get; set; } = true;
        public string OutputFolder { get; set; } = string.Empty;
        public bool PreserveMetadata { get; set; } = true;

        public ExportOptions Clone()
        {
            return new ExportOptions
            {
                Format = Format,
                Quality = Quality,
                Suffix = Suffix,
                UseSourceFolder = UseSourceFolder,
                OutputFolder = OutputFolder,
                PreserveMetadata = PreserveMetadata
            };
        }
    }
}