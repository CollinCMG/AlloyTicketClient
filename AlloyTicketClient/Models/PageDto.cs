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

    public class AttachmentInputDto : IPageItem
    {
        public Guid? DefinitionID { get; set; }

        public string FieldName { get; set; } = string.Empty;
        public string ElementDefinition { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public AttachmentConfig? Config { get; set; } // Parsed config from XML

        // Added for file upload mapping
        public string? UploadedFileName { get; set; }
        public string? UploadedFileContentBase64 { get; set; }
        public bool IsHidden { get; set; } = false;
        public FieldType? FieldType { get; set; } = Enums.FieldType.Attachment;
        public bool? Mandatory { get; set; }

        public static AttachmentConfig? ParseConfigFromXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) return null;
            try
            {
                var doc = XDocument.Parse(xml);
                var config = new AttachmentConfig();
                foreach (var item in doc.Descendants("ITEM"))
                {
                    var name = (string?)item.Attribute("Name");
                    var value = item.Attribute("Value")?.Value;
                    if (string.IsNullOrEmpty(name) || value == null) continue;
                    switch (name)
                    {
                        case "Caption":
                            config.Caption = value;
                            break;
                        case "ForProgram":
                            config.ForProgram = value == "1" || value.ToLower() == "true";
                            break;
                        case "Mandatory":
                            config.Mandatory = value == "1" || value.ToLower() == "true";
                            break;
                        case "ReadOnly":
                            config.ReadOnly = value == "1" || value.ToLower() == "true";
                            break;
                        case "InclFiles":
                            config.InclFiles = value == "1" || value.ToLower() == "true";
                            break;
                        case "InclExisting":
                            config.InclExisting = value == "1" || value.ToLower() == "true";
                            break;
                    }
                }
                return config;
            }
            catch
            {
                return null;
            }
        }
    }
}
