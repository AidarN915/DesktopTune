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
        private Player _player;
        public MainViewModel(SettingsViewModel settings)
        {
            _settings = settings;
            _volume = _settings.UserSettings.Volume;

            ToggleVideo = new RelayCommand(PlayPauseVideo);
            SkipTrack = new RelayCommand(SkipMusic);
            BanLink = new RelayCommand(BanTrack);
            BanAuthor = new RelayCommand(BanAuthorChannel);
            BanOwner = new RelayCommand(BanMusicOwner);

            Order = new RelayCommand<string>(OrderMusic);
        }
        public void SetHub(IHubContext<PlayerHub> hub)
        {
            _hub = hub;
        }
        public Player Player { 
            get { return _player; } 
            set { _player = value; OnPropertyChanged(); } 
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
        public ICommand BanLink { get; }
        public ICommand BanAuthor { get; }
        public ICommand BanOwner { get; }
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
        private void BanTrack()
        {
            _ = Player.BanCurrentTrack();
        }
        private void BanMusicOwner()
        {
            _ = Player.BanCurrentOwner();
        }
        private void BanAuthorChannel()
        {
            _ = Player.BanCurrentAuthor();
        }

        public ICommand Order { get; }
        private async void OrderMusic(string link)
        {
            ChatClient chat = ((MainWindow)Application.Current.MainWindow).AppVM.ChatClient;
            await chat.OrderMusic(link,"AppTest",false);
        }
    }
}
