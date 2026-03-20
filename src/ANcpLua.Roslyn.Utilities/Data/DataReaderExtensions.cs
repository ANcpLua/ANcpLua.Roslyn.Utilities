using System.Data;
using System.Data.Common;
using System.IO;
using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Fluent, zero-allocation <see cref="IDataReader" /> extensions via <see cref="ColumnReader" />.
/// </summary>
/// <example>
///     <code>
/// while (reader.Read())
/// {
///     var name  = reader.Col("Name").AsString;
///     var age   = reader.Col("Age").AsInt32;
///     var score = reader.Col(2).GetDouble(0.0);
/// }
/// </code>
/// </example>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class DataReaderExtensions
{
    /// <summary>Access a column by ordinal.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ColumnReader Col(this IDataReader reader, int ordinal) => new(reader, ordinal);

    /// <summary>Access a column by name.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ColumnReader Col(this IDataReader reader, string name) => new(reader, reader.GetOrdinal(name));
}

/// <summary>
///     Zero-allocation ref struct for reading typed, null-safe values from a single <see cref="IDataRecord" /> column.
///     Nullable <c>AsXxx</c> properties return <c>null</c> on <see cref="DBNull" />;
///     <c>GetXxx(default)</c> methods return the caller-supplied fallback instead.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    readonly ref struct ColumnReader
{
    private readonly IDataRecord _reader;
    private readonly int _ordinal;

    /// <summary>Initializes a <see cref="ColumnReader" /> for the given column ordinal.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ColumnReader(IDataRecord reader, int ordinal)
    {
        _reader = reader;
        _ordinal = ordinal;
    }

    private bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _reader.IsDBNull(_ordinal);
    }

    // ── Scalars (nullable) ──────────────────────────────────────────────

    /// <summary>Column value as <see cref="string" />, or <c>null</c>.</summary>
    public string? AsString
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNull ? null : _reader.GetString(_ordinal);
    }

    /// <summary>Column value as <see cref="ReadOnlySpan{T}" /> of <see cref="char" />, or empty.</summary>
    public ReadOnlySpan<char> Text
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNull ? default : _reader.GetString(_ordinal).AsSpan();
    }

    /// <summary>Column value as <see cref="int" />, or <c>null</c>.</summary>
    public int? AsInt32
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNull ? null : _reader.GetInt32(_ordinal);
    }

    /// <summary>Column value as <see cref="byte" />, or <c>null</c>.</summary>
    public byte? AsByte
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNull ? null : _reader.GetByte(_ordinal);
    }

    /// <summary>Column value as <see cref="sbyte" /> (unchecked narrowing from byte), or <c>null</c>.</summary>
    public sbyte? AsSByte
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNull ? null : unchecked((sbyte)_reader.GetByte(_ordinal));
    }

    /// <summary>Column value as <see cref="long" />, or <c>null</c>.</summary>
    public long? AsInt64
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNull ? null : _reader.GetInt64(_ordinal);
    }

    /// <summary>
    ///     Column value as <see cref="ulong" />, or <c>null</c>.
    ///     Reads via <see cref="decimal" /> to handle providers that store unsigned 64-bit values that way.
    /// </summary>
    public ulong? AsUInt64
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNull
            ? null
            : _reader is DbDataReader db
                ? (ulong)db.GetFieldValue<decimal>(_ordinal)
                : (ulong)Convert.ToDecimal(_reader.GetValue(_ordinal), CultureInfo.InvariantCulture);
    }

    /// <summary>Column value as <see cref="double" />, or <c>null</c>.</summary>
    public double? AsDouble
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNull ? null : _reader.GetDouble(_ordinal);
    }

    /// <summary>Column value as <see cref="decimal" />, or <c>null</c>.</summary>
    public decimal? AsDecimal
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNull ? null : _reader.GetDecimal(_ordinal);
    }

    /// <summary>Column value as <see cref="float" /> (narrowed from double), or <c>null</c>.</summary>
    public float? AsFloat
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNull ? null : (float)_reader.GetDouble(_ordinal);
    }

    /// <summary>Column value as <see cref="bool" />, or <c>null</c>.</summary>
    public bool? AsBool
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNull ? null : _reader.GetBoolean(_ordinal);
    }

    /// <summary>Column value as <see cref="DateTime" />, or <c>null</c>.</summary>
    public DateTime? AsDateTime
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNull ? null : _reader.GetDateTime(_ordinal);
    }

    /// <summary>Column value as <see cref="DateTimeOffset" /> (UTC offset), or <c>null</c>.</summary>
    public DateTimeOffset? AsDateTimeOffset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNull ? null : new DateTimeOffset(_reader.GetDateTime(_ordinal), TimeSpan.Zero);
    }

    /// <summary>Column value as <see cref="Guid" />, or <c>null</c>.</summary>
    public Guid? AsGuid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNull ? null : _reader.GetGuid(_ordinal);
    }

    // ── Collections ─────────────────────────────────────────────────────

    /// <summary>Column value as <see cref="IReadOnlyList{T}" />, or <c>null</c>. Provider must materialize arrays as CLR lists.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<T>? AsList<T>() => IsNull ? null : _reader.GetValue(_ordinal) as IReadOnlyList<T>;

    /// <summary>Column value as <see cref="IReadOnlyDictionary{TKey, TValue}" />, or <c>null</c>. Provider must materialize maps as CLR dictionaries.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyDictionary<TKey, TValue>? AsMap<TKey, TValue>() where TKey : notnull =>
        IsNull ? null : _reader.GetValue(_ordinal) as IReadOnlyDictionary<TKey, TValue>;

    // ── Binary / Stream ─────────────────────────────────────────────────

    /// <summary>Column value as byte array, or <c>null</c>.</summary>
    public byte[]? AsBytes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNull ? null : _reader.GetValue(_ordinal) as byte[];
    }

    /// <summary>
    ///     Column value as <see cref="Stream" />. Returns <see cref="Stream.Null" /> on <see cref="DBNull" />.
    ///     Uses <see cref="DbDataReader.GetStream" /> when available; falls back to wrapping byte arrays.
    /// </summary>
    /// <exception cref="InvalidOperationException">Column is neither a stream nor a byte array.</exception>
    public Stream AsStream
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (IsNull) return Stream.Null;

            if (_reader is DbDataReader db)
                return db.GetStream(_ordinal);

            var value = _reader.GetValue(_ordinal);
            return value switch
            {
                Stream s => s,
                byte[] bytes => new MemoryStream(bytes, false),
                _ => throw new InvalidOperationException(
                    $"Column {_ordinal} is not a binary/stream column (was {value.GetType().FullName}).")
            };
        }
    }

    // ── Scalars with default fallback ───────────────────────────────────

    /// <summary>String value, or <paramref name="defaultValue" /> on null.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetString(string defaultValue) => IsNull ? defaultValue : _reader.GetString(_ordinal);

    /// <summary>Int32 value, or <paramref name="defaultValue" /> on null.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetInt32(int defaultValue) => IsNull ? defaultValue : _reader.GetInt32(_ordinal);

    /// <summary>Byte value, or <paramref name="defaultValue" /> on null.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetByte(byte defaultValue) => IsNull ? defaultValue : _reader.GetByte(_ordinal);

    /// <summary>SByte value (unchecked from byte), or <paramref name="defaultValue" /> on null.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte GetSByte(sbyte defaultValue) => IsNull ? defaultValue : unchecked((sbyte)_reader.GetByte(_ordinal));

    /// <summary>Int64 value, or <paramref name="defaultValue" /> on null.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetInt64(long defaultValue) => IsNull ? defaultValue : _reader.GetInt64(_ordinal);

    /// <summary>UInt64 value (via decimal conversion), or <paramref name="defaultValue" /> on null.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong GetUInt64(ulong defaultValue) => IsNull
        ? defaultValue
        : _reader is DbDataReader db
            ? (ulong)db.GetFieldValue<decimal>(_ordinal)
            : (ulong)Convert.ToDecimal(_reader.GetValue(_ordinal), CultureInfo.InvariantCulture);

    /// <summary>Double value, or <paramref name="defaultValue" /> on null.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetDouble(double defaultValue) => IsNull ? defaultValue : _reader.GetDouble(_ordinal);

    /// <summary>Decimal value, or <paramref name="defaultValue" /> on null.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal GetDecimal(decimal defaultValue) => IsNull ? defaultValue : _reader.GetDecimal(_ordinal);

    /// <summary>Float value (narrowed from double), or <paramref name="defaultValue" /> on null.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetFloat(float defaultValue) => IsNull ? defaultValue : (float)_reader.GetDouble(_ordinal);

    /// <summary>Bool value, or <paramref name="defaultValue" /> on null.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetBool(bool defaultValue) => IsNull ? defaultValue : _reader.GetBoolean(_ordinal);

    /// <summary>DateTime value, or <paramref name="defaultValue" /> on null.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime GetDateTime(DateTime defaultValue) => IsNull ? defaultValue : _reader.GetDateTime(_ordinal);

    /// <inheritdoc />
    public override string ToString()
    {
        if (IsNull) return "NULL";
        try
        {
            return _reader.GetValue(_ordinal).ToString() ?? "NULL";
        }
        catch
        {
            return "Err";
        }
    }
}
