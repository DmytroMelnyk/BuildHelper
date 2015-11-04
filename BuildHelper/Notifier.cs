using System;
using System.Collections.Generic;
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
    }
}