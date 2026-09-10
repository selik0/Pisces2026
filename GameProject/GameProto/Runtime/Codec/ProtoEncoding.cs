using System.Text;

namespace GameProto
{
    internal static class ProtoEncoding
    {
        public static readonly Encoding Utf8 = new UTF8Encoding(false, true);
    }
}
