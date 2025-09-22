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
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;


namespace WpfYoutubePlayer
{
    public partial class MainWindow : Window
    {
        public AppViewModel AppVM { get; set; }

 //       private IHost? _webHost;

        public MainWindow()
        {
            /*_webHost = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls("http://localhost:5000");
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseStaticFiles(); // если будут html/css/js

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/player", async context =>
                        {
                            string videoId = "fH92Dq7jO90";
                            string html = $@"
<!doctype html>
<html lang='ru'>
<head>
  <meta charset='utf-8' />
  <meta name='viewport' content='width=device-width,initial-scale=1' />
  <title>Player</title>
  <style>
    html, body {{
      height: 100%;
      margin: 0;
      background: black;
      overflow: hidden;
    }}
    #player-wrapper {{
      position: absolute;*/
      //inset: 0; /* top:0; right:0; bottom:0; left:0; */
   /* }}
    iframe {{
      width: 100%;
      height: 100%;
      border: 0;
      display: block;
    }}
  </style>
</head>
<body>
  <div id='player-wrapper'>
    <iframe id='yt' 
      src='https://www.youtube.com/embed/{videoId}?autoplay=1&controls=0&modestbranding=1&rel=0&playsinline=1&fs=1'
      allow='autoplay; fullscreen; picture-in-picture'
      allowfullscreen
      ></iframe>
  </div>

  <script>
    // Если нужно — можно ловить resize (но iframe 100% справится сам)
    window.addEventListener('resize', () => {{
      // noop
    }});
  </script>
</body>
</html>";
                            context.Response.ContentType = "text/html; charset=utf-8";
                            await context.Response.WriteAsync(html);
                        });
                    });
                });
            })
            .Build();

            _webHost.Start();*/


            AppVM = new AppViewModel();
            this.DataContext = AppVM;

            InitializeComponent();
            MainFrame.Navigate(new MainPage { DataContext = AppVM.MainVM});

            AppVM.ChatClient.Client_Connect();
        }
/*
        protected override void OnClosed(EventArgs e)
        {
            _webHost?.StopAsync().GetAwaiter().GetResult();
            _webHost?.Dispose();
            base.OnClosed(e);
        }*/
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

    }
}
