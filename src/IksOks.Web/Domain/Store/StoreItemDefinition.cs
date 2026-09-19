namespace IksOks.Web.Domain.Store;

public sealed record StoreItemDefinition(
    string Key,
    string Name,
    string Type,
    string Value,
    int Price);