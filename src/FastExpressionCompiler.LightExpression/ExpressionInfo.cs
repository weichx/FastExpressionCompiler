/*
The MIT License (MIT)

Copyright (c) 2016 Maksim Volkau

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included AddOrUpdateServiceFactory
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

// ReSharper disable CoVariantArrayConversion

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace FastExpressionCompiler.LightExpression
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    internal static class Tools
    {
        private static class EmptyArray<T>
        {
            public static readonly T[] Value = new T[0];
        }

        public static T[] Empty<T>() => EmptyArray<T>.Value;

        public static R[] Project<T, R>(this T[] source, Func<T, R> project)
        {
            if (source == null || source.Length == 0)
                return Empty<R>();

            if (source.Length == 1)
                return new[] { project(source[0]) };

            var result = new R[source.Length];
            for (var i = 0; i < result.Length; ++i)
                result[i] = project(source[i]);
            return result;
        }

        public static Type[] GetParamExprTypes(IList<ParameterExpressionInfo> paramExprs)
        {
            if (paramExprs == null || paramExprs.Count == 0)
                return Empty<Type>();

            if (paramExprs.Count == 1)
                return new[] { paramExprs[0].IsByRef ? paramExprs[0].Type.MakeByRefType() : paramExprs[0].Type };

            var paramTypes = new Type[paramExprs.Count];
            for (var i = 0; i < paramTypes.Length; i++)
            {
                var parameterExpr = paramExprs[i];
                paramTypes[i] = parameterExpr.IsByRef ? parameterExpr.Type.MakeByRefType() : parameterExpr.Type;
            }

            return paramTypes;
        }

        public static Type GetFuncOrActionType(Type[] paramTypes, Type returnType)
        {
            if (returnType == typeof(void))
            {
                switch (paramTypes.Length)
                {
                    case 0: return typeof(Action);
                    case 1: return typeof(Action<>).MakeGenericType(paramTypes);
                    case 2: return typeof(Action<,>).MakeGenericType(paramTypes);
                    case 3: return typeof(Action<,,>).MakeGenericType(paramTypes);
                    case 4: return typeof(Action<,,,>).MakeGenericType(paramTypes);
                    case 5: return typeof(Action<,,,,>).MakeGenericType(paramTypes);
                    case 6: return typeof(Action<,,,,,>).MakeGenericType(paramTypes);
                    case 7: return typeof(Action<,,,,,,>).MakeGenericType(paramTypes);
                    default:
                        throw new NotSupportedException(
                            string.Format("Action with so many ({0}) parameters is not supported!", paramTypes.Length));
                }
            }

            paramTypes = paramTypes.WithLast(returnType);
            switch (paramTypes.Length)
            {
                case 1: return typeof(Func<>).MakeGenericType(paramTypes);
                case 2: return typeof(Func<,>).MakeGenericType(paramTypes);
                case 3: return typeof(Func<,,>).MakeGenericType(paramTypes);
                case 4: return typeof(Func<,,,>).MakeGenericType(paramTypes);
                case 5: return typeof(Func<,,,,>).MakeGenericType(paramTypes);
                case 6: return typeof(Func<,,,,,>).MakeGenericType(paramTypes);
                case 7: return typeof(Func<,,,,,,>).MakeGenericType(paramTypes);
                case 8: return typeof(Func<,,,,,,,>).MakeGenericType(paramTypes);
                default:
                    throw new NotSupportedException(
                        string.Format("Func with so many ({0}) parameters is not supported!", paramTypes.Length));
            }
        }

        public static T[] WithLast<T>(this T[] source, T value)
        {
            if (source == null || source.Length == 0)
                return new[] { value };
            if (source.Length == 1)
                return new[] { source[0], value };
            if (source.Length == 2)
                return new[] { source[0], source[1], value };
            var sourceLength = source.Length;
            var result = new T[sourceLength + 1];
            Array.Copy(source, result, sourceLength);
            result[sourceLength] = value;
            return result;
        }
    }

    /// <summary>Facade for constructing expression info.</summary>
    public abstract class ExpressionInfo
    {
        /// <summary>Expression node type.</summary>
        public abstract ExpressionType NodeType { get; }

        /// <summary>All expressions should have a Type.</summary>
        public abstract Type Type { get; }

        /// <summary>Converts back to respective expression so you may Compile it by usual means.</summary>
        public abstract Expression ToExpression();

        /// <summary>Converts to Expression and outputs its as string</summary>
        public override string ToString() => ToExpression().ToString();

        /// <summary>Analog of Expression.Parameter</summary>
        /// <remarks>For now it is return just an `Expression.Parameter`</remarks>
        public static ParameterExpressionInfo Parameter(Type type, string name = null) =>
            new ParameterExpressionInfo(type.IsByRef ? type.GetElementType() : type, name, type.IsByRef);

        /// <summary>Analog of Expression.Constant</summary>
        public static ConstantExpressionInfo Constant(object value, Type type = null) =>
            value == null && type == null ? _nullExprInfo
                : new ConstantExpressionInfo(value, type ?? value.GetType());

        private static readonly ConstantExpressionInfo
            _nullExprInfo = new ConstantExpressionInfo(null, typeof(object));

        /// <summary>Analog of Expression.New</summary>
        public static NewExpressionInfo New(ConstructorInfo ctor) =>
            new NewExpressionInfo(ctor, Tools.Empty<ExpressionInfo>());

        /// <summary>Analog of Expression.New</summary>
        public static NewExpressionInfo New(ConstructorInfo ctor, params ExpressionInfo[] arguments) =>
            new NewExpressionInfo(ctor, arguments);

        /// <summary>Static method call</summary>
        public static MethodCallExpressionInfo Call(MethodInfo method, params ExpressionInfo[] arguments) =>
            new MethodCallExpressionInfo(null, method, arguments);

        /// <summary>Instance method call</summary>
        public static MethodCallExpressionInfo Call(
            ExpressionInfo instance, MethodInfo method, params ExpressionInfo[] arguments) =>
            new MethodCallExpressionInfo(instance, method, arguments);

        /// <summary>Static property</summary>
        public static PropertyExpressionInfo Property(PropertyInfo property) =>
            new PropertyExpressionInfo(null, property);

        /// <summary>Instance property</summary>
        public static PropertyExpressionInfo Property(ExpressionInfo instance, PropertyInfo property) =>
            new PropertyExpressionInfo(instance, property);

        /// <summary>Static field</summary>
        public static FieldExpressionInfo Field(FieldInfo field) =>
            new FieldExpressionInfo(null, field);

        /// <summary>Instance field</summary>
        public static FieldExpressionInfo Field(ExpressionInfo instance, FieldInfo field) =>
            new FieldExpressionInfo(instance, field);

        /// <summary>Analog of Expression.Lambda</summary>
        public static LambdaExpressionInfo Lambda(ExpressionInfo body) =>
            new LambdaExpressionInfo(null, body, Tools.Empty<ExpressionInfo>());

        /// <summary>Analog of Expression.Lambda</summary>
        public static LambdaExpressionInfo Lambda(ExpressionInfo body, params ParameterExpressionInfo[] parameters) =>
            new LambdaExpressionInfo(null, body, parameters);

        /// <summary>Analog of Expression.Lambda with lambda type specified</summary>
        public static LambdaExpressionInfo Lambda(Type delegateType, ExpressionInfo body, params ExpressionInfo[] parameters) =>
            new LambdaExpressionInfo(delegateType, body, parameters);

        /// <summary>Analog of Expression.Convert</summary>
        public static UnaryExpressionInfo Convert(ExpressionInfo operand, Type targetType) =>
            new UnaryExpressionInfo(ExpressionType.Convert, operand, targetType);

        /// <summary>Analog of Expression.Lambda</summary>
        public static ExpressionInfo<TDelegate> Lambda<TDelegate>(ExpressionInfo body) =>
            new ExpressionInfo<TDelegate>(body, Tools.Empty<ParameterExpressionInfo>());

        /// <summary>Analog of Expression.Lambda</summary>
        public static ExpressionInfo<TDelegate> Lambda<TDelegate>(ExpressionInfo body,
            params ParameterExpressionInfo[] parameters) =>
            new ExpressionInfo<TDelegate>(body, parameters);

        /// <summary>Analog of Expression.ArrayIndex</summary>
        public static BinaryExpressionInfo ArrayIndex(ExpressionInfo array, ExpressionInfo index) =>
            new ArrayIndexExpressionInfo(array, index, array.Type.GetElementType());

        /// <summary>Expression.Bind used in Expression.MemberInit</summary>
        public static MemberAssignmentInfo Bind(MemberInfo member, ExpressionInfo expression) =>
            new MemberAssignmentInfo(member, expression);

        /// <summary>Analog of Expression.MemberInit</summary>
        public static MemberInitExpressionInfo MemberInit(NewExpressionInfo newExpr,
            params MemberAssignmentInfo[] bindings) =>
            new MemberInitExpressionInfo(newExpr, bindings);

        /// <summary>Enables member assignment on existing instance expression.</summary>
        public static ExpressionInfo MemberInit(ExpressionInfo instanceExpr,
            params MemberAssignmentInfo[] assignments) =>
            new MemberInitExpressionInfo(instanceExpr, assignments);

        /// <summary>Constructs an array given the array type and item initializer expressions.</summary>
        public static NewArrayExpressionInfo NewArrayInit(Type type, params ExpressionInfo[] initializers) =>
            new NewArrayExpressionInfo(type, initializers);

        /// <summary>Constructs assignment expression.</summary>
        public static ExpressionInfo Assign(ExpressionInfo left, ExpressionInfo right) =>
            new AssignBinaryExpressionInfo(left, right, left.Type);

        /// <summary>Invoke</summary>
        public static ExpressionInfo Invoke(ExpressionInfo lambda, params ExpressionInfo[] args) =>
            new InvocationExpressionInfo(lambda, args, lambda.Type);

        /// <summary>Binary add</summary>
        public static ExpressionInfo Add(ExpressionInfo left, ExpressionInfo right) =>
            new ArithmeticBinaryExpressionInfo(ExpressionType.Add, left, right, left.Type);

        /// <summary>Binary substract</summary>
        public static ExpressionInfo Substract(ExpressionInfo left, ExpressionInfo right) =>
            new ArithmeticBinaryExpressionInfo(ExpressionType.Subtract, left, right, left.Type);

        public static ExpressionInfo Multiply(ExpressionInfo left, ExpressionInfo right) =>
            new ArithmeticBinaryExpressionInfo(ExpressionType.Multiply, left, right, left.Type);

        public static ExpressionInfo Divide(ExpressionInfo left, ExpressionInfo right) =>
            new ArithmeticBinaryExpressionInfo(ExpressionType.Divide, left, right, left.Type);

        public static BlockExpressionInfo Block(params ExpressionInfo[] expressions) =>
            new BlockExpressionInfo(expressions[expressions.Length - 1].Type,
                Tools.Empty<ParameterExpressionInfo>(), expressions);

        public static TryExpressionInfo TryCatch(ExpressionInfo body, params CatchBlockInfo[] handlers) =>
            new TryExpressionInfo(body, null, handlers);

        public static TryExpressionInfo TryCatchFinally(ExpressionInfo body, ExpressionInfo @finally, params CatchBlockInfo[] handlers) =>
            new TryExpressionInfo(body, @finally, handlers);

        public static TryExpressionInfo TryFinally(ExpressionInfo body, ExpressionInfo @finally) =>
            new TryExpressionInfo(body, @finally, null);

        public static CatchBlockInfo Catch(ParameterExpressionInfo variable, ExpressionInfo body) =>
            new CatchBlockInfo(variable, body, null, variable.Type);

        public static CatchBlockInfo Catch(Type test, ExpressionInfo body) =>
            new CatchBlockInfo(null, body, null, test);

        public static UnaryExpressionInfo Throw(ExpressionInfo value) =>
            new UnaryExpressionInfo(ExpressionType.Throw, value, typeof(void));
    }

    public class UnaryExpressionInfo : ExpressionInfo
    {
        public override ExpressionType NodeType { get; }
        public override Type Type { get; }

        public readonly ExpressionInfo Operand;

        public readonly MethodInfo Method;

        public override Expression ToExpression()
        {
            if (NodeType == ExpressionType.Convert)
                return Expression.Convert(Operand.ToExpression(), Type);
            throw new NotSupportedException("Cannot convert ExpressionInfo to Expression of type " + NodeType);
        }

        public UnaryExpressionInfo(ExpressionType nodeType, ExpressionInfo operand, Type type)
        {
            NodeType = nodeType;
            Operand = operand;
            Type = type;
        }

        public UnaryExpressionInfo(ExpressionType nodeType, ExpressionInfo operand, MethodInfo method)
        {
            NodeType = nodeType;
            Operand = operand;
            Method = method;
        }
    }

    public abstract class BinaryExpressionInfo : ExpressionInfo
    {
        public override ExpressionType NodeType { get; }
        public override Type Type { get; }

        public readonly ExpressionInfo Left, Right;

        protected BinaryExpressionInfo(ExpressionType nodeType, ExpressionInfo left, ExpressionInfo right, Type type)
        {
            NodeType = nodeType;
            Type = type;
            Left = left;
            Right = right;
        }
    }

    public class ArithmeticBinaryExpressionInfo : BinaryExpressionInfo
    {
        public ArithmeticBinaryExpressionInfo(ExpressionType nodeType, ExpressionInfo left, ExpressionInfo right, Type type)
            : base(nodeType, left, right, type) { }

        public override Expression ToExpression()
        {
            if (NodeType == ExpressionType.Add)
                return Expression.Add(Left.ToExpression(), Right.ToExpression());
            if (NodeType == ExpressionType.Subtract)
                return Expression.Subtract(Left.ToExpression(), Right.ToExpression());
            if (NodeType == ExpressionType.Multiply)
                return Expression.Multiply(Left.ToExpression(), Right.ToExpression());
            if (NodeType == ExpressionType.Divide)
                return Expression.Divide(Left.ToExpression(), Right.ToExpression());
            throw new NotSupportedException($"Not valid {NodeType} for arithmetic binary expression.");
        }
    }

    public class ArrayIndexExpressionInfo : BinaryExpressionInfo
    {
        public ArrayIndexExpressionInfo(ExpressionInfo left, ExpressionInfo right, Type type)
            : base(ExpressionType.ArrayIndex, left, right, type) { }

        public override Expression ToExpression() =>
            Expression.ArrayIndex(Left.ToExpression(), Right.ToExpression());
    }

    public class AssignBinaryExpressionInfo : BinaryExpressionInfo
    {
        public AssignBinaryExpressionInfo(ExpressionInfo left, ExpressionInfo right, Type type)
            : base(ExpressionType.Assign, left, right, type) { }

        public override Expression ToExpression() =>
            Expression.Assign(Left.ToExpression(), Right.ToExpression());
    }

    public class MemberInitExpressionInfo : ExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.MemberInit;
        public override Type Type => ExpressionInfo.Type;

        public NewExpressionInfo NewExpressionInfo => ExpressionInfo as NewExpressionInfo;

        public readonly ExpressionInfo ExpressionInfo;
        public readonly MemberAssignmentInfo[] Bindings;

        public override Expression ToExpression() =>
            Expression.MemberInit(NewExpressionInfo.ToNewExpression(),
                Bindings.Project(b => b.ToMemberAssignment()));

        public MemberInitExpressionInfo(NewExpressionInfo newExpressionInfo, MemberAssignmentInfo[] bindings)
            : this((ExpressionInfo)newExpressionInfo, bindings) { }

        public MemberInitExpressionInfo(ExpressionInfo expressionInfo, MemberAssignmentInfo[] bindings)
        {
            ExpressionInfo = expressionInfo;
            Bindings = bindings ?? Tools.Empty<MemberAssignmentInfo>();
        }
    }

    public class ParameterExpressionInfo : ExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.Parameter;
        public override Type Type { get; }

        public readonly string Name;

        public readonly bool IsByRef;

        public override Expression ToExpression() => ParamExpr;

        public ParameterExpression ParamExpr =>
            _parameter ?? (_parameter = Expression.Parameter(Type, Name));

        public static implicit operator ParameterExpression(ParameterExpressionInfo info) => info.ParamExpr;

        public ParameterExpressionInfo(Type type, string name, bool isByRef)
        {
            Type = type;
            Name = name;
            IsByRef = isByRef;
        }

        private ParameterExpression _parameter;
    }

    public class ConstantExpressionInfo : ExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.Constant;
        public override Type Type { get; }

        public readonly object Value;

        public override Expression ToExpression() => Expression.Constant(Value, Type);

        public ConstantExpressionInfo(object value, Type type)
        {
            Value = value;
            Type = type;
        }
    }

    public abstract class ArgumentsExpressionInfo : ExpressionInfo
    {
        public readonly ExpressionInfo[] Arguments;

        protected Expression[] ArgumentsToExpressions() => Arguments.Project(info => info.ToExpression());

        protected ArgumentsExpressionInfo(ExpressionInfo[] arguments)
        {
            Arguments = arguments ?? Tools.Empty<ExpressionInfo>();
        }
    }

    public class NewExpressionInfo : ArgumentsExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.New;
        public override Type Type => Constructor.DeclaringType;

        public readonly ConstructorInfo Constructor;

        public override Expression ToExpression() => ToNewExpression();

        public NewExpression ToNewExpression() => Expression.New(Constructor, ArgumentsToExpressions());

        public NewExpressionInfo(ConstructorInfo constructor, params ExpressionInfo[] arguments) : 
            base(arguments)
        {
            Constructor = constructor;
        }
    }

    public class NewArrayExpressionInfo : ArgumentsExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.NewArrayInit;
        public override Type Type { get; }

        // todo: That it is a ReadOnlyCollection<Expression> in original NewArrayExpression. 
        // I made it a ICollection for now to use Arguments as input, without changing Arguments type
        public ICollection<ExpressionInfo> Expressions => Arguments;

        public override Expression ToExpression() =>
            Expression.NewArrayInit(_elementType, ArgumentsToExpressions());

        public NewArrayExpressionInfo(Type elementType, ExpressionInfo[] elements) : base(elements)
        {
            Type = elementType.MakeArrayType();
            _elementType = elementType;
        }

        private readonly Type _elementType;
    }

    public class MethodCallExpressionInfo : ArgumentsExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.Call;
        public override Type Type => Method.ReturnType;

        public readonly MethodInfo Method;
        public readonly ExpressionInfo Object;

        public override Expression ToExpression() =>
            Expression.Call(Object?.ToExpression(), Method, ArgumentsToExpressions());

        public MethodCallExpressionInfo(ExpressionInfo @object, MethodInfo method, params ExpressionInfo[] arguments)
            : base(arguments)
        {
            Object = @object;
            Method = method;
        }
    }

    public abstract class MemberExpressionInfo : ExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.MemberAccess;
        public readonly MemberInfo Member;

        public readonly ExpressionInfo Expression;

        protected MemberExpressionInfo(ExpressionInfo expression, MemberInfo member)
        {
            Expression = expression;
            Member = member;
        }
    }

    public class PropertyExpressionInfo : MemberExpressionInfo
    {
        public override Type Type => PropertyInfo.PropertyType;
        public PropertyInfo PropertyInfo => (PropertyInfo)Member;

        public override Expression ToExpression() =>
            System.Linq.Expressions.Expression.Property(Expression.ToExpression(), PropertyInfo);

        public PropertyExpressionInfo(ExpressionInfo instance, PropertyInfo property)
            : base(instance, property) { }
    }

    public class FieldExpressionInfo : MemberExpressionInfo
    {
        public override Type Type => FieldInfo.FieldType;
        public FieldInfo FieldInfo => (FieldInfo)Member;

        public override Expression ToExpression() =>
            System.Linq.Expressions.Expression.Field(Expression.ToExpression(), FieldInfo);

        public FieldExpressionInfo(ExpressionInfo instance, FieldInfo field)
            : base(instance, field) { }
    }

    public struct MemberAssignmentInfo
    {
        public MemberInfo Member;
        public ExpressionInfo Expression;

        public MemberBinding ToMemberAssignment() =>
            System.Linq.Expressions.Expression.Bind(Member, Expression.ToExpression());

        public MemberAssignmentInfo(MemberInfo member, ExpressionInfo expression)
        {
            Member = member;
            Expression = expression;
        }
    }

    public class InvocationExpressionInfo : ArgumentsExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.Invoke;
        public override Type Type { get; }

        public readonly ExpressionInfo ExprToInvoke;

        public override Expression ToExpression() =>
            Expression.Invoke(ExprToInvoke.ToExpression(), ArgumentsToExpressions());

        public InvocationExpressionInfo(ExpressionInfo exprToInvoke, ExpressionInfo[] arguments, Type type) : base(arguments)
        {
            ExprToInvoke = exprToInvoke;
            Type = type;
        }
    }

    public class BlockExpressionInfo : ExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.Block;

        public override Type Type { get; }

        public readonly ParameterExpressionInfo[] Variables;
        public readonly ExpressionInfo[] Expressions;
        public readonly ExpressionInfo Result;

        public override Expression ToExpression() =>
            Expression.Block(Expressions.Project(info => info.ToExpression()));

        public BlockExpressionInfo(Type type, ParameterExpressionInfo[] variables, ExpressionInfo[] expressions)
        {
            Variables = variables;
            Expressions = expressions;
            Result = expressions[expressions.Length - 1];
            Type = type;
        }
    }

    public class TryExpressionInfo : ExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.Try;
        public override Type Type { get; }

        public readonly ExpressionInfo Body;
        public readonly CatchBlockInfo[] Handlers;
        public readonly ExpressionInfo Finally;

        public override Expression ToExpression() =>
            Finally == null ? Expression.TryCatch(Body.ToExpression(), ToCatchBlocks(Handlers)) :
            Handlers == null ? Expression.TryFinally(Body.ToExpression(), Finally.ToExpression()) :
            Expression.TryCatchFinally(Body.ToExpression(), Finally.ToExpression(), ToCatchBlocks(Handlers));

        private static CatchBlock[] ToCatchBlocks(CatchBlockInfo[] hs)
        {
            if (hs == null)
                return Tools.Empty<CatchBlock>();
            var catchBlocks = new CatchBlock[hs.Length];
            for (var i = 0; i < hs.Length; ++i)
                catchBlocks[i] = hs[i].ToCatchBlock();
            return catchBlocks;
        }

        public TryExpressionInfo(ExpressionInfo body, ExpressionInfo @finally, CatchBlockInfo[] handlers)
        {
            Type = body.Type;
            Body = body;
            Handlers = handlers;
            Finally = @finally;
        }
    }

    public sealed class CatchBlockInfo
    {
        public readonly ParameterExpressionInfo Variable;
        public readonly ExpressionInfo Body;
        public readonly ExpressionInfo Filter;
        public readonly Type Test;

        public CatchBlockInfo(ParameterExpressionInfo variable, ExpressionInfo body, ExpressionInfo filter, Type test)
        {
            Variable = variable;
            Body = body;
            Filter = filter;
            Test = test;
        }

        public CatchBlock ToCatchBlock() => Expression.Catch(Variable.ParamExpr, Body.ToExpression());
    }

    public class LambdaExpressionInfo : ArgumentsExpressionInfo
    {
        public override ExpressionType NodeType => ExpressionType.Lambda;
        public override Type Type { get; }

        public readonly ExpressionInfo Body;
        public ExpressionInfo[] Parameters => Arguments;

        public override Expression ToExpression() => ToLambdaExpression();

        public LambdaExpression ToLambdaExpression() =>
            Expression.Lambda(Body.ToExpression(),
                Parameters.Project(p => (ParameterExpression)p.ToExpression()));

        public LambdaExpressionInfo(Type delegateType, ExpressionInfo body, ExpressionInfo[] parameters) : base(parameters)
        {
            Body = body;
            var bodyType = body.Type;
            Type = delegateType != null && delegateType != typeof(Delegate)
                ? delegateType
                : Tools.GetFuncOrActionType(Tools.GetParamExprTypes(parameters.Project(e => (ParameterExpressionInfo)e)), bodyType);
        }
    }

    public sealed class ExpressionInfo<TDelegate> : LambdaExpressionInfo
    {
        public Type DelegateType => Type;

        public override Expression ToExpression() => ToLambdaExpression();

        public new Expression<TDelegate> ToLambdaExpression() =>
            Expression.Lambda<TDelegate>(Body.ToExpression(),
                Parameters.Project(p => (ParameterExpression)p.ToExpression()));

        public ExpressionInfo(ExpressionInfo body, ExpressionInfo[] parameters)
            : base(typeof(TDelegate), body, parameters) { }
    }
}
