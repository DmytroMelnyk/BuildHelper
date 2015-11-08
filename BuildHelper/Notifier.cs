using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BuildHelper
{
    [Serializable]
    public abstract class Notifier : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged = delegate {};

        protected virtual void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected virtual void SetField<T>(ref T flagField, bool setFlag, T flag, [CallerMemberName] string propertyName = null) 
            where T: struct, IConvertible
        {
            Type type = typeof(T);
            if (!type.IsEnum || !type.GetCustomAttributes(typeof(FlagsAttribute), false).Any())
                throw new ArgumentException("Type should be flagged enum");

            dynamic dFlag = flag;
            T newFlagField = setFlag ? (flagField | dFlag) : (flagField & ~dFlag);
            if (!EqualityComparer<T>.Default.Equals(flagField, newFlagField))
            {
                flagField = newFlagField;
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            //or we can use smth. like this:
            //if (Type.GetTypeCode(Enum.GetUnderlyingType(type)) == TypeCode.UInt64)
            //{
            //    UInt64 uFlag = flag.ToUInt64(null);
            //    UInt64 uOldFlagField = flagField.ToUInt64(null);
            //    UInt64 uNewFlagField = setFlag ? (uOldFlagField | uFlag) : (uOldFlagField & ~uFlag);

            //    if (uOldFlagField != uNewFlagField)
            //    {
            //        flagField = (T)Enum.ToObject(type, uNewFlagField);
            //    }
            //}
            //else
            //{
            //    var aaa = (T)Convert.ChangeType(flag, Enum.GetUnderlyingType(type));

            //    Int64 uFlag = flag.ToInt64(null);
            //    Int64 uOldFlagField = flagField.ToInt64(null);
            //    Int64 uNewFlagField = setFlag ? (uOldFlagField | uFlag) : (uOldFlagField & ~uFlag);

            //    if (uOldFlagField != uNewFlagField)
            //    {
            //        flagField = (T)Enum.ToObject(type, uNewFlagField);
            //    }
            //}
        }
    }
}