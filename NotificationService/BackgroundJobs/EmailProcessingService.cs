//using NotificationService.BackgroundJobs;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.DependencyInjection;
//using System;
//using System.Threading;
//using System.Threading.Tasks;

//namespace NotificationService.Services
//{
//    public class EmailProcessingService : BackgroundService
//    {
//        private readonly IServiceProvider _serviceProvider;
//        private readonly ILogger<EmailProcessingService> _logger;

//        public EmailProcessingService(IServiceProvider serviceProvider, ILogger<EmailProcessingService> logger)
//        {
//            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        }

//        /// <summary>
//        /// Executes the background service to process scheduled emails periodically.
//        /// </summary>
//        /// <param name="stoppingToken">Token to monitor for cancellation requests.</param>
//        /// <returns>A task that represents the execution of the background service.</returns>
//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            _logger.LogInformation("Email processing service started at: {time}", DateTime.UtcNow);

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    // Create a scope to resolve the scoped EmailJob
//                    using (var scope = _serviceProvider.CreateScope())
//                    {
//                        var emailJob = scope.ServiceProvider.GetRequiredService<EmailJob>();

//                        // Process scheduled emails using EmailJob
//                        await emailJob.ProcessScheduledEmailsAsync(stoppingToken);
//                    }
//                }
//                catch (TaskCanceledException)
//                {
//                    _logger.LogWarning("Email processing task was canceled.");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "An error occurred while processing scheduled emails at {time}", DateTime.UtcNow);
//                }

//                // Wait 1 minute before processing again, unless cancellation is requested
//                try
//                {
//                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
//                }
//                catch (TaskCanceledException)
//                {
//                    _logger.LogWarning("Delay task was canceled. Stopping service.");
//                    break;
//                }
//            }

//            _logger.LogInformation("Email processing service stopped at: {time}", DateTime.UtcNow);
//        }

//        /// <summary>
//        /// Executes any additional cleanup when the service is stopping.
//        /// </summary>
//        /// <param name="cancellationToken">Cancellation token for the stop operation.</param>
//        /// <returns>A task that represents the stop operation.</returns>
//        public override async Task StopAsync(CancellationToken cancellationToken)
//        {
//            _logger.LogInformation("Email processing service is stopping at: {time}", DateTime.UtcNow);
//            await base.StopAsync(cancellationToken);
//        }
//    }
//}
