namespace MerchantFraud.Ml;

public sealed class FraudModelRegistry
{
    private readonly List<FraudModelVersion> _versions = new();

    public IReadOnlyList<FraudModelVersion> Versions => _versions;

    public void Register(FraudModelVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);
        if (_versions.Any(existing => string.Equals(existing.Version, version.Version, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Model version '{version.Version}' is already registered.");
        }

        if (version.Status == FraudModelStatus.Champion && Champion() is not null)
        {
            throw new InvalidOperationException("A champion model already exists. Promote or retire it first.");
        }

        _versions.Add(version);
    }

    public FraudModelVersion Promote(string version)
    {
        var index = IndexOf(version);
        var target = _versions[index];
        if (target.Status == FraudModelStatus.Champion)
        {
            return target;
        }

        var championIndex = _versions.FindIndex(candidate => candidate.Status == FraudModelStatus.Champion);
        if (championIndex >= 0 && championIndex != index)
        {
            _versions[championIndex] = _versions[championIndex] with { Status = FraudModelStatus.Retired };
        }

        _versions[index] = target with { Status = FraudModelStatus.Champion };
        return _versions[index];
    }

    public FraudModelVersion Retire(string version)
    {
        var index = IndexOf(version);
        var current = _versions[index];
        if (current.Status == FraudModelStatus.Champion)
        {
            throw new InvalidOperationException("Champion model cannot be retired; promote another model first.");
        }

        _versions[index] = current with { Status = FraudModelStatus.Retired };
        return _versions[index];
    }

    public FraudModelVersion? Champion() => _versions.FirstOrDefault(candidate => candidate.Status == FraudModelStatus.Champion);

    private int IndexOf(string version)
    {
        var index = _versions.FindIndex(candidate => string.Equals(candidate.Version, version, StringComparison.Ordinal));
        if (index < 0)
        {
            throw new KeyNotFoundException($"Model version '{version}' is not registered.");
        }

        return index;
    }
}
