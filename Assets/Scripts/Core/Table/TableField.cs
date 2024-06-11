using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 最终代码的类型
/// </summary>
public enum TableBaseType : byte
{
    Null,
    Byte,
    Short,
    Int,
    Float,
    Bool,
    String,
    ULong,
}

/// <summary>
/// 二进制存储类型
/// </summary>
public enum AttrType
{
    Null,
    Zero,
    SByte,
    Short,
    Int,
    Long,
    ULong,
    Byte,
    UShort,
    UInt,
}

public class TableField
{
    public const String BYTE = "byte";
    public const String SHORT = "short";
    public const String INT = "int";
    public const String FLOAT = "float";
    public const String BOOL = "bool";
    public const String STRING = "string";
    public const String ULONG = "ulong";

    public const String BeTranslate = "translate";
    public const String KEY = "key";
    public const String DEL = "del";

    public TableBaseType fieldType;
}
