using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using BuildHelper.Helpers;

namespace BuildHelper
{
    [Serializable]
    public abstract class Notifier : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected virtual void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected virtual void SetField<T>(ref T flagField, bool setFlag, T flag, [CallerMemberName] string propertyName = null)
            where T : struct, IComparable, IFormattable, IConvertible
        {
            T newFlagField = EnumFlagAttributesHelper<T>.ChangeFlag(flagField, flag, setFlag);
            SetField<T>(ref flagField, newFlagField, propertyName);
        }
    }






}