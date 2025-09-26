using DesktopTune.Model;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopTune.Services
{
    public class PlayerHub : Hub
    {
        private readonly Player _player;

        public PlayerHub(Player player)
        {
            _player = player;
        }

        public async Task<Music> Next()
        {
            var next = _player.GetNext();
            return next;
        }

        public async Task<int> GetVolume()
        {
            var volume = await _player.GetVolume();
            return volume;
        }

        public async Task MusicEnd()
        {
            await _player.MusicEnd();
        }
    }
}
