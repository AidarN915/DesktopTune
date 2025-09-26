using DesktopTune.Model;
using DesktopTune.Services;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DesktopTune.ViewModel
{
    public class QueueViewModel : BaseViewModel
    {
        public Player Player;
        public QueueViewModel(Player player)
        {
            Player = player;
            EnqueueMusic = new RelayCommand<Music>(DeleteMusic);
        }
        public ObservableCollection<Music> MusicQueue { get => Player.MusicQueue; }
        public ObservableCollection<Music> PriorityMusicQueue { get => Player.PriorityMusicQueue; }
        public ICommand EnqueueMusic { get; }
        private async void DeleteMusic(Music m)
        {
            await Player.EnqueueMusic(m);
        }
    }
}
