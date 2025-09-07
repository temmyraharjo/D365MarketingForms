using Microsoft.PowerPlatform.Dataverse.Client;

namespace D365MarketingForms.Server.Services
{
    public interface IDataverseService
    {
        ServiceClient GetClient();
        Task<bool> IsConnectionActiveAsync();
    }

    public class DataverseService : IDataverseService
    {
        private readonly ServiceClient _serviceClient;
        private readonly ILogger<DataverseService> _logger;

        public DataverseService(IConfiguration configuration, ILogger<DataverseService> logger)
        {
            _logger = logger;
            
            try
            {
                if (configuration["Dataverse:UseConnectionString"].Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    string connectionString = configuration["Dataverse:ConnectionString"];
                    _serviceClient = new ServiceClient(connectionString);
                }
                else
                {
                    // Alternative initialization method if needed
                    _serviceClient = new ServiceClient(
                        new Uri(configuration["Dataverse:Url"]),
                        configuration["Dataverse:ClientId"],
                        configuration["Dataverse:ClientSecret"],
                        useUniqueInstance: true);
                }
                
                if (!_serviceClient.IsReady)
                {
                    _logger.LogError("Failed to connect to Dataverse: {0}", _serviceClient.LastError);
                    throw new Exception("Failed to connect to Dataverse: " + _serviceClient.LastError);
                }
                
                _logger.LogInformation("Connected to Dataverse successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Dataverse service");
                throw;
            }
        }

        public ServiceClient GetClient()
        {
            return _serviceClient;
        }

        public async Task<bool> IsConnectionActiveAsync()
        {
            try
            {
                // A simple way to check if the connection is active
                await _serviceClient.WhoAmIAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Dataverse connection");
                return false;
            }
        }
    }
}