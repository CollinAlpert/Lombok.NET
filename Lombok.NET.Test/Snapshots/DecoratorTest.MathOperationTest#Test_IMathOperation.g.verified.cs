﻿//HintName: Test_IMathOperation.g.cs
// <auto-generated/>
using Lombok.NET;

namespace Test;
#nullable enable
public class MathOperationDecorator : IMathOperation
{
    private readonly IMathOperation _mathOperation;
    public MathOperationDecorator(IMathOperation mathOperation)
    {
        _mathOperation = mathOperation;
    }

    public virtual int Execute(int val)
    {
        return _mathOperation.Execute(val);
    }
}