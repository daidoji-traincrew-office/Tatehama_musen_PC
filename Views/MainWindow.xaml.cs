
using System;
using System.Windows;
using System.Windows.Controls;
using Tatehama_musen_PC.Models;
using Tatehama_musen_PC.ViewModels;

namespace Tatehama_musen_PC.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            PopulateCallList();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTitle();
        }

        private void PopulateCallList()
        {
            CallList?.Items.Clear();
            var allLocations = LocationData.GetLocations();
            foreach (var location in allLocations)
            {
                CallList?.Items.Add(location);
            }
        }

        private void UpdateTitle()
        {
            var app = (App)Application.Current;
            if (!string.IsNullOrEmpty(app.SelectedDisplayName))
            {
                this.Title = $"Tatehama Musen - {app.SelectedDisplayName} ({app.SelectedPhoneNumber})";
            }
            else
            {
                this.Title = "Tatehama Musen";
            }
        }

        private async void ChangeWorkLocationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var workLocationWindow = new WorkLocationWindow();
            workLocationWindow.ShowDialog();
            UpdateTitle();
            await _viewModel.ReregisterWithServerAsync();
        }
        private void AudioSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var audioSettingsWindow = new AudioSettingsWindow();
            audioSettingsWindow.Owner = this;
            audioSettingsWindow.ShowDialog();
        }
        
        private void CallList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CallList.SelectedItem is CallListItem selectedItem)
            {
                _viewModel.TenkeyViewModel.PhoneNumber = selectedItem.PhoneNumber;
            }
        }

        private void jyuwaButton_Click(object sender, RoutedEventArgs e) { }
        private void hashinButton_Click(object sender, RoutedEventArgs e) { }
        private void syuwaButton_Click(object sender, RoutedEventArgs e) { }

        private async void Window_Closed(object sender, EventArgs e)
        {
            if (_viewModel.ConnectionService.IsConnected)
            {
                await _viewModel.ConnectionService.DisconnectAsync();
            }

            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
