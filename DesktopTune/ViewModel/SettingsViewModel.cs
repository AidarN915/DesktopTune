using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfYoutubePlayer;

namespace DesktopTune.ViewModel
{
    public class SettingsViewModel : BaseViewModel
    {
        private UserSettings _userSettings;

        public SettingsViewModel()
        {
            _userSettings = SettingsService.LoadSettings();
            SaveCommand = new RelayCommand(SaveSettings);
            OpenAccessTokenLink = new RelayCommand(TwitchAccessTokenLink);
            OpenClientIdLink = new RelayCommand(TwitchClientIdLink);
        }
        public UserSettings UserSettings
        {
            get => _userSettings;
            set
            {
                _userSettings = value;
                OnPropertyChanged();
            }

        }
        public ICommand SaveCommand { get; }
        public ICommand OpenAccessTokenLink { get; }
        public ICommand OpenClientIdLink { get; }
        
        private void SaveSettings()
        {
            SettingsService.SaveSettings(_userSettings);

            ((MainWindow)Application.Current.MainWindow).AppVM.ChatClient.Client_Connect();
            MessageBox.Show("Настройки сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void TwitchAccessTokenLink()
        {
            OpenUrl("https://twitchtokengenerator.com/");
        }
        
        private void TwitchClientIdLink()
        {
            OpenUrl("https://dev.twitch.tv/console");
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show("Не удалось открыть ссылку.");
            }
        }
    }
}
