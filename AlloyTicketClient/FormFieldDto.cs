using AlloyTicketClient.Enums;

public class FormFieldDto
{
    public Guid ID { get; set; }
    public string? Field_Name { get; set; }
    public string? Field_Label { get; set; }
    public int? Field_Num { get; set; }
    public bool? Virtual { get; set; }
    public bool? Mandatory { get; set; }
    public bool? Read_Only { get; set; }
    public string? Lookup_Values { get; set; }
    public string? Table_Name { get; set; }
    public Guid? Lookup_ID { get; set; }
    public string? Filter { get; set; }
    public FieldType? FieldType { get; set; } 
    public string? Display_Fields { get; set; } // Added for display_fields column
    public List<string>? Options { get; set; } // For dropdown options
}