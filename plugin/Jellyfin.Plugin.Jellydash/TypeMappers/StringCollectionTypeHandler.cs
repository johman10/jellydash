using System;
using System.Collections.ObjectModel;
using System.Data;
using Dapper;

namespace Jellyfin.Plugin.Jellydash.TypeMappers;

/// <summary>
/// Dapper type handler that serializes <see cref="Collection{T}"/> of <see cref="string"/>
/// to a single TEXT column and parses it back.
/// </summary>
public sealed class StringCollectionTypeHandler : SqlMapper.TypeHandler<Collection<string>?>
{
    private const char Separator = ',';

    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, Collection<string>? value)
    {
        if (value is null || value.Count == 0)
        {
            parameter.Value = DBNull.Value;
            return;
        }

        parameter.DbType = DbType.String;
        parameter.Value = string.Join(Separator, value);
    }

    /// <inheritdoc />
    public override Collection<string> Parse(object value)
    {
        var result = new Collection<string>();

        if (value is null || value is DBNull)
        {
            return result;
        }

        if (value is string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return result;
            }

            foreach (var part in s.Split(Separator, StringSplitOptions.RemoveEmptyEntries))
            {
                result.Add(part);
            }

            return result;
        }

        throw new DataException($"Cannot convert {value.GetType()} to Collection<string>.");
    }
}
