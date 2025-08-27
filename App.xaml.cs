using System.Windows;
using Tatehama_musen_PC.Views;

namespace Tatehama_musen_PC
{
    public partial class App : Application
    {
        public string? SelectedPhoneNumber { get; set; }
        public string? SelectedDisplayName { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow();
            this.MainWindow = mainWindow;

            var workLocationWindow = new WorkLocationWindow();
            workLocationWindow.ShowDialog();

            if (string.IsNullOrEmpty(SelectedPhoneNumber))
            {
                // 勤務地が選択されなかったのでアプリを終了
                this.Shutdown();
            }
            else
            {
                mainWindow.Show();
            }
        }
    }
}

