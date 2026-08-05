using PaperlessScanBridge.Application.Profiles;
using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.UnitTests;

public sealed class ProfileDefaultsServiceTests
{
    [Fact]
    public async Task RejectsAStaleScannerChoiceWithoutOverwritingStoredDefaults()
    {
        var repository = new RepositoryStub();
        var service = new ProfileDefaultsService(repository, new ScannerRepositoryStub(), new CurrentProfileStub());
        var value = new ProfileDefaults(42,"Missing source",ScanColorMode.Color,300,null,null,null,[],DateTimeOffset.MinValue);
        var result = await service.SaveAsync(value);
        Assert.False(result.IsValid); Assert.Equal(2, result.Errors.Count); Assert.Equal(0, repository.SaveCalls);
    }

    private sealed class RepositoryStub : IProfileDefaultsRepository
    {
        public int SaveCalls {get;private set;} public Task<ProfileDefaults> GetAsync(string profileId, CancellationToken c=default)=>throw new NotImplementedException();
        public Task SaveAsync(string profileId, ProfileDefaults d,CancellationToken c=default){SaveCalls++;return Task.CompletedTask;} public Task ResetAsync(string profileId, CancellationToken c=default)=>Task.CompletedTask;
    }
    private sealed class CurrentProfileStub : ICurrentProfileAccessor
    {
        public Task<UserProfile> GetRequiredAsync(CancellationToken cancellationToken = default) => Task.FromResult(new UserProfile("test-profile", "test", "subject", "Test", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    private sealed class ScannerRepositoryStub : ISelectedScannerRepository
    {
        public Task<SelectedScanner?> GetByIdAsync(long id,CancellationToken c)=>Task.FromResult<SelectedScanner?>(new(id,"HP","1",80,"http","http://1",DateTimeOffset.UtcNow,"dev",["ADF"],[200]));
        public Task<SelectedScanner?> GetAsync(CancellationToken c)=>throw new NotImplementedException(); public Task<IReadOnlyList<SelectedScanner>> ListAsync(CancellationToken c)=>throw new NotImplementedException();
        public Task<SelectedScanner> SaveAsync(DiscoveredScanner s,DateTimeOffset d,CancellationToken c)=>throw new NotImplementedException(); public Task<SelectedScanner> SaveSaneProfileAsync(long id,ScannerDevice d,ScannerCapabilities p,CancellationToken c)=>throw new NotImplementedException();
    }
}
