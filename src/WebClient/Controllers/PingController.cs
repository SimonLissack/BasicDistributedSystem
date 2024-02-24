using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using WebClient.Models;
using WebClient.Services;

namespace WebClient.Controllers;

[ApiController]
[Route("[controller]")]
public class PingController : ControllerBase
{
    readonly IPingRepository _pingRepository;
    readonly IWorkRequestPublisherService _publisherService;

    public PingController(IPingRepository pingRepository, IWorkRequestPublisherService publisherService)
    {
        _pingRepository = pingRepository;
        _publisherService = publisherService;
    }

    [HttpGet]
    public IActionResult GetPings()
    {
        return Ok(_pingRepository.GetAll());
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetPing(Guid id)
    {
        if (_pingRepository.TryGetModel(id, out var pingModel))
        {
            return Ok(pingModel);
        }

        return NotFound(id);
    }

    [HttpPost]
    public async Task<IActionResult> Ping(int delayInSeconds)
    {
        var ping = new PingModel
        {
            Id = Guid.NewGuid(),
            RequestedAt = DateTime.UtcNow,
            DelayInSeconds = delayInSeconds
        };

        _pingRepository.SaveModel(ping);

        var traceId = await _publisherService.PublishWorkRequest(ping.Id, ping.DelayInSeconds);

        if (traceId is not null)
        {
            Response.Headers.Add("x-trace-id", new StringValues(traceId));
        }

        return Ok(ping);
    }

    [HttpDelete]
    public IActionResult Delete(Guid id)
    {
        _pingRepository.DeleteModel(id);
        return Ok();
    }
}
