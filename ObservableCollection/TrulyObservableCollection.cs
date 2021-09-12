using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

// Copyright (c) 2021 Panagiotis Mitropanos
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace Cheapo.ObservableCollection
{
    public sealed class TrulyObservableCollection<T> : ObservableCollection<T>
        where T : INotifyPropertyChanged
    {
        private TrulyObservableCollection()
        {
            CollectionChanged += TrulyObservableCollection_CollectionChanged;
        }

        public TrulyObservableCollection(IEnumerable<T> pItems) : this()
        {
            foreach (var item in pItems) Add(item);
        }

        public event PropertyChangedEventHandler ItemPropertyChanged;

        private void TrulyObservableCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (var item in e.NewItems)
                    ((INotifyPropertyChanged)item).PropertyChanged += item_PropertyChanged;

            if (e.OldItems == null) return;
            {
                foreach (var item in e.OldItems)
                    ((INotifyPropertyChanged)item).PropertyChanged -= item_PropertyChanged;
            }
        }

        private void item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var args =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender,
                    IndexOf((T)sender));
            OnCollectionChanged(args);

            ItemPropertyChanged?.Invoke(sender, e);
        }
    }
}