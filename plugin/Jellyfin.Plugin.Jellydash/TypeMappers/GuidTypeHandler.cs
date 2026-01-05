using System;
using System.Data;
using Dapper;

namespace Jellyfin.Plugin.Jellydash.TypeMappers;

/// <summary>
/// Dapper type handler for mapping SQLite TEXT values to <see cref="Guid"/>.
/// </summary>
public sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value.ToString();
    }

    /// <inheritdoc />
    public override Guid Parse(object value)
    {
        return value switch
        {
            Guid g => g,
            string s when !string.IsNullOrWhiteSpace(s) => Guid.Parse(s),
            byte[] bytes when bytes.Length == 16 => new Guid(bytes),
            null => throw new DataException("Cannot convert null to Guid."),
            _ => throw new DataException($"Cannot convert {value.GetType()} to Guid.")
        };
    }
}
