namespace AlloyTicketClient.Models
{
    public class RequestActionPayload
    {
        public string FormId { get; set; } 
        public object Data { get; set; } 
        public string CategoryId { get; set; }
        public string Route { get; set; }
    }
}
