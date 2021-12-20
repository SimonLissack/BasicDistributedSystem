using Microsoft.AspNetCore.Mvc;
using WebClient.Models;
using WebClient.Services;

namespace WebClient.Controllers;

[ApiController]
[Route("[controller]")]
public class PingController : ControllerBase
{
    readonly IPingRepository _pingRepository;

    public PingController(IPingRepository pingRepository)
    {
        _pingRepository = pingRepository;
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
    public IActionResult Ping(int delayInSeconds)
    {
        var ping = new PingModel
        {
            Id = Guid.NewGuid(),
            RequestedAt = DateTime.UtcNow,
            DelayInSeconds = delayInSeconds
        };

        _pingRepository.SaveModel(ping);

        // Send to queue

        return Ok(ping);
    }
}
