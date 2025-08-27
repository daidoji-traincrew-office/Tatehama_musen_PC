using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NAudio.Wave;

namespace Tatehama_musen_PC.ViewModels
{
    public class TenkeyViewModel : INotifyPropertyChanged, IDisposable
    {
        private string _phoneNumber = "";
        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                _phoneNumber = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddNumberCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand DeleteCommand { get; }

        private readonly WaveOutEvent _waveOut;
        private readonly AudioFileReader _audioFileReader;

        public TenkeyViewModel()
        {
            AddNumberCommand = new RelayCommand<string>(AddNumber);
            ClearCommand = new RelayCommand(Clear);
            DeleteCommand = new RelayCommand(Delete);

            _waveOut = new WaveOutEvent();
            // The following path needs to be absolute
            _audioFileReader = new AudioFileReader(@"d:\\tatehama_Client\PC\Tatehama_musen_PC\sound\push.wav");
        }

        private void PlaySound()
        {
            _audioFileReader.Position = 0;
            _waveOut.Stop();
            _waveOut.Init(_audioFileReader);
            _waveOut.Play();
        }

        private void AddNumber(string number)
        {
            if (PhoneNumber.Length < 4)
            {
                PhoneNumber += number;
                PlaySound();
            }
        }

        private void Clear()
        {
            if (!string.IsNullOrEmpty(PhoneNumber))
            {
                PhoneNumber = "";
                PlaySound();
            }
        }

        private void Delete()
        {
            if (!string.IsNullOrEmpty(PhoneNumber))
            {
                PhoneNumber = PhoneNumber.Substring(0, PhoneNumber.Length - 1);
                PlaySound();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Dispose()
        {
            _waveOut.Dispose();
            _audioFileReader.Dispose();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute((T)parameter);

        public void Execute(object parameter) => _execute((T)parameter);

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
