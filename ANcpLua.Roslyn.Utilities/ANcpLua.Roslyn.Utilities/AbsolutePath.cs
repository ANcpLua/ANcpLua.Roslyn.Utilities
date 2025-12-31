using System.Diagnostics;
using Meziantou.Framework;

namespace ANcpLua.Roslyn.Utilities;

[DebuggerDisplay("{Inner}")]
public readonly record struct AbsolutePath(FullPath Inner) : IComparable<AbsolutePath>
{
    public string Value => Inner.Value;
    public bool IsEmpty => Inner.IsEmpty;
    public string Name => Inner.Name;
    public string Stem => Inner.NameWithoutExtension;
    public string Extension => Inner.Extension;
    public bool Exists => File.Exists(Inner) || Directory.Exists(Inner);
    public AbsolutePath? Parent => Inner.Parent.IsEmpty ? null : new(Inner.Parent);

    public static AbsolutePath Parse(string path) => new(FullPath.FromPath(path));

    public static explicit operator AbsolutePath(string path) => Parse(path);
    public static implicit operator string(AbsolutePath path) => path.Value;
    public static implicit operator FullPath(AbsolutePath path) => path.Inner;

    public static AbsolutePath operator /(AbsolutePath left, string right) => new(left.Inner / right);

    public int CompareTo(AbsolutePath other) => Inner.CompareTo(other.Inner);
    public override string ToString() => Value;
}