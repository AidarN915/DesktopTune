using DesktopTune.Model;
using DesktopTune.Services;
using GalaSoft.MvvmLight.Command;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TwitchLib.Communication.Interfaces;
using WpfYoutubePlayer;

namespace DesktopTune.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        private SettingsViewModel _settings;
        private int _volume;
        private IHubContext<PlayerHub> _hub;
        public  Player Player;
        public MainViewModel(SettingsViewModel settings)
        {
            _settings = settings;
            _volume = _settings.UserSettings.Volume;

            ToggleVideo = new RelayCommand(PlayPauseVideo);
            SkipTrack = new RelayCommand(SkipMusic);

            Order = new RelayCommand<string>(OrderMusic);
        }
        public void SetHub(IHubContext<PlayerHub> hub)
        {
            _hub = hub;
        }
        public void SetPlayer(Player player)
        {
            Player = player;
        }
        public int Volume
        {
            get => _volume;
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    OnPropertyChanged();
                    if (_hub != null)
                        _ = _hub.Clients.All.SendAsync("SetVolume", value);
                    _settings.UserSettings.Volume = value;
                }
            }
        }
        public ICommand ToggleVideo { get; }
        public ICommand SkipTrack { get; }
        private async void PlayPauseVideo()
        {
            if (_hub != null)
            {
                await _hub.Clients.All.SendAsync("PlayPauseVideo");
            }
        }
        private async void SkipMusic()
        {
            if (_hub != null)
            {
                await _hub.Clients.All.SendAsync("SkipTrack");
            }
        }

        public ICommand Order { get; }
        private async void OrderMusic(string link)
        {
            ChatClient chat = ((MainWindow)Application.Current.MainWindow).AppVM.ChatClient;
            await chat.OrderMusic(link);
        }
    }
}
