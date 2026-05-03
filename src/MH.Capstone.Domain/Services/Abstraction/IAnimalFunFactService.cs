namespace MH.Capstone.Domain.Services.Abstraction
{
    /// <summary>
    /// CSP-172: returns a short, human-readable "fun fact" string about a species,
    /// sourced from the API-Ninjas Animals API via <see cref="IApiCaller{TConfig}"/>
    /// and its SQL-backed cache.
    /// </summary>
    public interface IAnimalFunFactService
    {
        /// <summary>
        /// Resolves a fun fact for the given species name.
        /// Returns <c>null</c> when the species cannot be resolved, the API call fails,
        /// the species is the "Unknown" sentinel, or the response carries no fun-fact
        /// material. The caller is expected to render a friendly fallback in that case.
        /// </summary>
        Task<string?> GetFunFactAsync(string speciesName, CancellationToken cancellationToken = default);
    }
}
