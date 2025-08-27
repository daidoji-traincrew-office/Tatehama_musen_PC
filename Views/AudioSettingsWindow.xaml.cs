using System.ComponentModel;
using System.Windows;
using Tatehama_musen_PC.ViewModels;

namespace Tatehama_musen_PC.Views
{
    public partial class AudioSettingsWindow : Window
    {
        public AudioSettingsWindow()
        {
            InitializeComponent();
            var viewModel = new AudioSettingsViewModel();
            DataContext = viewModel;

            viewModel.RequestClose += (s, e) => this.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (DataContext is AudioSettingsViewModel vm)
            {
                vm.Dispose();
            }
            base.OnClosing(e);
        }
    }
}
