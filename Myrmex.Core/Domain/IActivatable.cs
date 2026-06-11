namespace Myrmex.Core.Domain;

public interface IActivatable
{
    bool IsActive { get; }

    void Deactivate();

    void Reactivate();
}
