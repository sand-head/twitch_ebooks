using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Database.Models;

namespace TwitchEbooks2.Infrastructure
{
    public class ChannelService
    {
        private readonly TwitchEbooksContext _context;

        public ChannelService(TwitchEbooksContext context)
        {
            _context = context;
        }

        public async Task AddAsync(uint channelId)
        {
            if (_context.Channels.Any(c => c.Id == channelId))
                return;

            _context.Channels.Add(new TwitchChannel
            {
                Id = channelId
            });
            await _context.SaveChangesAsync();
            // todo: also connect client to channel, generate pool
            // probably do that using mediatr
        }

        public async Task RemoveAsync(uint channelId)
        {
            var channel = _context.Channels.FirstOrDefault(c => c.Id == channelId);
            if (channel is null)
                return;

            _context.Channels.Remove(channel);
            await _context.SaveChangesAsync();
            // todo: also disconnect client from channel, delete pool
            // probably do that using mediatr
        }

        public Task Get(uint channelId)
        {
            throw new NotImplementedException();
        }
    }
}
