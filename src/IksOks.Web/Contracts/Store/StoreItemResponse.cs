namespace IksOks.Web.Contracts.Store;

public sealed record StoreItemResponse(
    string Key,
    string Name,
    string Type,
    string Value,
    int Price,
    bool IsOwned,
    bool IsActive);