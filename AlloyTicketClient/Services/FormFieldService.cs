using AlloyTicketClient.Enums;
using AlloyTicketClient.Models;
using AlloyTicketClient.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Data.Common;

public class FormFieldService
{
    private readonly AlloyNavigatorDbContext _db;
    private readonly ConcurrentDictionary<string, List<DropdownOptionDto>> _dropdownOptionsCache = new();

    public FormFieldService(AlloyNavigatorDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// Gets the FormId for a given objectId.
    /// </summary>
    public async Task<Guid> GetFormIdByObjectId(string objectId)
    {
        if (string.IsNullOrWhiteSpace(objectId))
            return Guid.Empty;

        var sql = @"SELECT e.Form_ID FROM cfgLCEvents e INNER JOIN cfgLCActionList al ON e.EventID = al.EventID INNER JOIN Service_Request_Fulfillment_List fl ON fl.Request_Create_Action_ID = al.id INNER JOIN Service_Catalog_Item_List cil ON fl.ID = cil.Request_Fulfillment_ID WHERE OID = @ObjId";
        var connString = _db.Database.GetConnectionString();
        using (var connection = new SqlConnection(connString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Parameters.Add(new SqlParameter("@ObjId", objectId));
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var formIdObj = reader["Form_ID"];
                        if (formIdObj != DBNull.Value && Guid.TryParse(formIdObj.ToString(), out var formId))
                            return formId;
                    }
                }
            }
        }
        return Guid.Empty;
    }

    public async Task<Guid> GetFormIdByActionId(int? actionId)
    {
        if (actionId == null)
            return Guid.Empty;

        var sql = @"SELECT e.Form_ID FROM   alloynavigator.dbo.cfgLCActionList  al
 INNER JOIN cfgLCEvents e
    ON al.ID = e.ID
Where al.EventID = @ActionId
";

        var connString = _db.Database.GetConnectionString();
        using (var connection = new SqlConnection(connString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Parameters.Add(new SqlParameter("@ActionId", actionId));
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var formIdObj = reader["Form_ID"];
                        if (formIdObj != DBNull.Value && Guid.TryParse(formIdObj.ToString(), out var formId))
                            return formId;
                    }
                }
            }
        }
        return Guid.Empty;
    }

    /// <summary>
    /// Gets all form pages and their items for a given formId.
    /// </summary>
    public async Task<List<PageDto>> GetFormPagesAsync(Guid formId)
    {
        var sql = @"
WITH PageBreaks AS (
    SELECT
        e.Field_ID      AS PageFieldID,
        e.Name          AS PageName,
        e.Form_ID       AS Form_ID,
        e.Rank          AS PageRank,
        fd.Field_Num    AS StartFieldNum,
        LEAD(e.Rank, 1, 999999) OVER (ORDER BY e.Rank) AS NextPageRank
    FROM cfgLCFormElements e
    LEFT JOIN cfgLCFormDefinition fd
        ON REPLACE(REPLACE(e.Field_ID, '{', ''), '}', '') = REPLACE(REPLACE(fd.ID, '{', ''), '}', '')
    WHERE e.Type = 0
      AND e.Form_ID = @FormId
),
FieldAssignments AS (
    SELECT
        d.ID            AS DefinitionID,
        d.Field_Num     AS Field_Num,
        CASE
            WHEN d.Virtual = 0 THEN d.Field_Name
            ELSE f.Field_Caption
        END             AS Field_Name,
        d.Field_Label   AS Field_Label,
        d.Field_Value   AS Field_Value,
        d.Form_ID       AS Form_ID,
        f.Field_Caption AS Field_Caption,
        pb.PageName     AS PageName,
        pb.PageRank     AS PageRank,
        NULL            AS ElementType,
        d.Field_Num     AS SortOrder,
        NULL            AS ElementDefinition,
        f.Field_Type    AS FieldType,
        d.Mandatory     AS Mandatory,
        d.Read_Only     AS ReadOnly,
        Lookup_Values   AS Lookup_Values,
        CASE 
            WHEN f.Table_Name = 'Persons' THEN 'Person_List'
            WHEN f.Table_Name = 'Organizational_Units' THEN 'Organizational_Unit_List'
            ELSE f.Table_Name
        END             AS Table_Name,
        Virtual         AS Virtual,
        ct.Display_Fields AS Display_Fields,
        Filter          AS Filter
    FROM cfgLCFormDefinition d
    LEFT JOIN cfgLCFormFields f
        ON REPLACE(REPLACE(d.Field_Name, '{', ''), '}', '') = REPLACE(REPLACE(f.ID, '{', ''), '}', '')
    LEFT JOIN cfgCustTables ct
        ON f.Table_Name = ct.Table_Name
    OUTER APPLY (
        SELECT TOP 1 pb2.PageName, pb2.PageRank
        FROM PageBreaks pb2
        WHERE pb2.Form_ID = d.Form_ID
          AND pb2.StartFieldNum <= d.Field_Num
        ORDER BY pb2.StartFieldNum DESC
    ) pb
    WHERE d.Form_ID = @FormId
),
ElementAssignments AS (
    SELECT
        e.ID            AS DefinitionID,
        fd.Field_Num    AS Field_Num,
        e.Name          AS Field_Name,
        NULL            AS Field_Label,
        NULL            AS Field_Value,
        e.Form_ID       AS Form_ID,
        NULL            AS Field_Caption,
        pb.PageName     AS PageName,
        pb.PageRank     AS PageRank,
        e.Type          AS ElementType,
        fd.Field_Num    AS SortOrder,
        e.Definition    AS ElementDefinition,
        NULL            AS FieldType,
        NULL            AS Mandatory,
        NULL            AS ReadOnly,
        NULL            AS Lookup_Values,
        NULL            AS Table_Name,
        NULL            AS Virtual,
        NULL            AS Display_Fields,
        NULL            AS Filter
    FROM cfgLCFormElements e
    LEFT JOIN cfgLCFormDefinition fd
        ON REPLACE(REPLACE(e.Field_ID, '{', ''), '}', '') = REPLACE(REPLACE(fd.ID, '{', ''), '}', '')
    OUTER APPLY (
        SELECT TOP 1 pb2.PageName, pb2.PageRank, pb2.NextPageRank
        FROM PageBreaks pb2
        WHERE pb2.Form_ID = e.Form_ID
          AND pb2.PageRank <= e.Rank
          AND e.Rank < pb2.NextPageRank
        ORDER BY pb2.PageRank DESC
    ) pb
    WHERE e.Type = 1
      AND e.Form_ID = @FormId
)
SELECT
    PageName,
    PageRank,
    Field_Num,
    Field_Name,
    Field_Label,
    Field_Value,
    DefinitionID,
    ElementType,
    ElementDefinition,
    SortOrder,
    FieldType,
    Mandatory,
    ReadOnly,
    Lookup_Values      AS LookupValues,
    Table_Name         AS TableName,
    Virtual,
    Display_Fields     AS DisplayFields,
    Filter
FROM FieldAssignments

UNION ALL

SELECT
    PageName,
    PageRank,
    Field_Num,
    Field_Name,
    Field_Label,
    Field_Value,
    DefinitionID,
    ElementType,
    ElementDefinition,
    SortOrder,
    FieldType,
    Mandatory,
    ReadOnly,
    Lookup_Values      AS LookupValues,
    Table_Name         AS TableName,
    Virtual,
    Display_Fields     AS DisplayFields,
    Filter
FROM ElementAssignments

ORDER BY PageRank, SortOrder, DefinitionID
";
        var param = new SqlParameter("@FormId", formId);
        var results = new List<dynamic>();
        // Read all data into memory before further processing (no MARS required)
        using (var command = _db.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = sql;
            command.Parameters.Add(param);
            EnsureConnectionOpen(command);
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    results.Add(ReadFormPageRow(reader));
                }
            }
        }
        // All data is now in memory, safe to process
        var pages = results
            .GroupBy(r => new { r.PageName, r.PageRank })
            .Select(g => new PageDto
            {
                PageName = g.Key.PageName,
                PageRank = g.Key.PageRank,
                Items = g.OrderBy(x => x.SortOrder)
                    .Select(x => MapToPageItem(x))
                    .Where(i => i != null)
                    .Cast<IPageItem>()
                    .ToList()
            })
            .OrderBy(p => p.PageRank)
            .ToList();
        return pages;
    }

    /// <summary>
    /// Gets dropdown options for a given form field, using cache if available.
    /// </summary>
    public async Task<List<DropdownOptionDto>> GetDropdownOptionsAsync(FormFieldDto field)
    {
        if (string.IsNullOrWhiteSpace(field.TableName) || string.IsNullOrWhiteSpace(field.DisplayFields))
        {
            return new List<DropdownOptionDto>();
        }
        var cacheKey = $"{field.TableName}|{field.DisplayFields}|{field.Filter}";
        if (_dropdownOptionsCache.TryGetValue(cacheKey, out var cachedOptions))
        {
            return cachedOptions;
        }
        var sql = string.Empty;

        if (field.TableName == "Person_List")
        {
            sql = $"SELECT {field.DisplayFields + ", [Primary_Email]"} FROM [{field.TableName}]";
        }
        else
        {
            sql = $"SELECT {field.DisplayFields} FROM [{field.TableName}]";
        }

        if (!string.IsNullOrWhiteSpace(field.Filter))
            sql += $" WHERE {field.Filter}";
        var options = new List<DropdownOptionDto>();
        var connString = _db.Database.GetConnectionString();
        using (var connection = new SqlConnection(connString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var option = new DropdownOptionDto();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var name = reader.GetName(i);
                            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            option.Properties[name] = value;
                        }
                        options.Add(option);
                    }
                }
            }
        }
        _dropdownOptionsCache[cacheKey] = options;
        return options;
    }

    /// <summary>
    /// Ensures the database connection is open for a given command.
    /// </summary>
    private void EnsureConnectionOpen(DbCommand command)
    {
        if (command.Connection != null && command.Connection.State != System.Data.ConnectionState.Open)
            command.Connection.Open();
        else if (command.Connection == null)
            throw new InvalidOperationException("Database command connection is null.");
    }

    /// <summary>
    /// Reads a row from the form page query and returns a dynamic object.
    /// </summary>
    private dynamic ReadFormPageRow(DbDataReader reader)
    {
        return new
        {
            PageName = reader["PageName"]?.ToString(),
            PageRank = reader["PageRank"] != DBNull.Value ? Convert.ToInt32(reader["PageRank"]) : 0,
            FieldNum = reader["Field_Num"] != DBNull.Value ? (int?)Convert.ToInt32(reader["Field_Num"]) : null,
            FieldName = reader["Field_Name"]?.ToString(),
            FieldLabel = reader["Field_Label"]?.ToString(),
            FieldValue = reader["Field_Value"]?.ToString(),
            DefinitionID = reader["DefinitionID"] != DBNull.Value ? (Guid?)Guid.Parse(reader["DefinitionID"].ToString()) : null,
            ElementType = reader["ElementType"] != DBNull.Value ? (int?)Convert.ToInt32(reader["ElementType"]) : null,
            ElementDefinition = reader["ElementDefinition"]?.ToString(),
            SortOrder = reader["SortOrder"] != DBNull.Value ? Convert.ToInt32(reader["SortOrder"]) : 0,
            FieldType = reader["FieldType"] != DBNull.Value ? (FieldType?)Enum.ToObject(typeof(FieldType), reader["FieldType"]) : null,
            Mandatory = reader["Mandatory"] != DBNull.Value ? (bool?)Convert.ToBoolean(reader["Mandatory"]) : null,
            ReadOnly = reader["ReadOnly"] != DBNull.Value ? (bool?)Convert.ToBoolean(reader["ReadOnly"]) : null,
            LookupValues = reader["LookupValues"]?.ToString(),
            TableName = reader["TableName"]?.ToString(),
            Virtual = reader["Virtual"] != DBNull.Value ? (bool?)Convert.ToBoolean(reader["Virtual"]) : null,
            DisplayFields = reader["DisplayFields"]?.ToString(),
            Filter = reader["Filter"]?.ToString()
        };
    }

    /// <summary>
    /// Maps a dynamic row to an IPageItem (FieldInputDto or FieldTextDto).
    /// </summary>
    private IPageItem? MapToPageItem(dynamic x)
    {
        if (x.ElementType == null)
        {
            return new FieldInputDto
            {
                Field_Num = x.FieldNum,
                FieldLabel = x.FieldLabel,
                FieldValue = x.FieldValue,
                FieldName = x.FieldName,
                DefinitionID = x.DefinitionID,
                SortOrder = x.SortOrder,
                FieldType = x.FieldType,
                Mandatory = x.Mandatory,
                ReadOnly = x.ReadOnly,
                Lookup_Values = x.LookupValues,
                Table_Name = x.TableName,
                Virtual = x.Virtual,
                DisplayFields = x.DisplayFields,
                Filter = x.Filter
            };
        }
        else if (x.ElementType == 1)
        {
            return new FieldTextDto
            {
                ElementDefinition = x.ElementDefinition,
                SortOrder = x.SortOrder
            };
        }
        else
        {
            return null;
        }
    }
}
