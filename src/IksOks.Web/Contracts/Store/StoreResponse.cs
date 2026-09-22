namespace IksOks.Web.Contracts.Store;

public sealed record StoreResponse(
    int TokenBalance,
    string? ActiveBorderKey,
    IReadOnlyList<StoreItemResponse> Items);