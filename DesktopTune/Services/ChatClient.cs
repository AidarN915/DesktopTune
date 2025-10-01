using DesktopTune.Model;
using DesktopTune.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Client;
using TwitchLib.EventSub.Websockets.Core.Handler;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;
using static System.Net.WebRequestMethods;

namespace DesktopTune.Services
{
    public class ChatClient : INotifyPropertyChanged
    {

        private TwitchClient _client = new TwitchClient();
        private SettingsViewModel _settingsVM;
        private CommandViewModel _commandsVM;
        private Player _player;
        private ClientWebSocket ws;
        private CancellationTokenSource _wsCts;
        private static readonly HttpClient _http = new HttpClient();
        private bool _isChatConnected = false;
        private bool _isEventSubConnected = false;

        public ChatClient(SettingsViewModel settings,CommandViewModel commands)
        {
            _settingsVM = settings;
            _commandsVM = commands;
            _client.OnConnected += Client_OnConnected;
            _client.OnConnectionError += Client_OnConnectionError;
            _client.OnChatCommandReceived += Client_OnCommandReceive;


        }

        public bool IsChatConnected
        {
            get 
            { 
                return _isChatConnected; 
            }
            set 
            { 
                _isChatConnected = value;
                OnPropertyChanged();
            }
        }

        public bool IsEventSubConnected
        {
            get 
            { 
                return _isEventSubConnected; 
            }
            set 
            {
                _isEventSubConnected = value;
                OnPropertyChanged();
            }
        }
            


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Client_Connect()
        {
            if (string.IsNullOrWhiteSpace(_settingsVM.UserSettings.UserName)
                || string.IsNullOrWhiteSpace(_settingsVM.UserSettings.AccessToken)
                || string.IsNullOrWhiteSpace(_settingsVM.UserSettings.Channel)){
                return;
            }
            if (_client.IsConnected)
            {
                _client.Disconnect();
                IsChatConnected = false;
                _client = new TwitchClient();
            }
            ConnectionCredentials credentials = new ConnectionCredentials(_settingsVM.UserSettings.UserName
                                                                    , _settingsVM.UserSettings.AccessToken);
            _client.Initialize(credentials, _settingsVM.UserSettings.Channel);
            _client.Connect();

            _ = StartEventSubAsync();
        }
        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            IsChatConnected = true;
        }
        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            IsChatConnected = false;
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

        public void SetPlayer(Player player)
        {
            _player = player;
        }

        public async Task<bool> OrderMusic(string MusicLink,string OwnerName, bool isPriority)
        {
            int ampIndex = MusicLink.IndexOf('&');
            if (ampIndex > 0)
            {
                MusicLink = MusicLink.Substring(0, ampIndex);
            }
            YouTubeService yt = new YouTubeService();
            var res = await yt.GetVideoInfo(MusicLink);
            if (res != null && res.HasValue)
            {
                Music music = new Music();
                music.VideoId = res.Value.VideoId;
                music.YoutubeLink = MusicLink;
                music.Title = res.Value.Title;
                music.OwnerName = OwnerName;
                music.Thumbnail = res.Value.Thumbnail;

                return await _player.OrderMusic(music, isPriority);
            }
            return false;
        }

        //------------------------------------------------------------
        // Запустить подключение и подписку
        public async Task StartEventSubAsync()
        {
            if (string.IsNullOrWhiteSpace(_settingsVM.UserSettings.AccessToken) ||
                string.IsNullOrWhiteSpace(_settingsVM.UserSettings.ClientId))
            {
                MessageBox.Show("Нехватает данных для EventSub.");
                return;
            }

            var broadcasterId = _settingsVM.UserSettings.ChannelId;

            // Подключаемся к EventSub WebSocket
            await ConnectWebsocketAndSubscribeAsync(broadcasterId);
        }

        private async Task ConnectWebsocketAndSubscribeAsync(string broadcasterId)
        {
            _wsCts?.Cancel();
            _wsCts = new CancellationTokenSource();

            ws?.Dispose();
            ws = new ClientWebSocket();
            // опционально установить KeepAliveInterval (по желанию)
            // ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

            await ws.ConnectAsync(new Uri("wss://eventsub.wss.twitch.tv/ws"), CancellationToken.None);

            // Получаем welcome (секунда/две)
            string welcome = await ReceiveSingleMessageAsync(ws, _wsCts.Token);
            if (string.IsNullOrEmpty(welcome))
            {
                IsEventSubConnected = false;
                return;
            }

            string sessionId = ExtractSessionIdFromWelcome(welcome);
            if (string.IsNullOrEmpty(sessionId))
            {
                IsEventSubConnected = false;
                return;
            }

            // Создаём подписку через Helix
            bool subCreated = await CreateEventSubSubscriptionAsync(sessionId, broadcasterId);
            if (!subCreated)
            {
                IsEventSubConnected = false;
            }
            else
            {
                IsEventSubConnected = true;
            }

            // Запускаем цикл прослушивания в фоне
            _ = Task.Run(() => ListenLoopAsync(_wsCts.Token));
        }

        // Осторожно: это одно сообщение (учитывает фрагментацию)
        private async Task<string> ReceiveSingleMessageAsync(ClientWebSocket socket, CancellationToken token)
        {
            var ms = new MemoryStream();
            var buffer = new byte[4096];

            while (!token.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                if (result.MessageType == WebSocketMessageType.Close)
                    return null;

                ms.Write(buffer, 0, result.Count);

                if (result.EndOfMessage)
                {
                    var msgBytes = ms.ToArray();
                    ms.Dispose();
                    return Encoding.UTF8.GetString(msgBytes);
                }
            }
            return null;
        }

        private string ExtractSessionIdFromWelcome(string welcomeJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(welcomeJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("payload", out var payload) &&
                    payload.TryGetProperty("session", out var session) &&
                    session.TryGetProperty("id", out var idProp))
                {
                    return idProp.GetString();
                }
            }
            catch (Exception ex)
            {
                // логгирование
            }
            return null;
        }

        private async Task<bool> CreateEventSubSubscriptionAsync(string sessionId, string broadcasterId)
        {
            try
            {
                var subRequest = new
                {
                    type = "channel.channel_points_custom_reward_redemption.add",
                    version = "1",
                    condition = new { broadcaster_user_id = broadcasterId },
                    transport = new { method = "websocket", session_id = sessionId }
                };

                var bodyJson = JsonSerializer.Serialize(subRequest);
                using var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settingsVM.UserSettings.AccessToken);
                // Ensure Client-Id is present
                if (_http.DefaultRequestHeaders.Contains("Client-Id"))
                    _http.DefaultRequestHeaders.Remove("Client-Id");
                _http.DefaultRequestHeaders.Add("Client-Id", _settingsVM.UserSettings.ClientId);

                var resp = await _http.PostAsync("https://api.twitch.tv/helix/eventsub/subscriptions", content);
                var respBody = await resp.Content.ReadAsStringAsync();

                if (resp.StatusCode == HttpStatusCode.Accepted)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // Цикл прослушивания, запускается в фоне
        private async Task ListenLoopAsync(CancellationToken token)
        {
            var buffer = new byte[8192];

            try
            {
                while (!token.IsCancellationRequested && ws?.State == WebSocketState.Open)
                {
                    var ms = new MemoryStream();
                    WebSocketReceiveResult result = null;

                    do
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                            return;
                        }
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    var message = Encoding.UTF8.GetString(ms.ToArray());
                    ms.Dispose();

                    await ProcessWsMessageAsync(message);
                }
            }
            catch (OperationCanceledException)
            {
                // ожидаемое при отмене
            }
            catch (Exception ex)
            {
                // можно пробовать переподключиться
            }
        }

        private async Task ProcessWsMessageAsync(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string messageType = null;
                if (root.TryGetProperty("metadata", out var m) &&
                    m.TryGetProperty("message_type", out var mt))
                {
                    messageType = mt.GetString();
                }

                if (messageType == "session_keepalive")
                {
                    // можно логировать
                    Console.WriteLine("keepalive");
                    return;
                }
                if (messageType == "notification")
                {
                    // В payload.event содержится событие редемпшена
                    if (root.TryGetProperty("payload", out var payload) &&
                        payload.TryGetProperty("event", out var ev))
                    {
                        string redemptionId = ev.GetProperty("id").GetString();
                        string rewardId = ev.GetProperty("reward").GetProperty("id").GetString();
                        string userName = ev.GetProperty("user_name").GetString();
                        string rewardTitle = ev.GetProperty("reward").GetProperty("title").GetString();
                        string userInput = ev.TryGetProperty("user_input", out var ui) ? ui.GetString() : null;

                        // UI вызов: MessageBox из UI-потока
                        await Application.Current.Dispatcher.Invoke(async () =>
                        {
                            MessageBox.Show($"Награда: {rewardTitle}\nПользователь: {userName}\nКомментарий: {userInput}");
                            if (rewardTitle.Contains("заказ музыки(YouTube)", StringComparison.OrdinalIgnoreCase))
                            {
                                var success = await OrderMusic(userInput, userName, rewardTitle.Contains("Приоритетный", StringComparison.OrdinalIgnoreCase));

                                if (!success)
                                {
                                    await UpdateRedemptionStatusAsync(redemptionId, rewardId, "CANCELED");
                                }
                                else
                                {
                                    await UpdateRedemptionStatusAsync(redemptionId, rewardId, "FULFILLED");
                                }
                            }
                        });
                    }
                    return ;
                }
                if (messageType == "session_reconnect")
                {
                    // Twitch просит переподключиться на другой URL
                    if (root.TryGetProperty("payload", out var payload) &&
                        payload.TryGetProperty("session", out var session) &&
                        session.TryGetProperty("reconnect_url", out var ru))
                    {
                        string reconnectUrl = ru.GetString();

                        _ = Task.Run(async () =>
                        {
                            await SafeReconnectAsync(reconnectUrl);
                        });
                    }
                    return ;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing ws message: " + ex);
            }

            return ;
        }

        private async Task SafeReconnectAsync(string reconnectUrl)
        {
            try
            {
                _wsCts?.Cancel();
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "reconnect", CancellationToken.None);
            }
            catch { }

            // создаём новое соединение на reconnectUrl
            _wsCts = new CancellationTokenSource();
            ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(reconnectUrl), CancellationToken.None);

            // получаем welcome и sessionId, и (если нужно) перенастраиваем подписки через Helix
            // Но Twitch гарантирует, что существующие подписки должны оставаться активными по новой сессии
            string welcome = await ReceiveSingleMessageAsync(ws, _wsCts.Token);
            string sessionId = ExtractSessionIdFromWelcome(welcome);
            Console.WriteLine("Reconnected, new session: " + sessionId);

            // Запускаем новый listen loop
            _ = Task.Run(() => ListenLoopAsync(_wsCts.Token));
        }

        // Корректно останавливаем
        public async Task StopEventSubAsync()
        {
            _wsCts?.Cancel();

            if (ws != null && (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived))
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "client closing", CancellationToken.None);
                }
                catch { }
            }
            ws?.Dispose();
            ws = null;
        }
        public async Task UpdateRedemptionStatusAsync(string redemptionId, string rewardId, string status)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settingsVM.UserSettings.AccessToken);
            http.DefaultRequestHeaders.Add("Client-Id", _settingsVM.UserSettings.ClientId);

            var body = new
            {
                status = status // "CANCELED"/"FULFILLED"
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            string url = $"https://api.twitch.tv/helix/channel_points/custom_rewards/redemptions" +
                         $"?id={redemptionId}&broadcaster_id={_settingsVM.UserSettings.ChannelId}&reward_id={rewardId}";

            var response = await http.PatchAsync(url, content);

            if (!response.IsSuccessStatusCode) { 
                string error = await response.Content.ReadAsStringAsync();
            }
        }

    }
}
