using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Tatehama_musen_PC.Models;

namespace Tatehama_musen_PC.ViewModels
{
    public class WorkLocationViewModel : INotifyPropertyChanged
    {
        public List<CallListItem> Locations { get; set; }

        private CallListItem? _selectedLocation;
        public CallListItem? SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                _selectedLocation = value;
                OnPropertyChanged();
                ((RelayCommand)SelectCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand SelectCommand { get; }

        public event EventHandler? RequestClose;

        public WorkLocationViewModel()
        {
            Locations = LocationData.GetLocations();
            SelectCommand = new RelayCommand(Select, () => SelectedLocation != null);
        }

        private void Select()
        {
            if (SelectedLocation != null)
            {
                var app = (App)App.Current;
                app.SelectedPhoneNumber = SelectedLocation.PhoneNumber;
                app.SelectedDisplayName = SelectedLocation.DisplayName;
            }
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
