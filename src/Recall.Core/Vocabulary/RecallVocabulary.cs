namespace Recall.Core.Vocabulary;

public enum Modality
{
    Text = 0,
    Image = 1,
    Video = 2,
    Audio = 3
}

public enum StoredWidth
{
    W768 = 768,
    W512 = 512,
    W256 = 256
}

public enum AgeCutoffYears
{
    Two = 2,
    Five = 5,
    Seven = 7
}

public enum OpLogOperation
{
    Insert = 0,
    Delete = 1,
    Pin = 2,
    Unpin = 3
}

public enum PinResult
{
    Regrown = 0,
    MissingFileStays = 1,
    UnpinReturnedShort = 2,
    UnpinStaysLong = 3,
    AlreadyLong = 4,
    GateFailedStaysLong = 5,
    NotMedia = 6,
    NotPinned = 7
}
