using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace BuildHelper
{
    public static class GettingEventArgsExtension
    {
        static Func<GettingEventArgs, int> _GetDelegate;

        static GettingEventArgsExtension()
        {
            PropertyInfo _CurrentPropertyInfo = typeof(GettingEventArgs).
                GetProperty("Current", BindingFlags.NonPublic | BindingFlags.Instance);
            ParameterExpression _Parameter = Expression.Parameter(typeof(GettingEventArgs));
            MethodCallExpression _MethodCall = Expression.Call(_Parameter, _CurrentPropertyInfo.GetGetMethod(true));
            _GetDelegate = Expression.
                    Lambda<Func<GettingEventArgs, int>>(_MethodCall, _Parameter).
                    Compile();
        }

        static public int GetCurrent(this GettingEventArgs arg)
        {
            return _GetDelegate(arg);
        }
    }
}
