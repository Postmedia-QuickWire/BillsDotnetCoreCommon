using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Common.Services
{
	// this service watches for too many login attempts over a short period of time
	// add service as a singleton, inject into your public accounts controller
	// note that the IPAddr is only for logging
	public interface ILoginWatcherService
	{
		public int KeepFailedAttemptsSecs { get; set; }
		public int NumAttemptsAllowed { get; set; }

		// call this BEFORE attempting ANY login, returns TRUE if max attempts are still pending
		public bool CheckPendingAttempsBeforeLogin(string username, string IPAddr);

		// call this after a FAILED login attempt, return TRUE if max attempts are hit
		public bool CheckAndCacheFailedAttempt(string username, string IPAddr);

		// clear user attempt cache/queue
		public void ClearUserLoginAttemptQueue(string username);
	}

	class LoginWatcherService : ILoginWatcherService
	{
		private readonly ILogger<LoginWatcherService> _logger;
		private ConcurrentDictionary<string, Queue<DateTime>> _AttemptsCache = new ConcurrentDictionary<string, Queue<DateTime>>();
		const int MAX_QUEUE_COUNT = 50;
		public int KeepFailedAttemptsSecs { get; set; } = 60;
		public int NumAttemptsAllowed { get; set; } = 4;

		public LoginWatcherService(ILogger<LoginWatcherService> logger)
		{
			_logger = logger;
		}


		// returns True if TOO MANY ATTEMPTS in X seconds
		// will also enqueue as a FAILED attempt - prolonging the pause time
		public bool CheckPendingAttempsBeforeLogin(string username, string IPAddr)
		{
			var attemptsCount = CheckAndOrCacheFailedAttempt(username, IPAddr, false);

			if (attemptsCount > NumAttemptsAllowed)
			{
				// and check for a MAX count to prevent DOS attack from wasting us
				if (attemptsCount < MAX_QUEUE_COUNT)
				{
					attemptsCount = CheckAndOrCacheFailedAttempt(username, IPAddr, true);
				}
				else
				{
					// email alert here or Invoke an event, will need to latch it though
				}
			}
			return attemptsCount > NumAttemptsAllowed;
		}


		// return true if too amny failed attempts - caller should post a msg
		public bool CheckAndCacheFailedAttempt(string username, string IPAddr)
		{
			var attemptsCount = CheckAndOrCacheFailedAttempt(username, IPAddr, true); // note the enqueue flag
			if (attemptsCount > NumAttemptsAllowed)
			{
				_logger.LogWarning("Too many login attempts ({cnt} over the last {sec} secs) for user: {usr}, ip: {ip}, pausing login"
					, attemptsCount, KeepFailedAttemptsSecs, username, IPAddr);
				return true; // caller must (should) notify user of the login pause
			}
			return false;
		}

		public void ClearUserLoginAttemptQueue(string username)
		{
			Queue<DateTime> userQueue;
			username = username.Trim().ToLower();
			if (_AttemptsCache.TryRemove(username, out userQueue))
			{
				userQueue.Clear(); // not really needed right? maybe we should clear it but keep the queue in the cache?
			}
		}


		protected int CheckAndOrCacheFailedAttempt(string username, string IPAddr, bool enqueue)
		{
			try
			{
				Queue<DateTime> userQueue;
				username = username.Trim().ToLower();
				if (!_AttemptsCache.TryGetValue(username, out userQueue))
				{
					userQueue = new Queue<DateTime>(); // note this could stay forever, see ClearUserLoginAttemptQueue
					_AttemptsCache[username] = userQueue;
				}

				// simply an X seconds queue per Y failed attempts
				var cleanupTime = DateTime.Now.AddSeconds(-KeepFailedAttemptsSecs);
				int attemptsCount = 0;
				lock (userQueue)
				{
					while (userQueue.Count != 0 && userQueue.Peek() < cleanupTime)
					{
						userQueue.Dequeue();
					}

					if (enqueue)
						userQueue.Enqueue(DateTime.Now);
					attemptsCount = userQueue.Count;
				}

				return attemptsCount;
			}
			catch(Exception e)
			{
				// not sure if an exception can happen here
				// may be worse logging it. maybe better let if fall through?
				_logger.LogCritical(e, "CheckAndOrCacheFailedAttempt exception, {m}, user:{u}, ip: {ip}", e.Message, username, IPAddr);
			}

			return MAX_QUEUE_COUNT; //should basically prevent login from happening
		}



	}
}
