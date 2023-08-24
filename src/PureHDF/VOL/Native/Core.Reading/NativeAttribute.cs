﻿using PureHDF.Selections;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PureHDF.VOL.Native;

internal class NativeAttribute : IH5Attribute
{
    #region Fields

    private IH5Dataspace? _space;
    private IH5DataType? _type;
    private readonly NativeReadContext _context;

    #endregion

    #region Constructors

    internal NativeAttribute(NativeReadContext context, AttributeMessage message)
    {
        _context = context;
        Message = message;

        InternalElementDataType = Message.Datatype.Properties.FirstOrDefault() switch
        {
            ArrayPropertyDescription array => array.BaseType,
            _ => Message.Datatype
        };
    }

    #endregion

    #region Properties

    public string Name => Message.Name;

    public IH5Dataspace Space
    {
        get
        {
            _space ??= new NativeDataspace(Message.Dataspace);

            return _space;
        }
    }

    public IH5DataType Type
    {
        get
        {
            _type ??= new NativeDataType(Message.Datatype);

            return _type;
        }
    }

    #endregion

    #region Methods

    public T[] Read<T>()
        where T : unmanaged
    {
        switch (Message.Datatype.Class)
        {
            case DatatypeMessageClass.FixedPoint:
            case DatatypeMessageClass.FloatingPoint:
            case DatatypeMessageClass.BitField:
            case DatatypeMessageClass.Opaque:
            case DatatypeMessageClass.Compound:
            case DatatypeMessageClass.Reference:
            case DatatypeMessageClass.Enumerated:
            case DatatypeMessageClass.Array:
                break;

            default:
                throw new Exception($"This method can only be used with one of the following type classes: '{DatatypeMessageClass.FixedPoint}', '{DatatypeMessageClass.FloatingPoint}', '{DatatypeMessageClass.BitField}', '{DatatypeMessageClass.Opaque}', '{DatatypeMessageClass.Compound}', '{DatatypeMessageClass.Reference}', '{DatatypeMessageClass.Enumerated}' and '{DatatypeMessageClass.Array}'.");
        }

        var buffer = Message.InputData;
        var byteOrderAware = Message.Datatype.BitField as IByteOrderAware;
        var destination = buffer;
        var source = destination.ToArray();

        if (byteOrderAware is not null)
            DataUtils.EnsureEndianness(source, destination.Span, byteOrderAware.ByteOrder, Message.Datatype.Size);

        return MemoryMarshal
            .Cast<byte, T>(Message.InputData.Span)
            .ToArray();
    }

    public T[] ReadCompound<T>(Func<FieldInfo, string?>? getName = default)
        where T : struct
    {
        getName ??= fieldInfo => fieldInfo.Name;

        var elementCount = Message.InputData.Length / InternalElementDataType.Size;
        var result = new T[elementCount];

        ReadUtils.ReadCompound<T>(_context, InternalElementDataType, Message.InputData.Span, result, getName);

        return result;
    }

    public Dictionary<string, object?>[] ReadCompound()
    {
        var elementCount = Message.InputData.Length / InternalElementDataType.Size;
        var result = new Dictionary<string, object?>[elementCount];

        ReadUtils.ReadCompound(_context, InternalElementDataType, Message.InputData.Span, result);

        return result;
    }

    public string[] ReadString()
    {
        return ReadUtils.ReadString(_context, InternalElementDataType, Message.InputData.Span);
    }

    public T[]?[] ReadVariableLength<T>(
        Selection? fileSelection = null, 
        Selection? memorySelection = null, 
        ulong[]? memoryDims = null) where T : struct
    {
        var elementCount = Message.InputData.Length / InternalElementDataType.Size;
        var result = new T[elementCount][];

        ReadUtils.ReadVariableLengthSequence<T>(_context, InternalElementDataType, Message.InputData.Span, result);

        return result;
    }


    internal AttributeMessage Message { get; }

    internal DatatypeMessage InternalElementDataType { get; }

    #endregion
}