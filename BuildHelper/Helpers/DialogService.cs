using GalaSoft.MvvmLight;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BuildHelper
{
    public interface IDialogService
    {
        Task ShowProgressWindowAsync(string title, string message, bool isCancelable = false);
        void UpdateProgressWindow(double progressValue);
        void CloseProgressWindow();
        string OpenFileDialog(string defaultExt, string filter);
        bool? ShowDialog(Notifier viewModel);
        MessageBoxResult ShowMessageBox(string content, string title, MessageBoxButton buttons);
    }

    public class DialogService : IDialogService
    {
        ProgressDialogController controller { get; set; }

        static IDialogService _instance;
        public static IDialogService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DialogService();
                }
                return _instance;
            }
            internal set
            {
                _instance = value;
            }
        }

        public async Task ShowProgressWindowAsync(string title, string message, bool isCancelable = false)
        {
            controller = await ((MetroWindow)Application.Current.MainWindow).ShowProgressAsync(title, message, isCancelable);
            return;
        }

        public void UpdateProgressWindow(double progressValue)
        {
            if (controller != null)
            {
                controller.SetProgress(progressValue);
                controller.SetMessage((progressValue * 100).ToString());
            }
        }

        public async void CloseProgressWindow()
        {
            if (controller != null)
            {
                await controller.CloseAsync();
            }
        }

        public MessageBoxResult ShowMessageBox(string content, string title, MessageBoxButton buttons)
        {
            return MessageBox.Show(content, title, buttons);
        }


        public string OpenFileDialog(string defaultExt, string filter)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                DefaultExt = defaultExt,
                Filter = filter
            };

            if (dlg.ShowDialog().Value)
                return dlg.FileName;

            return String.Empty;
        }

        public bool? ShowDialog(Notifier viewModel)
        {
            return new DialogWindow()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow,
                ShowInTaskbar = false,
            }.ShowDialog();
        }
    }
}
