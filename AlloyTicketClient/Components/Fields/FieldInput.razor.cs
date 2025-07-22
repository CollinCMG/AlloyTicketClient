using AlloyTicketClient.Enums;
using AlloyTicketClient.Models.DTOs;
using AlloyTicketClient.Services;
using Microsoft.AspNetCore.Components;

namespace AlloyTicketClient.Components.Fields
{
    public partial class FieldInput : ComponentBase
    {
        // PARAMETERS
        [Parameter] public FormFieldDto? Field { get; set; }
        [Parameter] public object? Value { get; set; }
        [Parameter] public EventCallback<object?> OnValueChanged { get; set; }

        // INJECTED SERVICES
        [Inject] private FormFieldService FormFieldService { get; set; } = default!;

        // PROPERTIES/FIELDS
        private List<DropdownOptionDto>? Options;
        private List<string> SelectedMultiValues
        {
            get
            {
                if (Value is List<string> list)
                    return list;
                if (Value is string s && !string.IsNullOrWhiteSpace(s))
                    return s.Split(';').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
                return new List<string>();
            }
            set
            {
                if (!Equals(Value, value))
                {
                    Value = value;
                    OnValueChanged.InvokeAsync(value);
                }
            }
        }
        private string? CurrentValue
        {
            get
            {
                // Prefer Value, but if null/empty, use Field.FieldValue
                var val = Value is DropdownOptionDto dto
                    ? dto.Properties.ContainsKey("Id") ? dto.Properties["Id"]?.ToString() : dto.Properties.Values.FirstOrDefault()?.ToString()
                    : Value?.ToString();
                if (string.IsNullOrEmpty(val) && Field != null && !string.IsNullOrEmpty(Field.Field_Value))
                {
                    return Field.Field_Value;
                }
                return val;
            }
            set
            {
                if (Field?.FieldType == FieldType.Dropdown && Options != null)
                {
                    var selected = Options.FirstOrDefault(o => (o.Properties.ContainsKey("Id") ? o.Properties["Id"]?.ToString() : o.Properties.Values.FirstOrDefault()?.ToString()) == value);
                    if (selected != null && !Equals(Value, selected))
                    {
                        Value = selected;
                        OnValueChanged.InvokeAsync(selected);
                    }
                    else if (string.IsNullOrEmpty(value))
                    {
                        Value = null;
                        OnValueChanged.InvokeAsync(null);
                    }
                }
                else
                {
                    if (!Equals(Value, value))
                    {
                        Value = value;
                        OnValueChanged.InvokeAsync(value);
                    }
                }
            }
        }

        private bool IsChecked
        {
            get => Value?.ToString() == "true" || Value?.ToString() == "True";
            set
            {
                var strValue = value ? "true" : "false";
                if (!Equals(Value, strValue))
                {
                    Value = strValue;
                    OnValueChanged.InvokeAsync(strValue);
                }
            }
        }

        private DateTime? DateValue
        {
            get
            {
                if (DateTime.TryParse(Value?.ToString(), out var dt))
                    return dt;
                return null;
            }
            set
            {
                var strValue = value.HasValue ? value.Value.ToString("yyyy-MM-dd") : null;
                if (!Equals(Value, strValue))
                {
                    Value = strValue;
                    OnValueChanged.InvokeAsync(strValue);
                }
            }
        }

        // LIFECYCLE METHODS
        protected override async Task OnInitializedAsync()
        {
            await LoadDropdownOptions();
        }

        // EVENT HANDLERS
        private void OnMultiSelectChanged(ChangeEventArgs e)
        {
            if (e.Value is not null)
            {
                var selected = new List<string>();
                if (e.Value is string single)
                {
                    selected.Add(single);
                }
                else if (e.Value is IEnumerable<string> many)
                {
                    selected.AddRange(many);
                }
                else if (e.Value is string[] arr)
                {
                    selected.AddRange(arr);
                }
                else if (e.Value is IEnumerable<object> objArr)
                {
                    selected.AddRange(objArr.Select(x => x?.ToString() ?? string.Empty));
                }
                SelectedMultiValues = selected;
            }
            else
            {
                SelectedMultiValues = new List<string>();
            }
        }

        private void OnRadioChanged(string option)
        {
            if (!Equals(Value, option))
            {
                Value = option;
                OnValueChanged.InvokeAsync(option);
            }
        }

        private void OnDropdownChanged(ChangeEventArgs e)
        {
            var selectedValue = e.Value?.ToString();
            if (Options != null)
            {
                var selected = Options.FirstOrDefault(o => (o.Properties.ContainsKey("Id") ? o.Properties["Id"]?.ToString() : o.Properties.Values.FirstOrDefault()?.ToString()) == selectedValue);
                Value = selected;
                OnValueChanged.InvokeAsync(selected);
            }
        }

        // HELPER/UTILITY METHODS
        private async Task LoadDropdownOptions()
        {
            if (Field?.FieldType == FieldType.Dropdown && !string.IsNullOrWhiteSpace(Field.TableName) && !string.IsNullOrWhiteSpace(Field.DisplayFields))
            {
                Options = await FormFieldService.GetDropdownOptionsAsync(Field);
            }
            else
            {
                Options = null;
            }
        }

        private (string? value, string? display) GetDropdownValueAndDisplay(DropdownOptionDto option)
        {
            var value = option.Properties.ContainsKey("Id") ? option.Properties["Id"]?.ToString() : option.Properties.Values.FirstOrDefault()?.ToString();
            var display = option.Properties.ContainsKey("Display_Name") ? option.Properties["Display_Name"]?.ToString() : option.Properties.Values.FirstOrDefault()?.ToString();
            return (value, display);
        }

        private string GetDisplayName(FormFieldDto field)
            => string.IsNullOrWhiteSpace(field.Field_Label) ? field.Field_Name ?? string.Empty : field.Field_Label;
       
        // Parses lookup values for radio/select fields. Example: 1,"No,Yes","No,Yes" or 2,"A,B,C"
        private (int lookupType, List<string> options) ParseLookupTypeAndValues(string lookupValues)
        {
            var parts = lookupValues.Split(',', 2);
            if (parts.Length < 2 || !int.TryParse(parts[0], out var type))
            {
                // fallback: treat as type 2 (dropdown) and parse as before
                return (2, FormDataMapperService.ParseLookupValues(lookupValues).ToList());
            }
            var rest = parts[1];
            var options = new List<string>();
            
            // Try to extract quoted string(s)
            var quoteStart = rest.IndexOf('"');
            var quoteEnd = rest.LastIndexOf('"');
            if (quoteStart >= 0 && quoteEnd > quoteStart)
            {
                var quoted = rest.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                options = quoted.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                    .Select(x => x.Replace("\"", string.Empty)).ToList();
            }
            else
            {
                options = rest.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                    .Select(x => x.Replace("\"", string.Empty)).ToList();
            }
            return (type, options.Distinct().ToList());
        }

        private Dictionary<string, object> GetRequiredAttribute()
        {
            return Field?.Mandatory == true ? new Dictionary<string, object> { { "required", true } } : new Dictionary<string, object>();
        }
    }
}
