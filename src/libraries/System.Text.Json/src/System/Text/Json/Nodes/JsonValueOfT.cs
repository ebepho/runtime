﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Nodes
{
    [DebuggerDisplay("{ToJsonString(),nq}")]
    [DebuggerTypeProxy(typeof(JsonValue<>.DebugView))]
    internal abstract class JsonValue<TValue> : JsonValue
    {
        internal readonly TValue Value; // keep as a field for direct access to avoid copies

        protected JsonValue(TValue value, JsonNodeOptions? options = null) : base(options)
        {
            Debug.Assert(value != null);
            Debug.Assert(value is not JsonElement or JsonElement { ValueKind: not JsonValueKind.Null });

            if (value is JsonNode)
            {
                ThrowHelper.ThrowArgumentException_NodeValueNotAllowed(nameof(value));
            }

            Value = value;
        }

        public override T GetValue<T>()
        {
            // If no conversion is needed, just return the raw value.
            if (Value is T returnValue)
            {
                return returnValue;
            }

            if (Value is JsonElement)
            {
                return ConvertJsonElement<T>();
            }

            // Currently we do not support other conversions.
            // Generics (and also boxing) do not support standard cast operators say from 'long' to 'int',
            //  so attempting to cast here would throw InvalidCastException.
            throw new InvalidOperationException(SR.Format(SR.NodeUnableToConvert, Value!.GetType(), typeof(T)));
        }

        public override bool TryGetValue<T>([NotNullWhen(true)] out T value)
        {
            // If no conversion is needed, just return the raw value.
            if (Value is T returnValue)
            {
                value = returnValue;
                return true;
            }

            if (Value is JsonElement)
            {
                return TryConvertJsonElement<T>(out value);
            }

            // Currently we do not support other conversions.
            // Generics (and also boxing) do not support standard cast operators say from 'long' to 'int',
            //  so attempting to cast here would throw InvalidCastException.
            value = default!;
            return false;
        }

        private protected sealed override JsonValueKind GetValueKindCore()
        {
            if (Value is JsonElement element)
            {
                return element.ValueKind;
            }

            Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(default, JsonSerializerOptions.BufferSizeDefault, out PooledByteBufferWriter output);
            try
            {
                WriteTo(writer);
                writer.Flush();
                return JsonElement.ParseValue(output.WrittenMemory.Span, options: default).ValueKind;
            }
            finally
            {
                Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, output);
            }
        }

        internal sealed override bool DeepEqualsCore(JsonNode? otherNode)
        {
            if (otherNode is null)
            {
                return false;
            }

            if (Value is JsonElement thisElement && otherNode is JsonValue<JsonElement> { Value: JsonElement otherElement })
            {
                if (thisElement.ValueKind != otherElement.ValueKind)
                {
                    return false;
                }

                switch (thisElement.ValueKind)
                {
                    case JsonValueKind.Null:
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return true;

                    case JsonValueKind.String:
                        return thisElement.ValueEquals(otherElement.GetString());
                    case JsonValueKind.Number:
                        return thisElement.GetRawValue().Span.SequenceEqual(otherElement.GetRawValue().Span);
                    default:
                        Debug.Fail("Object and Array JsonElements cannot be contained in JsonValue.");
                        return false;
                }
            }

            using PooledByteBufferWriter thisOutput = WriteToPooledBuffer(this);
            using PooledByteBufferWriter otherOutput = WriteToPooledBuffer(otherNode);
            return thisOutput.WrittenMemory.Span.SequenceEqual(otherOutput.WrittenMemory.Span);

            static PooledByteBufferWriter WriteToPooledBuffer(
                JsonNode node,
                JsonSerializerOptions? options = null,
                JsonWriterOptions writerOptions = default,
                int bufferSize = JsonSerializerOptions.BufferSizeDefault)
            {
                var bufferWriter = new PooledByteBufferWriter(bufferSize);
                using var writer = new Utf8JsonWriter(bufferWriter, writerOptions);
                node.WriteTo(writer, options);
                return bufferWriter;
            }
        }

        internal TypeToConvert ConvertJsonElement<TypeToConvert>()
        {
            JsonElement element = (JsonElement)(object)Value!;

            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    if (typeof(TypeToConvert) == typeof(int) || typeof(TypeToConvert) == typeof(int?))
                    {
                        return (TypeToConvert)(object)element.GetInt32();
                    }

                    if (typeof(TypeToConvert) == typeof(long) || typeof(TypeToConvert) == typeof(long?))
                    {
                        return (TypeToConvert)(object)element.GetInt64();
                    }

                    if (typeof(TypeToConvert) == typeof(double) || typeof(TypeToConvert) == typeof(double?))
                    {
                        return (TypeToConvert)(object)element.GetDouble();
                    }

                    if (typeof(TypeToConvert) == typeof(short) || typeof(TypeToConvert) == typeof(short?))
                    {
                        return (TypeToConvert)(object)element.GetInt16();
                    }

                    if (typeof(TypeToConvert) == typeof(decimal) || typeof(TypeToConvert) == typeof(decimal?))
                    {
                        return (TypeToConvert)(object)element.GetDecimal();
                    }

                    if (typeof(TypeToConvert) == typeof(byte) || typeof(TypeToConvert) == typeof(byte?))
                    {
                        return (TypeToConvert)(object)element.GetByte();
                    }

                    if (typeof(TypeToConvert) == typeof(float) || typeof(TypeToConvert) == typeof(float?))
                    {
                        return (TypeToConvert)(object)element.GetSingle();
                    }

                    if (typeof(TypeToConvert) == typeof(uint) || typeof(TypeToConvert) == typeof(uint?))
                    {
                        return (TypeToConvert)(object)element.GetUInt32();
                    }

                    if (typeof(TypeToConvert) == typeof(ushort) || typeof(TypeToConvert) == typeof(ushort?))
                    {
                        return (TypeToConvert)(object)element.GetUInt16();
                    }

                    if (typeof(TypeToConvert) == typeof(ulong) || typeof(TypeToConvert) == typeof(ulong?))
                    {
                        return (TypeToConvert)(object)element.GetUInt64();
                    }

                    if (typeof(TypeToConvert) == typeof(sbyte) || typeof(TypeToConvert) == typeof(sbyte?))
                    {
                        return (TypeToConvert)(object)element.GetSByte();
                    }
                    break;

                case JsonValueKind.String:
                    if (typeof(TypeToConvert) == typeof(string))
                    {
                        return (TypeToConvert)(object)element.GetString()!;
                    }

                    if (typeof(TypeToConvert) == typeof(DateTime) || typeof(TypeToConvert) == typeof(DateTime?))
                    {
                        return (TypeToConvert)(object)element.GetDateTime();
                    }

                    if (typeof(TypeToConvert) == typeof(DateTimeOffset) || typeof(TypeToConvert) == typeof(DateTimeOffset?))
                    {
                        return (TypeToConvert)(object)element.GetDateTimeOffset();
                    }

                    if (typeof(TypeToConvert) == typeof(Guid) || typeof(TypeToConvert) == typeof(Guid?))
                    {
                        return (TypeToConvert)(object)element.GetGuid();
                    }

                    if (typeof(TypeToConvert) == typeof(char) || typeof(TypeToConvert) == typeof(char?))
                    {
                        string? str = element.GetString();
                        Debug.Assert(str != null);
                        if (str.Length == 1)
                        {
                            return (TypeToConvert)(object)str[0];
                        }
                    }
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    if (typeof(TypeToConvert) == typeof(bool) || typeof(TypeToConvert) == typeof(bool?))
                    {
                        return (TypeToConvert)(object)element.GetBoolean();
                    }
                    break;
            }

            throw new InvalidOperationException(SR.Format(SR.NodeUnableToConvertElement,
                element.ValueKind,
                typeof(TypeToConvert)));
        }

        internal bool TryConvertJsonElement<TypeToConvert>([NotNullWhen(true)] out TypeToConvert result)
        {
            bool success;

            JsonElement element = (JsonElement)(object)Value!;

            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    if (typeof(TypeToConvert) == typeof(int) || typeof(TypeToConvert) == typeof(int?))
                    {
                        success = element.TryGetInt32(out int value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(long) || typeof(TypeToConvert) == typeof(long?))
                    {
                        success = element.TryGetInt64(out long value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(double) || typeof(TypeToConvert) == typeof(double?))
                    {
                        success = element.TryGetDouble(out double value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(short) || typeof(TypeToConvert) == typeof(short?))
                    {
                        success = element.TryGetInt16(out short value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(decimal) || typeof(TypeToConvert) == typeof(decimal?))
                    {
                        success = element.TryGetDecimal(out decimal value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(byte) || typeof(TypeToConvert) == typeof(byte?))
                    {
                        success = element.TryGetByte(out byte value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(float) || typeof(TypeToConvert) == typeof(float?))
                    {
                        success = element.TryGetSingle(out float value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(uint) || typeof(TypeToConvert) == typeof(uint?))
                    {
                        success = element.TryGetUInt32(out uint value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(ushort) || typeof(TypeToConvert) == typeof(ushort?))
                    {
                        success = element.TryGetUInt16(out ushort value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(ulong) || typeof(TypeToConvert) == typeof(ulong?))
                    {
                        success = element.TryGetUInt64(out ulong value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(sbyte) || typeof(TypeToConvert) == typeof(sbyte?))
                    {
                        success = element.TryGetSByte(out sbyte value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }
                    break;

                case JsonValueKind.String:
                    if (typeof(TypeToConvert) == typeof(string))
                    {
                        string? strResult = element.GetString();
                        Debug.Assert(strResult != null);
                        result = (TypeToConvert)(object)strResult;
                        return true;
                    }

                    if (typeof(TypeToConvert) == typeof(DateTime) || typeof(TypeToConvert) == typeof(DateTime?))
                    {
                        success = element.TryGetDateTime(out DateTime value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(DateTimeOffset) || typeof(TypeToConvert) == typeof(DateTimeOffset?))
                    {
                        success = element.TryGetDateTimeOffset(out DateTimeOffset value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(Guid) || typeof(TypeToConvert) == typeof(Guid?))
                    {
                        success = element.TryGetGuid(out Guid value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(char) || typeof(TypeToConvert) == typeof(char?))
                    {
                        string? str = element.GetString();
                        Debug.Assert(str != null);
                        if (str.Length == 1)
                        {
                            result = (TypeToConvert)(object)str[0];
                            return true;
                        }
                    }
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    if (typeof(TypeToConvert) == typeof(bool) || typeof(TypeToConvert) == typeof(bool?))
                    {
                        result = (TypeToConvert)(object)element.GetBoolean();
                        return true;
                    }
                    break;
            }

            result = default!;
            return false;
        }

        [ExcludeFromCodeCoverage] // Justification = "Design-time"
        [DebuggerDisplay("{Json,nq}")]
        private sealed class DebugView
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public JsonValue<TValue> _node;

            public DebugView(JsonValue<TValue> node)
            {
                _node = node;
            }

            public string Json => _node.ToJsonString();
            public string Path => _node.GetPath();
            public TValue? Value => _node.Value;
        }
    }
}
