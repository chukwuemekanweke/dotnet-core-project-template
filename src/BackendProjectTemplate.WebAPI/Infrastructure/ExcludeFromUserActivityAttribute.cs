namespace BackendProjectTemplate.WebAPI.Infrastructure;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ExcludeFromUserActivityAttribute : Attribute;
