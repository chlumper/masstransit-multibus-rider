using MassTransit.EventHubIntegration;
using MassTransit.Registration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace WebAppTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {

        private readonly ILogger<TestController> _logger;
        private readonly IEventHubProducerProvider _provider;

        // we want to inject something here which allows us to produce a message at event hub
        // (the only workaround found was to inject "IBusInstance" but the actual instance might depend on order of bus setup)
        public TestController(ILogger<TestController> logger, IBusInstance<IAzureEventHubBus> busInstance) // <-- error:  Unable to resolve service for type 'MassTransit.Registration.IBusInstance`1[WebAppTest.IAzureEventHubBus]' while attempting to activate...
        {
            _logger = logger;

            IEventHubRider rider = busInstance.GetRider<IEventHubRider>();
            _provider = rider.GetProducerProvider();
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            _logger.LogInformation("Publishing message...");
            IEventHubProducer producer = await _provider.GetProducer("<my-eventhub>"); // <-- event hub name
            await producer.Produce<EventHubMessage>(new { Text = $"Hello, Computer. {DateTimeOffset.Now:O}" });
            return Ok();
        }
    }

    public interface EventHubMessage
    {
        string Text { get; }
    }
}
