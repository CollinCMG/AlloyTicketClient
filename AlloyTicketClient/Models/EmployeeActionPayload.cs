namespace AlloyTicketClient.Models
{
    public class EmployeeActionPayload
    {
        public string FormId { get; set; } // The form id to use for loading form data
        public object Data { get; set; } // Dynamic form data
        public string CategoryId { get; set; }
        public string Route { get; set; }
    }
}
