using System.Text.Json;

namespace AlloyTicketClient.Models
{
    public class RequestActionPayload
    {
        public Guid FormId { get; set; }
        public string Requester_ID { get; set; }
        public string ObjectId { get; set; }
        public JsonElement Data { get; set; }
    }
}