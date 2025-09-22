using DesktopTune.Model;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using WpfYoutubePlayer;

namespace DesktopTune.Services
{
    public class Player
    {
        public ObservableCollection<Music> MusicQueue { get; set; } = new ObservableCollection<Music>();
        public ObservableCollection<Music> PriorityMusicQueue { get; set; } = new ObservableCollection<Music>();

        private static readonly string MusicQueuePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WpfYoutubePlayer", "musicQueue.json");

        private static readonly string PriorityMusicQueuePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WpfYoutubePlayer", "priorityMusicQueue.json");

        public async Task<int> OrderMusic(Music music,bool isPriority)
        {
            Music m = MusicQueue.FirstOrDefault(x => x.YoutubeLink == music.YoutubeLink);
            Music m2 = PriorityMusicQueue.FirstOrDefault(x => x.YoutubeLink == music.YoutubeLink);
            ChatClient _chatClient = ((MainWindow)Application.Current.MainWindow).AppVM.ChatClient;
            if(m != null || m2 != null)
            {
                _chatClient.SendMessage("@" + music.OwnerName + ",трек " + music.YoutubeLink + " уже в очереди.Баллы возвращены");
                return 0;
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
            await SaveAsync();
            return 1;
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

                string json = JsonSerializer.Serialize(MusicQueue, new JsonSerializerOptions { WriteIndented = true });
                string json2 = JsonSerializer.Serialize(PriorityMusicQueue, new JsonSerializerOptions { WriteIndented = true });

                await File.WriteAllTextAsync(MusicQueuePath, json);
                await File.WriteAllTextAsync(PriorityMusicQueuePath, json2);
            }
            catch (Exception ex)
            {
            }
        }
    }
}
