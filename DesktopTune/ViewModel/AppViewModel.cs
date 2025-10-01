using DesktopTune.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopTune.ViewModel
{
    public class AppViewModel : BaseViewModel
    {
        public SettingsViewModel SettingsVM { get; set; }
        public CommandViewModel CommandsVM { get; set; }
        public MainViewModel MainVM { get; set; }
        public QueueViewModel QueueVM { get; set; }
        public ChatClient ChatClient { get; set; }
        public Player Player { get; set; }
        public IHubContext<PlayerHub> Hub {get;set;}

        public HostService HostService { get; set; }


        public AppViewModel()
        {
            SettingsVM = new SettingsViewModel();
            CommandsVM = new CommandViewModel();
            MainVM = new MainViewModel(SettingsVM);
            ChatClient = new ChatClient(SettingsVM,CommandsVM);

            SettingsVM.SetChatClient(ChatClient);

            Player = new Player(ChatClient,SettingsVM);
            MainVM.SetPlayer(Player);

            QueueVM = new QueueViewModel(Player);

            ChatClient.SetPlayer(Player);

            HostService = new HostService(); 

            _ = HostService.StartAsync(Player);

            Hub = HostService.HostS.Services.GetRequiredService<IHubContext<PlayerHub>>();
            MainVM.SetHub(Hub);
            Player.SetHub(Hub);
        }
    }
}
