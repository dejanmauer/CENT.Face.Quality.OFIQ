// Polyfill that enables 'record' types and 'init' setters on netstandard2.0 / .NET 4.x
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
