using DesktopTune.Services;
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
        public ChatClient ChatClient { get; set; }
        public AppViewModel()
        {
            SettingsVM = new SettingsViewModel();
            CommandsVM = new CommandViewModel();
            MainVM = new MainViewModel();
            ChatClient = new ChatClient();
        }
    }
}
