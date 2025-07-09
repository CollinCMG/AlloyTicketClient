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
    public string? LookupValues { get; set; }
    public string? TableName { get; set; }
    public Guid? LookupID { get; set; }
    public string? Filter { get; set; }
    public FieldType? FieldType { get; set; } 
    public string? DisplayFields { get; set; } 
    public List<string>? Options { get; set; } 
}