using System.Windows;
using Tatehama_musen_PC.ViewModels;

namespace Tatehama_musen_PC.Views
{
    public partial class WorkLocationWindow : Window
    {
        public WorkLocationWindow()
        {
            InitializeComponent();
            var viewModel = new WorkLocationViewModel();
            DataContext = viewModel;

            viewModel.RequestClose += (s, e) => this.Close();
        }
    }
}
