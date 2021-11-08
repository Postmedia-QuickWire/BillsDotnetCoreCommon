using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Common.Classes
{

	// Allows a hosted service to be DI injected anywhere
	// 
	// To use, first add your SINGLETON service, then add it's wrapped interface as a hosted service
	// services.AddSingleton<ISingletonInterface, SingletonImplementation>();
	// services.AddHostedService<BackgroundSerivceWrapper<ISingletonInterface>>();
	// 
	// You service just needs to implement IBackgroundServiceRunner.DoRun
	 
	public interface IBackgroundServiceRunner
	{
		public Task DoRun(CancellationToken stoppingToken);
		public void OnStart(CancellationToken cancellationToken);
		public void OnStop(CancellationToken cancellationToken);
	}

	public class BackgroundSerivceWrapper<T> : BackgroundService
	{
		private readonly IBackgroundServiceRunner _service;
		public BackgroundSerivceWrapper(T service)
		{
			_service = (IBackgroundServiceRunner)service;
		}
		protected async override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await _service.DoRun(stoppingToken).ConfigureAwait(false);
		}
		//public virtual void Dispose();

		public override async Task StartAsync(CancellationToken cancellationToken)
		{
			_service.OnStart(cancellationToken);
			await base.StartAsync(cancellationToken);
		}
		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			_service.OnStop(cancellationToken);
			await base.StopAsync(cancellationToken);
		}

	}
}
