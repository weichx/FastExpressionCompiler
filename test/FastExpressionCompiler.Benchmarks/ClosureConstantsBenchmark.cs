﻿using System;
using System.Linq;
using BenchmarkDotNet.Attributes;

using System.Linq.Expressions;
using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.Benchmarks
{
    public class ClosureConstantsBenchmark
    {
        private static Expression<Func<A>> CreateExpression()
        {
            var q = Constant(new Q());
            var x = Constant(new X());
            var y = Constant(new Y());
            var z = Constant(new Z());

            var fe = Lambda<Func<A>>(
                New(typeof(A).GetTypeInfo().DeclaredConstructors.First(),
                    q, x, y, z, New(typeof(B).GetTypeInfo().DeclaredConstructors.First(),
                        q, x, y, z), New(typeof(C).GetTypeInfo().DeclaredConstructors.First(),
                        q, x, y, z)));

            return fe;
        }

        [MemoryDiagnoser]
        public class Compilation
        {
            /*
            |      Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            |------------ |-----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
            |     Compile | 406.719 us | 1.4416 us | 1.2780 us | 55.79 |    0.28 | 0.9766 | 0.4883 |      - |   6.26 KB |
            | CompileFast |   7.290 us | 0.0321 us | 0.0285 us |  1.00 |    0.00 | 0.4501 | 0.2213 | 0.0305 |   2.06 KB |

            ## Optimizing storing closure constants into variables

            |      Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            |------------ |-----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
            |     Compile | 407.657 us | 0.9090 us | 0.8058 us | 58.66 |    0.32 | 0.9766 | 0.4883 |      - |   6.26 KB |
            | CompileFast |   6.947 us | 0.0392 us | 0.0367 us |  1.00 |    0.00 | 0.4272 | 0.2136 | 0.0305 |   1.96 KB |

            ## Everything with closure

            |      Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            |------------ |-----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
            |     Compile | 412.342 us | 2.2856 us | 2.1380 us | 58.68 |    0.43 | 0.9766 | 0.4883 |      - |   6.26 KB |
            | CompileFast |   7.027 us | 0.0333 us | 0.0312 us |  1.00 |    0.00 | 0.3433 | 0.1678 | 0.0305 |   1.56 KB |

            */

            private readonly Expression<Func<A>> _expr = CreateExpression();

            [Benchmark]
            public object Compile() =>
                _expr.Compile();

            [Benchmark(Baseline = true)]
            public object CompileFast() =>
                _expr.CompileFast(true);
        }

        [MemoryDiagnoser]
        public class Invocation
        {
            /*
BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  DefaultJob : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT


|              Method |     Mean |    Error |   StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------- |---------:|---------:|---------:|------:|-------:|------:|------:|----------:|
|     Invoke_Compiled | 37.25 ns | 0.153 ns | 0.128 ns |  1.00 | 0.0340 |     - |     - |     160 B |
| Invoke_CompiledFast | 37.10 ns | 0.141 ns | 0.125 ns |  1.00 | 0.0340 |     - |     - |     160 B |
            */

            private readonly Func<A> _compiled     = CreateExpression().Compile();
            private readonly Func<A> _compiledFast = CreateExpression().CompileFast(true);

            [Benchmark]
            public object Invoke_Compiled() =>
                _compiled();

            [Benchmark(Baseline = true)]
            public object Invoke_CompiledFast() =>
                _compiledFast();
        }

        public class Q { }
        public class X { }
        public class Y { }
        public class Z { }

        public class A
        {
            public Q Q { get; }
            public X X { get; }
            public Y Y { get; }
            public Z Z { get; }
            public B B { get; }
            public C C { get; }

            public A(Q q, X x, Y y, Z z, B b, C c)
            {
                Q = q;
                X = x;
                Y = y;
                Z = z;
                B = b;
                C = c;
            }
        }

        public class B
        {
            public Q Q { get; }
            public X X { get; }
            public Y Y { get; }
            public Z Z { get; }

            public B(Q q, X x, Y y, Z z)
            {
                Q = q;
                X = x;
                Y = y;
                Z = z;
            }
        }

        public class C
        {
            public Q Q { get; }
            public X X { get; }
            public Y Y { get; }
            public Z Z { get; }

            public C(Q q, X x, Y y, Z z)
            {
                Q = q;
                X = x;
                Y = y;
                Z = z;
            }
        }
    }
}
