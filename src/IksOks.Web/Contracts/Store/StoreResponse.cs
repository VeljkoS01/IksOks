namespace IksOks.Web.Contracts.Store;

public sealed record StoreResponse(
    int TokenBalance,
    IReadOnlyList<StoreItemResponse> Items);