using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

public class FormFieldService
{
    private readonly AlloyNavigatorDbContext _db;

    public FormFieldService(AlloyNavigatorDbContext db)
    {
        _db = db;
    }

    public async Task<List<FormFieldDto>> GetFormFieldsAsync(Guid formId)
    {
        var sql = @"
    SELECT
        d.ID,
        f.Id AS Field_Id,
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
        f.Field_Type AS FieldType
    FROM
        cfgLCFormDefinition d
    LEFT JOIN
        cfgLCFormFields f
        ON REPLACE(REPLACE(d.Field_Name, '{{', ''), '}}', '') = REPLACE(REPLACE(f.ID, '{{', ''), '}}', '')
    WHERE d.Form_ID = @formId
    ORDER BY d.Field_NUM";
        var param = new SqlParameter("@formId", formId);
        return await _db.FormFieldResults.FromSqlRaw(sql, param).ToListAsync();
    }
}
