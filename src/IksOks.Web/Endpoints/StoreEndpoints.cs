using System.Security.Claims;
using IksOks.Web.Contracts.Store;
using IksOks.Web.Domain.Entities;
using IksOks.Web.Domain.Store;
using IksOks.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IksOks.Web.Endpoints;

public static class StoreEndpoints
{
    public static IEndpointRouteBuilder MapStoreEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/store")
            .RequireAuthorization();

        group.MapGet(
            "",
            GetStoreAsync);

        group.MapPost(
            "/{itemKey}/purchase",
            PurchaseItemAsync);

        return endpoints;
    }

    private static async Task<IResult> GetStoreAsync(
        ClaimsPrincipal principal,
        IksOksDbContext db,
        CancellationToken cancellationToken)
    {
        var userIdValue = principal
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (!Guid.TryParse(
            userIdValue,
            out var userId))
        {
            return Results.Unauthorized();
        }

        var tokenBalance = await db.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => (int?)user.TokenBalance)
            .SingleOrDefaultAsync(cancellationToken);

        if (tokenBalance is null)
        {
            return Results.Unauthorized();
        }

        var ownedKeys = await db.UserPurchases
            .AsNoTracking()
            .Where(purchase =>
                purchase.UserId == userId)
            .Select(purchase =>
                purchase.ItemKey)
            .ToListAsync(cancellationToken);

        var ownedSet =
            ownedKeys.ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        var items =
            StoreCatalog.Items
                .Select(item =>
                    new StoreItemResponse(
                        item.Key,
                        item.Name,
                        item.Type,
                        item.Value,
                        item.Price,
                        ownedSet.Contains(item.Key)))
                .ToList();

        return Results.Ok(
            new StoreResponse(
                tokenBalance.Value,
                items));
    }

    private static async Task<IResult> PurchaseItemAsync(
        string itemKey,
        ClaimsPrincipal principal,
        IksOksDbContext db,
        CancellationToken cancellationToken)
    {
        var userIdValue = principal
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (!Guid.TryParse(
            userIdValue,
            out var userId))
        {
            return Results.Unauthorized();
        }

        var item =
            StoreCatalog.FindByKey(itemKey);

        if (item is null)
        {
            return Results.NotFound(new
            {
                error = "Store item was not found."
            });
        }

        await using var transaction =
            await db.Database.BeginTransactionAsync(
                cancellationToken);

        var alreadyOwned =
            await db.UserPurchases
                .AnyAsync(
                    purchase =>
                        purchase.UserId == userId &&
                        purchase.ItemKey == item.Key,
                    cancellationToken);

        if (alreadyOwned)
        {
            return Results.Conflict(new
            {
                error = "You already own this item."
            });
        }

        var updatedRows = await db.Users
            .Where(user =>
                user.Id == userId &&
                user.TokenBalance >= item.Price)
            .ExecuteUpdateAsync(
                setters =>
                    setters.SetProperty(
                        user => user.TokenBalance,
                        user =>
                            user.TokenBalance -
                            item.Price),
                cancellationToken);

        if (updatedRows == 0)
        {
            return Results.BadRequest(new
            {
                error = "You do not have enough tokens."
            });
        }

        db.UserPurchases.Add(
            new UserPurchase
            {
                UserId = userId,
                ItemKey = item.Key
            });

        try
        {
            await db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return Results.Conflict(new
            {
                error =
                    "The item could not be purchased."
            });
        }

        var tokenBalance = await db.Users
            .AsNoTracking()
            .Where(user =>
                user.Id == userId)
            .Select(user =>
                user.TokenBalance)
            .SingleAsync(cancellationToken);

        return Results.Ok(
            new PurchaseStoreItemResponse(
                item.Key,
                tokenBalance));
    }
}