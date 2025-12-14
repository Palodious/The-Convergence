public interface IAmmo
{
    void AddAmmo(int amount);
    int GetCurrentAmmo(gunStats gunType);
    int GetMaxAmmo(gunStats gunType);
    bool CanAddAmmo();
}