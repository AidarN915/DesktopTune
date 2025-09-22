using DesktopTune.Model;
using DesktopTune.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using WpfYoutubePlayer;

namespace DesktopTune.Services
{
    public class ChatClient
    {

        private TwitchClient _client = new TwitchClient();
        private SettingsViewModel _settingsVM;
        private CommandViewModel _commandsVM;

        public ChatClient()
        {

            _client.OnConnected += Client_OnConnected;
            _client.OnConnectionError += Client_OnConnectionError;
            _client.OnChatCommandReceived += Client_OnCommandReceive;
        }
        public void UpdateDataContext()
        {
            AppViewModel appVm= ((MainWindow)Application.Current.MainWindow).AppVM;
            _settingsVM = appVm.SettingsVM;
            _commandsVM = appVm.CommandsVM;
        }

        //Twitch IRC
        public void Client_Connect()
        {
            UpdateDataContext();
            if (string.IsNullOrWhiteSpace(_settingsVM.UserSettings.UserName)
                || string.IsNullOrWhiteSpace(_settingsVM.UserSettings.AccessToken)
                || string.IsNullOrWhiteSpace(_settingsVM.UserSettings.Channel)){
                return;
            }
            if (_client.IsConnected)
            {
                _client.Disconnect();
                _client = new TwitchClient();
            }
            ConnectionCredentials credentials = new ConnectionCredentials(_settingsVM.UserSettings.UserName
                                                                    , _settingsVM.UserSettings.AccessToken);
            _client.Initialize(credentials, _settingsVM.UserSettings.Channel);
            _client.Connect();
        }
        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            MessageBox.Show($"Подключено к каналу {_settingsVM.UserSettings.Channel} как {e.BotUsername}");
        }
        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            MessageBox.Show($"Ошибка подключения: {e.Error.Message}");
        }
        private void Client_OnCommandReceive(object sender, OnChatCommandReceivedArgs e)
        {
            TwitchChatCommand command = _commandsVM.Commands.FirstOrDefault(x => x.Command == e.Command.CommandText);
            if (command != null)
            {
                SendMessage(command.Answer);
            }

        }
        public void SendMessage(string message)
        {
            if (_client.IsConnected)
            {
                _client.SendMessage(_settingsVM.UserSettings.Channel, message);
            }
        }
    }
}
