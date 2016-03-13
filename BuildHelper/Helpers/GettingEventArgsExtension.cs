using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace BuildHelper
{
    public static class GettingEventArgsExtension
    {
        static Func<GettingEventArgs, int> _GetDelegate;

        static GettingEventArgsExtension()
        {
            //PropertyInfo _CurrentPropertyInfo = typeof(GettingEventArgs).
            //    GetProperty("Current", BindingFlags.NonPublic | BindingFlags.Instance);
            //ParameterExpression _Parameter = Expression.Parameter(typeof(GettingEventArgs));
            //MethodCallExpression _MethodCall = Expression.Call(_Parameter, _CurrentPropertyInfo.GetGetMethod(true));
            //_GetDelegate = Expression.
            //        Lambda<Func<GettingEventArgs, int>>(_MethodCall, _Parameter).
            //        Compile();

            var getDelegateMethod = new DynamicMethod("getDelegateMethod", typeof(int), new[] { typeof(GettingEventArgs) }, typeof(GettingEventArgs));
            var ilGen = getDelegateMethod.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, typeof(GettingEventArgs).GetField("m_currentNumOperations", BindingFlags.NonPublic | BindingFlags.Instance));
            ilGen.Emit(OpCodes.Ret);
            _GetDelegate = (Func<GettingEventArgs, int>)getDelegateMethod.CreateDelegate(typeof(Func<GettingEventArgs, int>));
        }

        static public int GetCurrent(this GettingEventArgs arg)
        {
            return _GetDelegate(arg);
        }
    }
}
