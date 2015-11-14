using System;
using System.Linq;
using System.Linq.Expressions;

namespace BuildHelper.Helpers
{
    public static class EnumFlagAttributesHelper<T>
    {
        static bool _IsFlaggedEnum = typeof(T).IsEnum && typeof(T).GetCustomAttributes(typeof(FlagsAttribute), false).Any();
        static ParameterExpression flagFieldExp = Expression.Parameter(typeof(T));
        static UnaryExpression flagFieldExpCast;
        static ParameterExpression flagExp = Expression.Parameter(typeof(T));
        static UnaryExpression flagExpCast;
        static Func<T, T, T> SetFlagDelegate;
        static Func<T, T, T> ClearFlagDelegate;

        static EnumFlagAttributesHelper()
        {
            if (!_IsFlaggedEnum)
                throw new ArgumentException("Type should be flagged enum");

            flagFieldExpCast = Expression.Convert(flagFieldExp, Enum.GetUnderlyingType(typeof(T)));
            flagExpCast = Expression.Convert(flagExp, Enum.GetUnderlyingType(typeof(T)));
            SetFlagDelegate = Expression.Lambda<Func<T, T, T>>(Expression.Convert(Expression.Or(flagFieldExpCast, flagExpCast), typeof(T)), flagFieldExp, flagExp).Compile();
            ClearFlagDelegate = Expression.Lambda<Func<T, T, T>>(Expression.Convert(Expression.And(flagFieldExpCast, Expression.Not(flagExpCast)), typeof(T)), flagFieldExp, flagExp).Compile();
        }

        public static T SetFlag(T flagField, T flag)
        {
            return SetFlagDelegate(flagField, flag);
        }

        public static T ClearFlag(T flagField, T flag)
        {
            return ClearFlagDelegate(flagField, flag);
        }

        public static T ChangeFlag(T flagField, T flag, bool setFlag)
        {
            return setFlag ? SetFlagDelegate(flagField, flag) : ClearFlagDelegate(flagField, flag);
        }
    }
}
