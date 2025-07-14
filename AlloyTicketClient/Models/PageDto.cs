using AlloyTicketClient.Enums;

namespace AlloyTicketClient.Models
{
    public class PageDto
    {
        public string PageName { get; set; } = string.Empty;
        public int PageRank { get; set; }
        public List<IPageItem> Items { get; set; } = new();
    }

    public interface IPageItem
    {
        int SortOrder { get; set; }
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
        public bool? Read_Only { get; set; }
        public string? Lookup_Values { get; set; }
        public string? Table_Name { get; set; }
        public Guid? Lookup_ID { get; set; }
        public string? Filter { get; set; }
        public FieldType? FieldType { get; set; }
        public string? DisplayFields { get; set; }
        public bool IsHidden { get; set; } = false;
    }

    public class FieldTextDto : IPageItem
    {
        public string ElementDefinition { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class AttachmentInputDto : IPageItem
    {
        public string ElementDefinition { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
