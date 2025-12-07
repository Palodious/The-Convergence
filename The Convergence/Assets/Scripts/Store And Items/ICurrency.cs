public interface ICurrency
{
    // Name of the currency type
    string Name { get; }

    // Current amount held
    int Amount { get; set; }
}