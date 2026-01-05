using System;
using System.Data;
using Dapper;

namespace Jellyfin.Plugin.Jellydash.TypeMappers;

/// <summary>
/// Dapper type handler for mapping SQLite TEXT values to nullable <see cref="Guid"/>.
/// </summary>
public sealed class NullableGuidTypeHandler : SqlMapper.TypeHandler<Guid?>
{
    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, Guid? value)
    {
        if (value.HasValue)
        {
            parameter.DbType = DbType.String;
            parameter.Value = value.Value.ToString();
        }
        else
        {
            parameter.Value = DBNull.Value;
        }
    }

    /// <inheritdoc />
    public override Guid? Parse(object value)
    {
        if (value is null || value is DBNull)
        {
            return null;
        }

        return value switch
        {
            Guid g => g,
            string s when !string.IsNullOrWhiteSpace(s) => Guid.Parse(s),
            byte[] bytes when bytes.Length == 16 => new Guid(bytes),
            _ => throw new DataException($"Cannot convert {value.GetType()} to Guid?.")
        };
    }
}
