using System.Diagnostics;
using BackendProjectTemplate.Domain.Common.Observability;

namespace BackendProjectTemplate.Infrastructure.Observability;

public sealed class CustomTelemetryContext : ICustomTelemetryContext
{
    public ICustomTelemetryContext SetProperty(string key, string value)
    {
        Activity.Current?.SetTag(key, value);
        return this;
    }

    public ICustomTelemetryContext AddCustomEvent(string name, Dictionary<string, string>? properties = null)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return this;
        }

        if (properties is null || properties.Count == 0)
        {
            activity.AddEvent(new ActivityEvent(name));
            return this;
        }

        var tags = new ActivityTagsCollection();
        foreach (var property in properties)
        {
            tags.Add(property.Key, property.Value);
        }

        activity.AddEvent(new ActivityEvent(name, tags: tags));
        return this;
    }
}
