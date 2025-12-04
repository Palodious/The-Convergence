// Every script that needs to save data will implement this interface.
// CaptureState() grabs whatever values I want to keep between sessions.
// RestoreState() puts those values back when loading a save.
public interface ISaveable
{
    object CaptureState();
    void RestoreState(object state);
}
