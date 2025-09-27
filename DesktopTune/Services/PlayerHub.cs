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

        public async Task<Music> GetNext()
        {
            var next = await _player.GetNext();
            return next;
        }

        public int GetVolume()
        {
            var volume = _player.GetVolume();
            return volume;
        }

        public Music GetNow()
        {
            var current = _player.CurrentMusic;
            return current;
        }
    }
}
