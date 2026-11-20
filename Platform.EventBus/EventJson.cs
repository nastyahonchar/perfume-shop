using System.Text.Json;

namespace Platform.EventBus;

public static class EventJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}