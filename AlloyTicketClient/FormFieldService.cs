using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Linq.Expressions;
using System.Collections.Concurrent;

public class FormFieldService
{
    private readonly AlloyNavigatorDbContext _db;
    // Add a thread-safe cache for dropdown options
    private readonly ConcurrentDictionary<string, List<string>> _dropdownOptionsCache = new();

    public FormFieldService(AlloyNavigatorDbContext db)
    {
        _db = db;
    }

    public async Task<List<FormFieldDto>> GetFormFieldsAsync(Guid formId)
    {
        var sql = @"
    SELECT
        d.ID,
        CASE
            WHEN d.Virtual = 0 THEN d.Field_Name
            ELSE f.Field_Caption
        END AS Field_Name,
        d.Field_Label,
        d.Field_Num,
        d.Virtual,
        d.Mandatory,
        d.Read_Only,
        f.Lookup_Values,
        f.Table_Name,
        f.Lookup_ID,
        d.Filter,
        f.Field_Type AS FieldType,
        ct.Display_Fields
    FROM
        cfgLCFormDefinition d
    LEFT JOIN
        cfgLCFormFields f
        ON REPLACE(REPLACE(d.Field_Name, '{{', ''), '}}', '') = REPLACE(REPLACE(f.ID, '{{', ''), '}}', '')
    LEFT JOIN cfgCustTables ct
        ON f.Table_Name = ct.Table_Name
    WHERE d.Form_ID = @formId
    ORDER BY d.Field_NUM";
        var param = new SqlParameter("@formId", formId);
        return await _db.FormFieldResults.FromSqlRaw(sql, param).ToListAsync();
    }

    public async Task GetDropdownOptionsAsync(FormFieldDto field)
    {
        if (string.IsNullOrWhiteSpace(field.Table_Name) || string.IsNullOrWhiteSpace(field.Display_Fields))
        {
            field.Options = new List<string>();
            return;
        }
        // Create a unique cache key for this field's dropdown options
        var cacheKey = $"{field.Table_Name}|{field.Display_Fields}|{field.Filter}";
        if (_dropdownOptionsCache.TryGetValue(cacheKey, out var cachedOptions))
        {
            field.Options = cachedOptions;
            return;
        }
        var sql = $"SELECT {field.Display_Fields} FROM [{field.Table_Name}]";
        if (!string.IsNullOrWhiteSpace(field.Filter))
            sql += $" WHERE {field.Filter}";
        var options = new List<string>();
        using (var command = _db.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = sql;
            if (command.Connection.State != System.Data.ConnectionState.Open)
                command.Connection.Open();
            try
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader.FieldCount > 0)
                        {
                            var values = new string[reader.FieldCount];
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                values[i] = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i).ToString();
                            }
                            options.Add(string.Join(" ", values).Trim());
                        }
                        else
                        {
                            options.Add(reader.IsDBNull(0) ? string.Empty : reader.GetValue(0).ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing SQL: {sql}");
                Console.WriteLine($"Exception: {ex}");
            }
            finally
            {
                if (command.Connection.State == System.Data.ConnectionState.Open)
                    command.Connection.Close();
            }
        }
        // Cache the options for future calls
        _dropdownOptionsCache[cacheKey] = options;
        field.Options = options;
    }
}
