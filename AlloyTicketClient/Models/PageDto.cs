using AlloyTicketClient.Enums;
using System.Xml.Linq;

namespace AlloyTicketClient.Models
{
    public class PageDto
    {
        public string PageName { get; set; } = string.Empty;
        public int PageRank { get; set; }
        public List<IPageItem> Items { get; set; } = new();

        // Returns true if all FieldInputDto items are hidden (IsHidden == true), false otherwise
        public bool IsHidden => Items.OfType<FieldInputDto>().Any() && Items.OfType<FieldInputDto>().All(f => f.IsHidden);
    }

    public interface IPageItem
    {
        public Guid? DefinitionID { get; set; }

        int SortOrder { get; set; }
        string FieldName { get; set; }
        bool IsHidden { get; set; }
        FieldType? FieldType { get; set; }
        bool? Mandatory { get; set; }


    }

    public class FieldInputDto : IPageItem
    {
        public int? Field_Num { get; set; }
        public string FieldLabel { get; set; } = string.Empty;
        public string FieldValue { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public Guid? DefinitionID { get; set; }
        public int SortOrder { get; set; }
        public bool? Virtual { get; set; }
        public bool? Mandatory { get; set; }
        public bool? ReadOnly { get; set; }
        public string? Lookup_Values { get; set; }
        public string? Table_Name { get; set; }
        public Guid? Lookup_ID { get; set; }
        public string? Filter { get; set; }
        public FieldType? FieldType { get; set; }
        public string? DisplayFields { get; set; }
        public bool IsHidden { get; set; } = false;
    }

    public class FieldTextConfig
    {
        public string? Text { get; set; }
        public string? Url { get; set; }
    }

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
                var (text, url) = AlloyTicketClient.Services.FormDataMapperService.GetTextAndUrl(xml);
                return new FieldTextConfig { Text = text, Url = url };
            }
            catch
            {
                return null;
            }
        }
    }
}
