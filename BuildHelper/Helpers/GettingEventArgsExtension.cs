using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace BuildHelper
{
    public static class GettingEventArgsExtension
    {
        static PropertyInfo _CurrentPropertyInfo = typeof(GettingEventArgs).
            GetProperty("Current", BindingFlags.NonPublic | BindingFlags.Instance);
        static ParameterExpression _Parameter = Expression.Parameter(typeof(GettingEventArgs));
        static MethodCallExpression _MethodCall = Expression.Call(_Parameter, _CurrentPropertyInfo.GetGetMethod(true));
        static Func<GettingEventArgs, int> _GetDelegate = Expression.
                    Lambda<Func<GettingEventArgs, int>>(_MethodCall, _Parameter).
                    Compile();

        static public int GetCurrent(this GettingEventArgs arg)
        {
            return _GetDelegate(arg);
        }
    }
}
