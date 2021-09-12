using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Cheapo.Commands;
using Cheapo.Database;
using Cheapo.Models;
using Cheapo.ObservableCollection;
using Microsoft.Win32;

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

namespace Cheapo.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly List<PurchaseModel> _purchasesToBeDeleted = new();

        private decimal _expenses;
        private ICommand _exportToFileCommand;
        private TrulyObservableCollection<PurchaseModel> _purchases;
        private ICommand _removeItemFromCollectionCommand;

        private ICommand _savePurchasesCommand;
        private DateTime _selectedDate;
        private ICommand _selectedDateChangedCommand;

        public MainWindowViewModel()
        {
            SelectedDate = DateTime.Now;
            InitializeMenuRemoveIcon();
            FillPurchaseCollection();
        }

        public Image RemoveImage { get; private set; }

        public ICommand SelectedDateChangedCommand => _selectedDateChangedCommand
            ??= new RelayCommand(_ => SelectedDateChanged());

        public ICommand RemoveItemFromCollectionCommand => _removeItemFromCollectionCommand
            ??= new RelayCommand(RemoveItemFromCollection);

        public ICommand SavePurchasesCommand => _savePurchasesCommand
            ??= new RelayCommand(_ => SavePurchases());

        public ICommand ExportToFileCommand => _exportToFileCommand
            ??= new RelayCommand(_ => ExportToFile());

        public TrulyObservableCollection<PurchaseModel> Purchases
        {
            get => _purchases;
            set
            {
                _purchases = value;
                Expenses = value.Sum(x => x.Price);
                OnPropertyChanged(nameof(Purchases));
            }
        }

        public decimal Expenses
        {
            get => _expenses;
            set
            {
                _expenses = value;
                OnPropertyChanged(nameof(Expenses));
            }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged(nameof(SelectedDate));
            }
        }

        private void InitializeMenuRemoveIcon()
        {
            var img = new Image();
            var path = ConfigurationManager.AppSettings["AppPath"] + "/Images/delete.png";
            img.Source = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
            RemoveImage = img;
        }

        private void CollectionChangedAsync(object sender, EventArgs e)
        {
            Purchases = (TrulyObservableCollection<PurchaseModel>)sender;
        }

        private void SavePurchases()
        {
            const string msg = "Είστε σίγουροι ότι θέλετε να προχωρήσετε με την αποθήκευση των αγορών?";
            const string title = "Αποθήκευση αλλαγών";
            const MessageBoxButton button = MessageBoxButton.YesNo;

            var result = MessageBox.Show(msg, title, button, MessageBoxImage.Question);
            if (result == MessageBoxResult.No) return;

            Mouse.OverrideCursor = Cursors.Wait;
            UpdatePurchases();
            InsertPurchases();
            DeletePurchases();
            Mouse.OverrideCursor = Cursors.Arrow;

            SystemSounds.Beep.Play();

            const string infoMsg = "Οι αλλαγές σας αποθηκεύτηκαν επιτυχώς!";
            const string infoTitle = nameof(Cheapo);
            MessageBox.Show(infoMsg, infoTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdatePurchases()
        {
            var purchasesToBeUpdated = Purchases.Where(x => x.Id is not 0).ToList();
            purchasesToBeUpdated.ForEach(SqlLiteDataAccess.UpdatePurchases);
        }

        private void DeletePurchases()
        {
            _purchasesToBeDeleted.ForEach(SqlLiteDataAccess.DeletePurchases);
        }

        private void InsertPurchases()
        {
            var purchasesToBeInserted = Purchases.Where(x => x.Id is 0).ToList();
            foreach (var p in purchasesToBeInserted)
            {
                p.Year = _selectedDate.Year.ToString();
                p.Month = _selectedDate.Month.ToString();
                SqlLiteDataAccess.InsertPurchases(p);
            }
        }

        private void RemoveItemFromCollection(object sender)
        {
            if (sender is not PurchaseModel purchase) return;
            if (purchase.Id is not 0) _purchasesToBeDeleted.Add(purchase);
            Purchases.Remove(purchase);
        }

        private void FillPurchaseCollection()
        {
            var year = SelectedDate.Year.ToString();
            var month = SelectedDate.Month.ToString();
            var purchases = SqlLiteDataAccess.LoadPurchasesByYearAndMonth(year, month);
            Purchases = new TrulyObservableCollection<PurchaseModel>(purchases);
            Purchases.CollectionChanged += CollectionChangedAsync;
        }

        private void SelectedDateChanged()
        {
            FillPurchaseCollection();
        }

        private void ExportToFile()
        {
            var exportDictionary = new Dictionary<MessageBoxResult, Action>
            {
                { MessageBoxResult.Cancel, () => { } },
                { MessageBoxResult.Yes, ExportYear },
                { MessageBoxResult.No, ExportMonthQuestion }
            };

            const string title = "Εξαγωγή αγορών";
            var year = SelectedDate.Year.ToString();
            var msg = $"Θέλετε να κάνετε εξαγωγή τα αρχεία όλου του {year}?";

            var result = MessageBox.Show(msg, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            Mouse.OverrideCursor = Cursors.Wait;
            if (exportDictionary.ContainsKey(result)) exportDictionary[result]();
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void ExportMonthQuestion()
        {
            const string title = "Εξαγωγή αγορών";
            var year = SelectedDate.Year.ToString();
            var secondMsg = $"Θέλετε να κάνετε εξαγωγή για τον \"{SelectedDate:MMMM}\" του {year}?";

            var secondResult = MessageBox.Show(secondMsg, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (secondResult == MessageBoxResult.No) return;

            Export(year, SelectedDate.Month.ToString());
        }

        private void ExportYear()
        {
            var year = SelectedDate.Year.ToString();
            Export(year);
        }

        private static void Export(string year, string month = null)
        {
            var jsonString = PurchasesToJson(year, month);

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Json files (*.json)|*.json",
                FilterIndex = 2,
                RestoreDirectory = true
            };

            if (!saveFileDialog.ShowDialog().GetValueOrDefault()) return;

            var filePath = saveFileDialog.FileName;
            File.AppendAllText(filePath, jsonString);

            const string title = "Εξαγωγή αγορών";
            const string msg = "Η Εξαγωγή ολοκληρώθηκε επιτυχώς!";
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static string PurchasesToJson(string year, string month = null)
        {
            var purchases = string.IsNullOrWhiteSpace(month)
                ? SqlLiteDataAccess.LoadPurchasesByYear(year)
                : SqlLiteDataAccess.LoadPurchasesByYearAndMonth(year, month);

            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                WriteIndented = true
            };

            return JsonSerializer.Serialize(purchases, options);
        }
    }
}