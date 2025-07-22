using AlloyTicketClient.Enums;

namespace AlloyTicketClient.Models
{
    public interface IPageItem
    {
        public Guid? DefinitionID { get; set; }

        int SortOrder { get; set; }
        string FieldName { get; set; }
        bool IsHidden { get; set; }
        FieldType? FieldType { get; set; }
        bool? Mandatory { get; set; }


    }
}
