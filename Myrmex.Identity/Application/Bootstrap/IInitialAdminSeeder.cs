namespace Myrmex.Identity.Application.Bootstrap;

public interface IInitialAdminSeeder
{
    Task<InitialAdminBootstrapResult> SeedAsync(CancellationToken cancellationToken);
}
