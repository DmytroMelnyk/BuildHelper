using System;
using System.Linq;
using System.Linq.Expressions;

namespace BuildHelper.Helpers
{
    public static class EnumFlagAttributesHelper<T>
    {
        static Func<T, T, T> _SetFlagDelegate;
        static Func<T, T, T> _ClearFlagDelegate;

        static EnumFlagAttributesHelper()
        {
            if (!typeof(T).IsEnum || !typeof(T).IsDefined(typeof(FlagsAttribute), false))
                throw new ArgumentException("Type should be flagged enum");

            ParameterExpression flagFieldParam = Expression.Parameter(typeof(T));
            UnaryExpression flagFieldParamCastToUnderType = Expression.Convert(flagFieldParam, Enum.GetUnderlyingType(typeof(T)));
            ParameterExpression flagParam = Expression.Parameter(typeof(T));
            UnaryExpression flagParamCastToUnderType = Expression.Convert(flagParam, Enum.GetUnderlyingType(typeof(T)));
            
            BinaryExpression setFlagFieldOfUnderType = Expression.Or(flagFieldParamCastToUnderType, flagParamCastToUnderType);
            UnaryExpression setFlagCovertedToOriginalType = Expression.Convert(setFlagFieldOfUnderType, typeof(T));
            BinaryExpression clearFlagFieldOfUnderType = Expression.And(flagFieldParamCastToUnderType, Expression.Not(flagParamCastToUnderType));
            UnaryExpression clearFlagCovertedToOriginalType = Expression.Convert(clearFlagFieldOfUnderType, typeof(T));

            _SetFlagDelegate = Expression.Lambda<Func<T, T, T>>(setFlagCovertedToOriginalType, flagFieldParam, flagParam).Compile();
            _ClearFlagDelegate = Expression.Lambda<Func<T, T, T>>(clearFlagCovertedToOriginalType, flagFieldParam, flagParam).Compile();
        }

        public static T SetFlag(T flagField, T flag)
        {
            return _SetFlagDelegate(flagField, flag);
        }

        public static T ClearFlag(T flagField, T flag)
        {
            return _ClearFlagDelegate(flagField, flag);
        }

        public static T ChangeFlag(T flagField, T flag, bool setFlag)
        {
            return setFlag ? _SetFlagDelegate(flagField, flag) : _ClearFlagDelegate(flagField, flag);
        }
    }
}
