using System.Text;

namespace ConfigTableGenerator;

internal static class BinaryConfigWriter
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public static byte[] Write(ConfigOutputModel model)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Utf8, true);
        writer.Write(new byte[] { (byte)'G', (byte)'C', (byte)'F', (byte)'G' });
        writer.Write(1U);
        writer.Write(SchemaHash.Compute(model));
        writer.Write(checked((uint)model.Rows.Count));
        foreach (LogicalRow row in model.Rows)
        {
            for (int i = 0; i < model.Fields.Count; i++)
            {
                WriteValue(writer, model.Fields[i].Type, row.Values[i]);
            }
        }
        return stream.ToArray();
    }

    private static void WriteValue(BinaryWriter writer, ConfigType type, object value)
    {
        if (type.Kind is ConfigTypeKind.Array or ConfigTypeKind.List)
        {
            object[] items = (object[])value;
            writer.Write(checked((uint)items.Length));
            foreach (object item in items) WriteValue(writer, type.ElementType!, item);
            return;
        }
        if (type.Kind == ConfigTypeKind.Custom)
        {
            object[] members = (object[])value;
            for (int i = 0; i < type.Members.Count; i++) WriteValue(writer, type.Members[i].Type, members[i]);
            return;
        }
        switch (type.Name)
        {
            case "uint": writer.Write((uint)value); break;
            case "int": writer.Write((int)value); break;
            case "bool": writer.Write((byte)((bool)value ? 1 : 0)); break;
            case "string": WriteString(writer, (string)value); break;
            case "float": writer.Write((float)value); break;
            case "long": writer.Write((long)value); break;
            case "double": writer.Write((double)value); break;
            default: throw new InvalidDataException($"不支持的字段类型：{type.Name}");
        }
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        byte[] data = Utf8.GetBytes(value);
        writer.Write(checked((uint)data.Length));
        writer.Write(data);
    }
}
