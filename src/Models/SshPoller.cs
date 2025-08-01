using System;
using System.Threading;

namespace SourceGit.Models
{
    public class SshPoller : IDisposable
    {
        private readonly Timer _timer;
        private readonly IRepository _repo;

        public SshPoller(IRepository repo)
        {
            _repo = repo;
            _timer = new Timer(OnTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromSeconds(600));
        }

        private void OnTimerElapsed(object state)
        {
            try
            {
                _repo.RefreshAll();
            }
            catch (Exception ex)
            {
                // Log or handle exceptions as needed
                // For now, silently continue to avoid breaking the polling
                Console.WriteLine($"Error during repository refresh: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
