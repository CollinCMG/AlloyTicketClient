using AlloyTicketClient.Enums;
using AlloyTicketClient.Models;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AlloyTicketClient.Services
{
    public static class FormDataMapperService
    {
        /// <summary>
        /// Maps fieldValues (by GUID) to a dictionary keyed by field name, including all required fields (even if hidden), with dropdown handling.
        /// </summary>
        public static Dictionary<string, object?> MapFieldValuesToNameKeyed(
            List<PageDto>? pages,
            Dictionary<string, object?> fieldValues)
        {
            var guidToName = new Dictionary<string, string>();
            var guidToType = new Dictionary<string, FieldType?>();
            var requiredFieldGuids = new HashSet<string>();
            var visibleFieldGuids = new HashSet<string>();
            if (pages != null)
            {
                foreach (var page in pages)
                {
                    foreach (var item in page.Items)
                    {
                        if (item is FieldInputDto fieldInput && fieldInput.DefinitionID != null && !string.IsNullOrWhiteSpace(fieldInput.FieldName))
                        {
                            var guid = fieldInput.DefinitionID.ToString()!;
                            guidToName[guid] = fieldInput.FieldName;
                            guidToType[guid] = fieldInput.FieldType;
                            if (fieldInput.Mandatory == true)
                                requiredFieldGuids.Add(guid);
                            if (!fieldInput.IsHidden)
                                visibleFieldGuids.Add(guid);
                        }
                    }
                }
            }
            var nameKeyed = new Dictionary<string, object?>();
            // Always include visible fields and required fields (even if hidden)
            var allRelevantGuids = visibleFieldGuids.Union(requiredFieldGuids);
            foreach (var guid in allRelevantGuids)
            {
                string? fieldName = null;
                FieldType? fieldType = null;
                if (guidToName.TryGetValue(guid, out var n) && !string.IsNullOrWhiteSpace(n))
                    fieldName = n;
                if (guidToType.TryGetValue(guid, out var t))
                    fieldType = t;
                object? value = fieldValues.TryGetValue(guid, out var v) ? v : null;
                if (fieldType == FieldType.Dropdown && value is DropdownOptionDto dto)
                {
                    if (dto.Properties.TryGetValue("Full_Name", out var fullNameVal) && fullNameVal != null)
                        value = fullNameVal;
                    else
                        value = dto.Properties.Values.FirstOrDefault();
                }
                nameKeyed[fieldName ?? guid] = value;
            }
            return nameKeyed;
        }

        /// <summary>
        /// Sets default values for any hidden required fields that are missing or empty in fieldValues.
        /// For Checkbox, sets to false. For Dropdown, sets to first available option if possible.
        /// </summary>
        public static void SetDefaultsForHiddenRequiredFields(List<PageDto>? pages, Dictionary<string, object?> fieldValues)
        {
            if (pages == null) return;
            foreach (var page in pages)
            {
                foreach (var item in page.Items)
                {
                    if (item is FieldInputDto fieldInput && fieldInput.DefinitionID != null && fieldInput.Mandatory == true && fieldInput.IsHidden)
                    {
                        var guid = fieldInput.DefinitionID.ToString()!;
                        if (!fieldValues.TryGetValue(guid, out var value) || value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            switch (fieldInput.FieldType)
                            {
                                case FieldType.Checkbox:
                                    fieldValues[guid] = false;
                                    break;
                                case FieldType.Dropdown:
                                    // Use Lookup_Values if available
                                    object? dropdownDefault = null;
                                    if (!string.IsNullOrWhiteSpace(fieldInput.Lookup_Values))
                                    {
                                        var options = fieldInput.Lookup_Values.Split(';').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
                                        if (options.Count > 0)
                                            dropdownDefault = options[0];
                                    }
                                    fieldValues[guid] = dropdownDefault;
                                    break;
                                default:
                                    fieldValues[guid] = string.Empty;
                                    break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Maps a FieldInputDto to a FormFieldDto for use in FieldInput components.
        /// </summary>
        public static FormFieldDto MapToFormFieldDto(FieldInputDto input)
        {
            return new FormFieldDto
            {
                Field_Num = input.Field_Num,
                Field_Label = input.FieldLabel,
                Field_Name = input.FieldName,
                ID = input.DefinitionID ?? Guid.Empty,
                Virtual = input.Virtual,
                Mandatory = input.Mandatory,
                Read_Only = input.Read_Only,
                LookupValues = input.Lookup_Values,
                TableName = input.Table_Name,
                LookupID = input.Lookup_ID,
                Filter = input.Filter,
                FieldType = input.FieldType,
                DisplayFields = input.DisplayFields
            };
        }

        /// <summary>
        /// Extracts text from an XML element definition, or strips tags if not valid XML.
        /// </summary>
        public static string GetTextValue(string? elementDefinition)
        {
            if (string.IsNullOrWhiteSpace(elementDefinition))
                return string.Empty;
            try
            {
                var doc = XDocument.Parse(elementDefinition);
                var item = doc.Descendants("ITEM")
                              .FirstOrDefault(e => (string?)e.Attribute("Name") == "Text");
                return item?.Attribute("Value")?.Value ?? string.Empty;
            }
            catch
            {
                return Regex.Replace(elementDefinition, "<.*?>", string.Empty).Trim();
            }
        }

        /// <summary>
        /// Extracts text and URL from an XML element definition, for text/attachment rendering.
        /// </summary>
        public static (string? Text, string? Url) GetTextAndUrl(string? elementDefinition)
        {
            if (string.IsNullOrWhiteSpace(elementDefinition))
                return (null, null);
            try
            {
                var doc = XDocument.Parse(elementDefinition);
                var textItem = doc.Descendants("ITEM").FirstOrDefault(e => (string?)e.Attribute("Name") == "Text");
                var urlItem = doc.Descendants("ITEM").FirstOrDefault(e => (string?)e.Attribute("Name") == "URL");
                var text = textItem?.Attribute("Value")?.Value;
                var url = urlItem?.Attribute("Value")?.Value;
                return (text, url);
            }
            catch
            {
                return (null, null);
            }
        }
    }
}
