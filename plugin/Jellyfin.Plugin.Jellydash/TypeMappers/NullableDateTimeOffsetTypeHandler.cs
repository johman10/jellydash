using System;
using System.Data;
using System.Globalization;
using Dapper;

namespace Jellyfin.Plugin.Jellydash.TypeMappers;

/// <summary>
/// Dapper type handler for mapping SQLite TEXT values to nullable <see cref="DateTimeOffset"/>.
/// </summary>
public sealed class NullableDateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset?>
{
    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset? value)
    {
        if (value.HasValue)
        {
            parameter.DbType = DbType.String;
            parameter.Value = value.Value.ToString("O", CultureInfo.InvariantCulture);
        }
        else
        {
            parameter.Value = DBNull.Value;
        }
    }

    /// <inheritdoc />
    public override DateTimeOffset? Parse(object value)
    {
        return value switch
        {
            DateTimeOffset dto => dto,
            string s when !string.IsNullOrWhiteSpace(s) => DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            DateTime dt => new DateTimeOffset(dt),
            null => null,
            DBNull => null,
            _ => throw new DataException($"Cannot convert {value.GetType()} to DateTimeOffset?.")
        };
    }
}
