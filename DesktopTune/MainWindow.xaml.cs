using DesktopTune.Model;
using DesktopTune.Services;
using DesktopTune.View;
using DesktopTune.ViewModel;
using Microsoft.Extensions.Hosting;
using System.Windows;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;


namespace WpfYoutubePlayer
{
    public partial class MainWindow : Window
    {
        public AppViewModel AppVM { get; set; }


        public MainWindow()
        {
            AppVM = new AppViewModel();
            this.DataContext = AppVM;

            InitializeComponent();
            MainFrame.Navigate(new MainPage { DataContext = AppVM.MainVM});

            AppVM.ChatClient.Client_Connect();


        }
        protected override void OnClosed(EventArgs e)
        {
            SettingsService.SaveSettings(AppVM.SettingsVM.UserSettings);
            _ = AppVM.Player.SaveAsync();
            _ = AppVM.HostService.StopAsync();
            base.OnClosed(e);
        }
        private void OpenMainPage(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new MainPage { DataContext = AppVM.MainVM});
        }

        private void OpenSettingsPage(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SettingsPage { DataContext = AppVM.SettingsVM });
        }

        private void OpenCommandsPage(object sender, RoutedEventArgs e)
        {  
            MainFrame.Navigate(new CommandsPage { DataContext = AppVM.CommandsVM});
        }

        private void OpenQueuePage(object sender, RoutedEventArgs e)
        {  
            MainFrame.Navigate(new QueuePage { DataContext = AppVM.QueueVM});
        }

    }
}
