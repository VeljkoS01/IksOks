namespace IksOks.Web.Contracts.Store;

public sealed record PurchaseStoreItemResponse(
    string ItemKey,
    int TokenBalance);