using AlloyTicketClient.Enums;

namespace AlloyTicketClient.Models.DTOs
{
    public class FieldInputDto
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
        public List<DropdownOptionDto>? Options { get; set; }

    }
}
