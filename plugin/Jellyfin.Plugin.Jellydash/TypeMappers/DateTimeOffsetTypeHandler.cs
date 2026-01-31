using System;
using System.Data;
using System.Globalization;
using Dapper;

namespace Jellyfin.Plugin.Jellydash.TypeMappers;

/// <summary>
/// Dapper type handler for mapping SQLite TEXT values to <see cref="DateTimeOffset"/>.
/// </summary>
public sealed class DateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value.ToString("O", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override DateTimeOffset Parse(object value)
    {
        return value switch
        {
            DateTimeOffset dto => dto,
            string s when !string.IsNullOrWhiteSpace(s) => DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            DateTime dt => new DateTimeOffset(dt),
            null => throw new DataException("Cannot convert null to DateTimeOffset."),
            _ => throw new DataException($"Cannot convert {value.GetType()} to DateTimeOffset.")
        };
    }
}
