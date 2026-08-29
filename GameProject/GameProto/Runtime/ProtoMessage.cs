using System;

namespace GameProto
{
    /// <summary>
    /// 特性驱动的 protobuf 消息基类。
    /// 子类用 <see cref="ProtoFieldAttribute"/> 标注公开字段或属性即可自动完成序列化与反序列化。
    /// 需要精细控制的消息可覆写 <see cref="WriteTo"/>、<see cref="ComputeSize"/>、
    /// <see cref="MergeFrom(ProtoReader)"/> 手工实现，三个方法必须一起覆写，保证长度计算一致。
    /// </summary>
    public abstract class ProtoMessage : IProtoMessage
    {
        /// <inheritdoc />
        public virtual void WriteTo(ProtoWriter writer)
        {
            ProtoCodec.WriteMessageFields(this, writer);
        }

        /// <inheritdoc />
        public virtual int ComputeSize()
        {
            return ProtoCodec.ComputeMessageSize(this);
        }

        /// <inheritdoc />
        public virtual void MergeFrom(ProtoReader reader)
        {
            ProtoCodec.MergeMessageFields(this, reader);
        }

        /// <summary>序列化为字节数组。</summary>
        public byte[] ToByteArray()
        {
            ProtoWriter writer = new ProtoWriter();
            WriteTo(writer);
            return writer.ToArray();
        }

        /// <summary>将字节数组解析并合并进当前实例（遵循 protobuf 合并语义）。</summary>
        public void MergeFrom(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            ProtoReader reader = new ProtoReader(data);
            MergeFrom(reader);
        }

        /// <summary>将字节数组解析并合并进当前实例（与 <see cref="MergeFrom(byte[])"/> 等价）。</summary>
        public void ParseFrom(byte[] data)
        {
            MergeFrom(data);
        }

        /// <summary>从字节数组创建新实例并解析。</summary>
        public static T FromByteArray<T>(byte[] data) where T : ProtoMessage, new()
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            T message = new T();
            message.MergeFrom(data);
            return message;
        }

        /// <summary>从字节数组创建新实例并解析（与 <see cref="FromByteArray{T}"/> 等价）。</summary>
        public static T ParseFrom<T>(byte[] data) where T : ProtoMessage, new()
        {
            return FromByteArray<T>(data);
        }

        /// <summary>
        /// 通过序列化往返复制当前实例；要求目标类型存在公开无参构造。
        /// </summary>
        public ProtoMessage Clone()
        {
            ProtoMessage copy = (ProtoMessage)Activator.CreateInstance(GetType());
            copy.MergeFrom(ToByteArray());
            return copy;
        }
    }
}
