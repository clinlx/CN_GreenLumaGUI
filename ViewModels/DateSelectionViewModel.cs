using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;

namespace CN_GreenLumaGUI.ViewModels
{
    public class DateSelectionViewModel : ObservableObject
    {
        private DateTime? selectedDate;
        public DateTime? SelectedDate
        {
            get => selectedDate;
            set => SetProperty(ref selectedDate, value);
        }

        public RelayCommand ConfirmCmd { get; }
        public RelayCommand CancelCmd { get; }

        private readonly Window _window;
        public bool IsConfirmed { get; private set; } = false;

        public DateSelectionViewModel(Window window)
        {
            _window = window;
            SelectedDate = DateTime.Now;
            ConfirmCmd = new RelayCommand(Confirm);
            CancelCmd = new RelayCommand(Cancel);
        }

        private void Confirm()
        {
            if (SelectedDate == null) return;
            IsConfirmed = true;
            _window.Close();
        }

        private void Cancel()
        {
            IsConfirmed = false;
            _window.Close();
        }
    }
}
