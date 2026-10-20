using TMS2000.Core.Exceptions;
using TMS2000.Core.Helper;
using TMS2000.Core.Models;
using TMS2000.Core.Repositories;
using TMS2000.Shared.DTO.Trades;
using TMS2000.Shared;

namespace TMS2000.Core.Services;

public class TradeService
{
    private readonly UserRepository _userRepository;
    private readonly ResourceRepository _resourceRepository;

    public TradeService(UserRepository userRepository, ResourceRepository resourceRepository)
    {
        _userRepository = userRepository;
        _resourceRepository = resourceRepository;
    }

    public async Task<TradeResponse> ExecuteAsync(string userId, TradeRequest request)
    {
        var type = request.Type!.Value;
        // non null : garanti par [Required] dans le DTO => je peux dire au compilateur en utilisant ! => c'est bon ça sera pas null.

        for (var attempt = 1; attempt <= GameSettings.TradeMaxAttempts; attempt++)
        // side note : la boucle try seulement si il y'a un echec de rédaction dans la bdd à cause des versions.
        {
            var user = await _userRepository.GetByIdAsync(userId)
                       ?? throw new NotFoundException("Joueur introuvable.");
            // synthax déjà vu : je re-comment =>  [ ?? => if left == null => apply right statement].

            var existing = user.Transactions.FirstOrDefault(t => t.TransactionId == request.TransactionId);

            if (existing != null)
                return ToResponse(existing, user.Balance);
            // above = vérifie si la transaction n'existe pas déjà => si c'est le cas, retourne la réponse déjà exisante => idempotence.

            var resource = await _resourceRepository.GetByIdAsync(request.ResourceId)
                           ?? throw new NotFoundException($"Ressource '{request.ResourceId}' introuvable.");

            var now = DateTime.UtcNow;
            var transaction = TradeEngine.Execute(
                user, resource, request.TransactionId, type, request.Quantity,
                resource.CurrentPrice, now, offline: false);
            // opérateur ternaire, en gros => si c'est de type buy ( statement à gauche right => do buy) sinon do sell.


            user.Transactions.Add(transaction);
            if (user.Transactions.Count > GameSettings.MaxTransactionsPerUser)
                user.Transactions.RemoveRange(0, user.Transactions.Count - GameSettings.MaxTransactionsPerUser);

            // removerange => prend à partir de 0, [number à remove], ici le nombre est en principe toujours 1, cela fonctionne quand ça depasse 200 MaxTransactionsPerUser.

            var expectedVersion = user.Version;
            user.Version++;

            if (await _userRepository.ReplaceIfVersionMatchesAsync(user, expectedVersion))
                return ToResponse(transaction, user.Balance);


        }

        throw new ConflictException("Le compte a été modifié par une autre opération en même temps. Réessayez.");
    }

    private static TradeResponse ToResponse(Transaction transaction, decimal balance) => new(
        TransactionId: transaction.TransactionId,
        Status: transaction.Status,
        ExecutedPrice: transaction.ExecutedPrice,
        Profit: transaction.Profit,
        Efficiency: transaction.Efficiency,
        NewBalance: balance);
}