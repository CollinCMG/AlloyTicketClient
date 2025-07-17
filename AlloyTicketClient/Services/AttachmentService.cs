using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace AlloyTicketClient.Services
{
    public class AttachmentService
    {
        /// <summary>
        /// Reads a list of IBrowserFile and returns a list of objects with file name, content type, and data as byte array.
        /// </summary>
        public async Task<List<dynamic>> PrepareAttachmentsAsync(IEnumerable<IBrowserFile> files)
        {
            var attachments = new List<dynamic>();
            foreach (var file in files)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await file.OpenReadStream().CopyToAsync(memoryStream);
                    var attachment = new
                    {
                        FileName = file.Name,
                        Description = file.ContentType,
                        Data = memoryStream.ToArray()
                    };
                    attachments.Add(attachment);
                }
            }
            return attachments;
        }

        /// <summary>
        /// Prepares attachments and returns a StringContent with application/json content type.
        /// </summary>
        public async Task<StringContent> CreateAttachmentStringContentAsync(IEnumerable<IBrowserFile> files)
        {
            var attachments = await PrepareAttachmentsAsync(files);
            var json = JsonSerializer.Serialize(attachments);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }
}
