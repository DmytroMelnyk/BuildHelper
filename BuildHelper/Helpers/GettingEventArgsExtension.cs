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
        static UnaryExpression instanceCast = (!_CurrentPropertyInfo.DeclaringType.IsValueType) ? Expression.TypeAs(parameter, _CurrentPropertyInfo.DeclaringType) : Expression.Convert(parameter, _CurrentPropertyInfo.DeclaringType);
        static Func<GettingEventArgs, int> GetDelegate = Expression.Lambda<Func<GettingEventArgs, int>>(Expression.TypeAs(Expression.Call(instanceCast, _CurrentPropertyInfo.GetGetMethod()), typeof(int)), parameter).Compile();

        static public int GetCurrent(this GettingEventArgs arg)
        {
            return (int)_CurrentPropertyInfo.GetValue(arg, null);
        }

        static public int GetCurrentStatic(this GettingEventArgs arg)
        {
            return GetDelegate(arg);
        }
    }
}
