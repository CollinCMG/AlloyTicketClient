using AlloyTicketClient.Enums;

namespace AlloyTicketClient.Models.DTOs
{
    public class FieldTextDto : IPageItem
    {
        public string ElementDefinition { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public FieldTextConfig? Config { get; set; } // Parsed config from XML
        public Guid? DefinitionID { get; set; }
        public string FieldName { get; set; }
        public bool IsHidden { get; set; } = false;
        public FieldType? FieldType { get; set; } = Enums.FieldType.Text;
        public bool? Mandatory { get; set; } = false;

        public static FieldTextConfig? ParseConfigFromXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) return null;
            try
            {
                // Use FormDataMapperService.GetTextAndUrl
                var (text, url) = Services.FormDataMapperService.GetTextAndUrl(xml);
                return new FieldTextConfig { Text = text, Url = url };
            }
            catch
            {
                return null;
            }
        }
    }
}
