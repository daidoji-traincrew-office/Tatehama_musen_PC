using System;
using System.Windows;
using System.Windows.Controls;
using Tatehama_musen_PC.Models;
using Tatehama_musen_PC.ViewModels;

namespace Tatehama_musen_PC.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new TenkeyViewModel();
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
                this.Title = $"Tatehama Musen - {app.SelectedDisplayName}";
            }
            else
            {
                this.Title = "Tatehama Musen";
            }
        }

        private void ChangeWorkLocationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var workLocationWindow = new WorkLocationWindow();
            workLocationWindow.ShowDialog();
            UpdateTitle();
        }
        private void AudioSettingsMenuItem_Click(object sender, RoutedEventArgs e) { }
        
        private void CallList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CallList.SelectedItem is CallListItem selectedItem && DataContext is TenkeyViewModel viewModel)
            {
                viewModel.PhoneNumber = selectedItem.PhoneNumber;
            }
        }

        private void jyuwaButton_Click(object sender, RoutedEventArgs e) { }
        private void hashinButton_Click(object sender, RoutedEventArgs e) { }
        private void syuwaButton_Click(object sender, RoutedEventArgs e) { }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}