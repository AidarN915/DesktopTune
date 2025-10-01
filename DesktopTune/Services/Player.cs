using DesktopTune.Model;
using DesktopTune.ViewModel;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using WpfYoutubePlayer;

namespace DesktopTune.Services
{
    public class Player : INotifyPropertyChanged
    {
        private ChatClient _chatClient;
        private SettingsViewModel _settings;
        private IHubContext<PlayerHub> _hub;
        private Music? _currentMusic;
        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<Music> MusicQueue { get; set; } = new ObservableCollection<Music>();
        public ObservableCollection<Music> PriorityMusicQueue { get; set; } = new ObservableCollection<Music>();
        
        private static readonly string MusicQueuePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WpfYoutubePlayer", "musicQueue.json");

        private static readonly string PriorityMusicQueuePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WpfYoutubePlayer", "priorityMusicQueue.json");

        public Player(ChatClient chatClient,SettingsViewModel settings) 
        {
            _chatClient = chatClient;
            _settings = settings;   
        }
        public Music? CurrentMusic
        {
            get { 
                return _currentMusic;
            }
            set { 
                _currentMusic = value;
                OnPropertyChanged();
            }
        }
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public void SetHub(IHubContext<PlayerHub> hub)
        {
            _hub = hub;
        }
        public async Task<Music> GetNext()
        {
            Music m;
            m = PriorityMusicQueue.FirstOrDefault();
            if (m == null)
            {
                m = MusicQueue.FirstOrDefault();
            }
            CurrentMusic = m;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                PriorityMusicQueue.Remove(_currentMusic);
                MusicQueue.Remove(_currentMusic);
            });
            await SaveAsync();
            return m;
        }

        public async Task<bool> OrderMusic(Music music,bool isPriority)
        {
            Music m = MusicQueue.FirstOrDefault(x => x.YoutubeLink == music.YoutubeLink);
            Music m2 = PriorityMusicQueue.FirstOrDefault(x => x.YoutubeLink == music.YoutubeLink);
            if (m != null || m2 != null || (CurrentMusic != null && CurrentMusic.YoutubeLink == music.YoutubeLink))
            {
                _chatClient.SendMessage("@" + music.OwnerName + ",трек " + music.YoutubeLink + " уже в очереди.Баллы возвращены");
                return false;
            }
            if (isPriority)
            {
                PriorityMusicQueue.Add(music);
                _chatClient.SendMessage("@" + music.OwnerName + ",трек " + music.YoutubeLink + " добавлен в приоритетную очередь");
            }
            else
            {
                MusicQueue.Add(music);
                _chatClient.SendMessage("@" + music.OwnerName + ",трек " + music.YoutubeLink + " добавлен в очередь");
            }

            await _hub.Clients.All.SendAsync("NewTrackNotify");
            await SaveAsync();
            return true;
        }


        public async Task LoadAsync()
        {
            try
            {
                if (File.Exists(MusicQueuePath))
                {
                    string json = await File.ReadAllTextAsync(MusicQueuePath);
                    var loadedQueue = JsonSerializer.Deserialize<Music[]>(json) ?? Array.Empty<Music>();

                    MusicQueue.Clear();
                    foreach (var music in loadedQueue)
                        MusicQueue.Add(music);
                    CurrentMusic = MusicQueue.FirstOrDefault();
                    MusicQueue.Remove(CurrentMusic);
                }

                if (File.Exists(PriorityMusicQueuePath))
                {
                    string json = await File.ReadAllTextAsync(PriorityMusicQueuePath);
                    var loadedQueue = JsonSerializer.Deserialize<Music[]>(json) ?? Array.Empty<Music>();

                    PriorityMusicQueue.Clear();
                    foreach (var music in loadedQueue)
                        PriorityMusicQueue.Add(music);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                string dir = Path.GetDirectoryName(MusicQueuePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var MusicQueueWithCurrent = new List<Music>();
                if(CurrentMusic != null) 
                    MusicQueueWithCurrent.Add(CurrentMusic);
                MusicQueueWithCurrent.AddRange(new List<Music>(MusicQueue));

                string json = JsonSerializer.Serialize(MusicQueueWithCurrent, new JsonSerializerOptions { WriteIndented = true });
                string json2 = JsonSerializer.Serialize(PriorityMusicQueue, new JsonSerializerOptions { WriteIndented = true });

                await File.WriteAllTextAsync(MusicQueuePath, json);
                await File.WriteAllTextAsync(PriorityMusicQueuePath, json2);
            }
            catch (Exception ex)
            {
            }
        }

        public int GetVolume()
        {
            return _settings.UserSettings.Volume;
        }

        public async Task EnqueueMusic(Music m)
        {
            MusicQueue.Remove(m);
            PriorityMusicQueue.Remove(m);
            await SaveAsync();
        }
    }
}
