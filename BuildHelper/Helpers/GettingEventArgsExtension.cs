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

        static ParameterExpression parameter = Expression.Parameter(typeof(GettingEventArgs));
        static Expression property = Expression.Property(parameter, _CurrentPropertyInfo);
        static UnaryExpression instanceCast = Expression.TypeAs(parameter, _CurrentPropertyInfo.DeclaringType);
        static UnaryExpression returnValueCast = Expression.
            Convert(Expression.Call(instanceCast, _CurrentPropertyInfo.GetGetMethod()), typeof(int));

        static Func<GettingEventArgs, int> GetDelegate = Expression.
            Lambda<Func<GettingEventArgs, int>>(returnValueCast, parameter).
            Compile();

        static public int GetCurrent(this GettingEventArgs arg)
        {
            return GetDelegate(arg);
        }
    }
}
