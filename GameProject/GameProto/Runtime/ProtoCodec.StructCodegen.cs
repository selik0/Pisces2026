using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace GameProto
{
    /// <summary>
    /// 本文件是 <see cref="ProtoCodec"/> 的 partial 部分：struct 消息的"直写"反序列化。
    /// 首次使用时为每个 struct 类型用表达式树编译一个 (ref T, ProtoReader) 解析委托：
    /// 字段读取与赋值直接生成 IL，不经过反射、顶层不装箱；
    /// 嵌套 struct 字段通过装箱 + 接口调用路由到各自类型的直写委托。
    /// 表达式编译不可用的环境（如 IL2CPP/AOT）自动回退到反射路径。
    /// </summary>
    public static partial class ProtoCodec
    {
        /// <summary>
        /// struct 消息的解析入口，由 struct 的 MergeFrom 实现以 ref this 委托调用。
        /// 内部使用按类型缓存的直写解析委托（表达式树编译，字段直接赋值）；
        /// 编译不可用时自动回退反射路径，行为与 class 消息一致。
        /// </summary>
        public static void MergeStructFields<T>(ref T message, ProtoReader reader) where T : struct, IProtoMessage
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            StructMergeCache<T>.Value(ref message, reader);
        }

        /// <summary>struct 消息的解析委托：(ref T, ProtoReader)。</summary>
        private delegate void StructMergeDelegate<T>(ref T message, ProtoReader reader) where T : struct;

        /// <summary>按类型缓存的 struct 直写解析委托（泛型静态类，无锁、O(1) 访问）。</summary>
        private static class StructMergeCache<T> where T : struct, IProtoMessage
        {
            public static readonly StructMergeDelegate<T> Value = Build();

            private static StructMergeDelegate<T> Build()
            {
                try
                {
                    return CompileStructMerge<T>();
                }
                catch (Exception)
                {
                    // 表达式编译不可用（如 IL2CPP/AOT）时回退反射路径
                    return (ref T message, ProtoReader reader) =>
                    {
                        object box = message;
                        MergeMessageFields(box, reader);
                        message = (T)box;
                    };
                }
            }
        }

        /// <summary>从 ref 参数拷贝出值（供表达式树使用，避免直接对 byref 参数做字段访问）。</summary>
        private static T LoadRef<T>(ref T source)
        {
            return source;
        }

        /// <summary>把值写回 ref 参数（供表达式树使用）。</summary>
        private static void StoreRef<T>(ref T dest, T value)
        {
            dest = value;
        }

        /// <summary>为类型 T 编译 struct 直写解析委托。</summary>
        private static StructMergeDelegate<T> CompileStructMerge<T>() where T : struct
        {
            Type type = typeof(T);
            TypeDescriptor descriptor = GetDescriptor(type);

            ParameterExpression message = Expression.Parameter(type.MakeByRefType(), "message");
            ParameterExpression reader = Expression.Parameter(typeof(ProtoReader), "reader");
            ParameterExpression local = Expression.Variable(type, "m");
            ParameterExpression tag = Expression.Variable(typeof(uint), "tag");
            ParameterExpression fieldNumber = Expression.Variable(typeof(int), "fieldNumber");
            ParameterExpression wireType = Expression.Variable(typeof(ProtoWireType), "wireType");
            LabelTarget breakLabel = Expression.Label("break");

            var cases = new List<SwitchCase>();
            foreach (FieldDescriptor field in descriptor.Fields)
            {
                cases.Add(Expression.SwitchCase(
                    BuildFieldMergeBody(field, local, reader, tag, wireType),
                    Expression.Constant(field.FieldNumber, typeof(int))));
            }

            // while (true) { tag = reader.ReadTag(); if (tag == 0) break;
            //   fieldNumber = (int)(tag >> 3); wireType = (ProtoWireType)(tag & 7);
            //   switch (fieldNumber) { case n: ...; default: reader.SkipField(tag); } }
            Expression dispatch = Expression.Switch(fieldNumber,
                Expression.Call(reader, MergeMethods.SkipField, tag),
                cases.ToArray());

            Expression loopBody = Expression.Block(
                Expression.Assign(tag, Expression.Call(reader, MergeMethods.ReadTag)),
                Expression.IfThen(
                    Expression.Equal(tag, Expression.Constant(0u)),
                    Expression.Break(breakLabel)),
                Expression.Assign(fieldNumber, Expression.Convert(
                    Expression.RightShift(tag, Expression.Constant(3)), typeof(int))),
                Expression.Assign(wireType, Expression.Convert(
                    Expression.And(tag, Expression.Constant(7u)), typeof(ProtoWireType))),
                dispatch);

            Expression body = Expression.Block(
                new[] { local, tag, fieldNumber, wireType },
                Expression.Assign(local, Expression.Call(MergeMethods.LoadRef.MakeGenericMethod(type), message)),
                Expression.Loop(loopBody, breakLabel),
                Expression.Call(MergeMethods.StoreRef.MakeGenericMethod(type), message, local));

            return Expression.Lambda<StructMergeDelegate<T>>(body, message, reader).Compile();
        }

        /// <summary>为单个已知字段生成解析表达式（switch case 的 body）。</summary>
        private static Expression BuildFieldMergeBody(FieldDescriptor field, ParameterExpression local,
            ParameterExpression reader, ParameterExpression tag, ParameterExpression wireType)
        {
            Expression fieldExpr = GetFieldExpression(local, field);
            switch (field.Kind)
            {
                case FieldKind.Scalar:
                    return BuildSimpleField(field, fieldExpr, reader, wireType,
                        ReadScalarValueExpr(reader, field));
                case FieldKind.String:
                    return BuildSimpleField(field, fieldExpr, reader, wireType,
                        Expression.Call(reader, MergeMethods.ReadString));
                case FieldKind.Bytes:
                    return BuildSimpleField(field, fieldExpr, reader, wireType,
                        Expression.Call(reader, MergeMethods.ReadBytes));
                case FieldKind.Message:
                    if (field.ElementType.IsValueType)
                    {
                        return BuildStructMessageField(field, fieldExpr, reader, wireType);
                    }
                    return BuildClassMessageField(field, fieldExpr, reader, wireType);
                case FieldKind.Repeated:
                    return BuildRepeatedField(field, fieldExpr, reader, wireType);
                case FieldKind.Map:
                    return BuildMapField(field, fieldExpr, reader, wireType);
                default:
                    throw new ProtoProtocolException("无效的字段类型");
            }
        }

        /// <summary>单值字段：wire type 匹配则读取并直写，否则抛协议异常（与反射路径一致）。</summary>
        private static Expression BuildSimpleField(FieldDescriptor field, Expression fieldExpr,
            ParameterExpression reader, ParameterExpression wireType, Expression readValue)
        {
            return Expression.IfThenElse(
                Expression.Equal(wireType, Expression.Constant(field.WireType, typeof(ProtoWireType))),
                Expression.Assign(fieldExpr, readValue),
                Expression.Call(MergeMethods.EnsureWireType, wireType,
                    Expression.Constant(field.WireType, typeof(ProtoWireType))));
        }

        /// <summary>class 嵌套消息字段：为空则创建，然后在长度区域内合并。</summary>
        private static Expression BuildClassMessageField(FieldDescriptor field, Expression fieldExpr,
            ParameterExpression reader, ParameterExpression wireType)
        {
            Type type = field.ElementType;
            Expression createIfNull = Expression.IfThen(
                Expression.Equal(fieldExpr, Expression.Constant(null, type)),
                Expression.Assign(fieldExpr, Expression.New(type)));
            Expression merge = Expression.Call(reader, MergeMethods.ReadMessageInto,
                Expression.Convert(fieldExpr, typeof(IProtoMessage)));
            Expression body = Expression.Block(createIfNull, merge);
            return BuildWithWireTypeGuard(body, wireType, ProtoWireType.LengthDelimited);
        }

        /// <summary>
        /// struct 嵌套消息字段：GetValue 语义下拿到的只能是装箱副本，合并后必须写回。
        /// box 上的接口调用会路由到嵌套类型的直写解析委托。
        /// </summary>
        private static Expression BuildStructMessageField(FieldDescriptor field, Expression fieldExpr,
            ParameterExpression reader, ParameterExpression wireType)
        {
            Type type = field.ElementType;
            ParameterExpression box = Expression.Variable(typeof(IProtoMessage), "box");
            Expression body = Expression.Block(new[] { box },
                Expression.Assign(box, Expression.Convert(fieldExpr, typeof(IProtoMessage))),
                Expression.Call(reader, MergeMethods.ReadMessageInto, box),
                Expression.Assign(fieldExpr, Expression.Convert(box, type)));
            return BuildWithWireTypeGuard(body, wireType, ProtoWireType.LengthDelimited);
        }

        /// <summary>
        /// 重复字段：数值标量元素同时支持 packed（LengthDelimited）与逐个 tag 两种形式；
        /// string/bytes/消息元素只支持逐个 tag。
        /// </summary>
        private static Expression BuildRepeatedField(FieldDescriptor field, Expression fieldExpr,
            ParameterExpression reader, ParameterExpression wireType)
        {
            Type listType = typeof(List<>).MakeGenericType(field.ElementType);
            ParameterExpression list = Expression.Variable(listType, "list");
            MethodInfo addMethod = listType.GetMethod("Add");

            // list = field; if (list == null) { list = new List<T>(); field = list; }
            Expression init = Expression.Block(
                Expression.Assign(list, fieldExpr),
                Expression.IfThen(
                    Expression.Equal(list, Expression.Constant(null, listType)),
                    Expression.Block(
                        Expression.Assign(list, Expression.New(listType)),
                        Expression.Assign(fieldExpr, list))));

            Expression readSingle = Expression.Call(list, addMethod, BuildElementRead(field, reader));
            Expression single = Expression.Block(init, readSingle);

            if (field.ElementKind == FieldKind.Scalar)
            {
                // packed：reader.ReadLengthDelimited(inner => { while (!inner.IsAtEnd) list.Add(...); })
                ParameterExpression inner = Expression.Parameter(typeof(ProtoReader), "inner");
                LabelTarget packedBreak = Expression.Label("break");
                LabelTarget packedContinue = Expression.Label("continue");
                Expression rawAdd = Expression.Call(list, addMethod, ReadScalarValueExpr(inner, field));
                Expression packedLoop = Expression.Loop(
                    Expression.IfThenElse(
                        Expression.Not(Expression.Property(inner, "IsAtEnd")),
                        Expression.Block(rawAdd, Expression.Continue(packedContinue)),
                        Expression.Break(packedBreak)),
                    packedBreak,
                    packedContinue);
                Expression packed = Expression.Block(init,
                    Expression.Call(reader, MergeMethods.ReadLengthDelimited,
                        Expression.Lambda<Action<ProtoReader>>(packedLoop, inner)));

                return Expression.Block(new[] { list },
                    Expression.IfThenElse(
                        Expression.Equal(wireType, Expression.Constant(ProtoWireType.LengthDelimited, typeof(ProtoWireType))),
                        packed,
                        Expression.IfThenElse(
                            Expression.Equal(wireType, Expression.Constant(field.WireType, typeof(ProtoWireType))),
                            single,
                            Expression.Call(MergeMethods.EnsureWireType, wireType,
                                Expression.Constant(field.WireType, typeof(ProtoWireType))))));
            }

            return Expression.Block(new[] { list },
                Expression.IfThenElse(
                    Expression.Equal(wireType, Expression.Constant(ProtoWireType.LengthDelimited, typeof(ProtoWireType))),
                    single,
                    Expression.Call(MergeMethods.EnsureWireType, wireType,
                        Expression.Constant(ProtoWireType.LengthDelimited, typeof(ProtoWireType)))));
        }

        /// <summary>map 字段：为空则创建 Dictionary，然后委托 ReadMapEntry 解析条目。</summary>
        private static Expression BuildMapField(FieldDescriptor field, Expression fieldExpr,
            ParameterExpression reader, ParameterExpression wireType)
        {
            Type dictType = typeof(Dictionary<,>).MakeGenericType(field.MapKeyType, field.ElementType);
            ParameterExpression dict = Expression.Variable(typeof(IDictionary), "dict");

            Expression init = Expression.Block(
                Expression.Assign(dict, Expression.Convert(fieldExpr, typeof(IDictionary))),
                Expression.IfThen(
                    Expression.Equal(dict, Expression.Constant(null, typeof(IDictionary))),
                    Expression.Block(
                        Expression.Assign(dict, Expression.Convert(Expression.New(dictType), typeof(IDictionary))),
                        Expression.Assign(fieldExpr, Expression.Convert(dict, dictType)))));

            Expression body = Expression.Block(new[] { dict }, init,
                Expression.Call(MergeMethods.ReadMapEntry, reader, dict, Expression.Constant(field)));
            return BuildWithWireTypeGuard(body, wireType, ProtoWireType.LengthDelimited);
        }

        /// <summary>重复字段元素的单值读取（无 tag，调用方负责 tag/wire type 校验）。</summary>
        private static Expression BuildElementRead(FieldDescriptor field, ParameterExpression reader)
        {
            switch (field.ElementKind)
            {
                case FieldKind.Scalar:
                    return ReadScalarValueExpr(reader, field);
                case FieldKind.String:
                    return Expression.Call(reader, MergeMethods.ReadString);
                case FieldKind.Bytes:
                    return Expression.Call(reader, MergeMethods.ReadBytes);
                case FieldKind.Message:
                {
                    // 与反射路径一致：CreateMessage 得到实例（struct 为装箱默认值），
                    // ReadMessageInto 在实例上合并，最后解箱/转型返回，list.Add 时写回
                    ParameterExpression box = Expression.Variable(typeof(IProtoMessage), "box");
                    return Expression.Block(new[] { box },
                        Expression.Assign(box,
                            Expression.Call(MergeMethods.CreateMessage, Expression.Constant(field.ElementType))),
                        Expression.Call(reader, MergeMethods.ReadMessageInto, box),
                        Expression.Convert(box, field.ElementType));
                }
                default:
                    throw new ProtoProtocolException("无效的重复元素类型");
            }
        }

        /// <summary>标量值的直写读取表达式（按类型 + zigzag 选择 reader 方法）。</summary>
        private static Expression ReadScalarValueExpr(ParameterExpression reader, FieldDescriptor field)
        {
            Type type = field.ElementType;
            if (type == typeof(int))
            {
                return Expression.Call(reader, field.ZigZag ? MergeMethods.ReadSInt32 : MergeMethods.ReadInt32);
            }
            if (type == typeof(long))
            {
                return Expression.Call(reader, field.ZigZag ? MergeMethods.ReadSInt64 : MergeMethods.ReadInt64);
            }
            if (type == typeof(uint))
            {
                return Expression.Call(reader, MergeMethods.ReadUInt32);
            }
            if (type == typeof(ulong))
            {
                return Expression.Call(reader, MergeMethods.ReadUInt64);
            }
            if (type == typeof(bool))
            {
                return Expression.Call(reader, MergeMethods.ReadBool);
            }
            if (type == typeof(float))
            {
                return Expression.Call(reader, MergeMethods.ReadFloat);
            }
            if (type == typeof(double))
            {
                return Expression.Call(reader, MergeMethods.ReadDouble);
            }
            if (type.IsEnum)
            {
                return Expression.Convert(Expression.Call(reader, MergeMethods.ReadInt64), type);
            }
            throw new ProtoProtocolException("不支持的标量类型：" + type);
        }

        /// <summary>用 wire type 守卫包裹字段解析体：不匹配则抛协议异常。</summary>
        private static Expression BuildWithWireTypeGuard(Expression body, ParameterExpression wireType,
            ProtoWireType expected)
        {
            return Expression.IfThenElse(
                Expression.Equal(wireType, Expression.Constant(expected, typeof(ProtoWireType))),
                body,
                Expression.Call(MergeMethods.EnsureWireType, wireType,
                    Expression.Constant(expected, typeof(ProtoWireType))));
        }

        /// <summary>取字段/属性的访问表达式（local 为值类型局部变量，可写）。</summary>
        private static Expression GetFieldExpression(ParameterExpression local, FieldDescriptor field)
        {
            if (field.Field != null)
            {
                return Expression.Field(local, field.Field);
            }
            return Expression.Property(local, field.Property);
        }

        /// <summary>表达式树编译使用的方法引用表。</summary>
        private static class MergeMethods
        {
            public static readonly MethodInfo ReadTag =
                typeof(ProtoReader).GetMethod("ReadTag", Type.EmptyTypes);
            public static readonly MethodInfo SkipField =
                typeof(ProtoReader).GetMethod("SkipField", new[] { typeof(uint) });
            public static readonly MethodInfo ReadInt32 =
                typeof(ProtoReader).GetMethod("ReadInt32", Type.EmptyTypes);
            public static readonly MethodInfo ReadUInt32 =
                typeof(ProtoReader).GetMethod("ReadUInt32", Type.EmptyTypes);
            public static readonly MethodInfo ReadInt64 =
                typeof(ProtoReader).GetMethod("ReadInt64", Type.EmptyTypes);
            public static readonly MethodInfo ReadUInt64 =
                typeof(ProtoReader).GetMethod("ReadUInt64", Type.EmptyTypes);
            public static readonly MethodInfo ReadBool =
                typeof(ProtoReader).GetMethod("ReadBool", Type.EmptyTypes);
            public static readonly MethodInfo ReadSInt32 =
                typeof(ProtoReader).GetMethod("ReadSInt32", Type.EmptyTypes);
            public static readonly MethodInfo ReadSInt64 =
                typeof(ProtoReader).GetMethod("ReadSInt64", Type.EmptyTypes);
            public static readonly MethodInfo ReadFloat =
                typeof(ProtoReader).GetMethod("ReadFloat", Type.EmptyTypes);
            public static readonly MethodInfo ReadDouble =
                typeof(ProtoReader).GetMethod("ReadDouble", Type.EmptyTypes);
            public static readonly MethodInfo ReadString =
                typeof(ProtoReader).GetMethod("ReadString", Type.EmptyTypes);
            public static readonly MethodInfo ReadBytes =
                typeof(ProtoReader).GetMethod("ReadBytes", Type.EmptyTypes);
            public static readonly MethodInfo ReadMessageInto =
                typeof(ProtoReader).GetMethod("ReadMessageInto", new[] { typeof(IProtoMessage) });
            public static readonly MethodInfo ReadLengthDelimited =
                typeof(ProtoReader).GetMethod("ReadLengthDelimited", new[] { typeof(Action<ProtoReader>) });
            public static readonly MethodInfo EnsureWireType =
                typeof(ProtoCodec).GetMethod(nameof(EnsureWireType), BindingFlags.NonPublic | BindingFlags.Static);
            public static readonly MethodInfo ReadMapEntry =
                typeof(ProtoCodec).GetMethod(nameof(ReadMapEntry), BindingFlags.NonPublic | BindingFlags.Static);
            public static readonly MethodInfo CreateMessage =
                typeof(ProtoCodec).GetMethod(nameof(CreateMessage), BindingFlags.NonPublic | BindingFlags.Static);
            public static readonly MethodInfo LoadRef =
                typeof(ProtoCodec).GetMethod(nameof(LoadRef), BindingFlags.NonPublic | BindingFlags.Static);
            public static readonly MethodInfo StoreRef =
                typeof(ProtoCodec).GetMethod(nameof(StoreRef), BindingFlags.NonPublic | BindingFlags.Static);
        }
    }
}
