using System.Security.Cryptography;
using System.Text;

namespace ConfigTableGenerator;

internal static class SchemaHash
{
    public static byte[] Compute(ConfigOutputModel model)
    {
        StringBuilder schema = new();
        schema.Append("version=1;target=").Append(model.Target).Append(";class=").Append(model.ClassName);
        schema.Append(";key=").Append(model.KeyFieldName).Append(";fields=");
        foreach (LogicalField field in model.Fields)
        {
            AppendType(schema.Append(field.Name).Append(':'), field.Type);
            schema.Append(';');
        }
        return SHA256.HashData(Encoding.UTF8.GetBytes(schema.ToString()))[..8];
    }

    private static void AppendType(StringBuilder schema, ConfigType type)
    {
        schema.Append(type.Kind).Append('(').Append(type.Name);
        if (type.ElementType != null)
        {
            schema.Append(',');
            AppendType(schema, type.ElementType);
        }
        foreach (ConfigMember member in type.Members)
        {
            schema.Append(',').Append(member.Name).Append(':');
            AppendType(schema, member.Type);
        }
        schema.Append(')');
    }
}
