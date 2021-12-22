using System.Collections.Concurrent;
using WebClient.Models;

namespace WebClient.Services;

public interface IPingRepository
{
    IEnumerable<PingModel> GetAll();
    bool TryGetModel(Guid id, out PingModel? pingModel);
    void SaveModel(PingModel model);
    PingModel? DeleteModel(Guid id);
}

public class InMemoryPingRepository : IPingRepository
{
    readonly ConcurrentDictionary<Guid, PingModel> _pings = new ();

    public IEnumerable<PingModel> GetAll() => _pings.Values;

    public bool TryGetModel(Guid id, out PingModel? pingModel) => _pings.TryGetValue(id, out pingModel);

    public void SaveModel(PingModel model) => _pings[model.Id] = model;

    public PingModel? DeleteModel(Guid id)
    {
        _pings.Remove(id, out var model);
        return model;
    }
}
