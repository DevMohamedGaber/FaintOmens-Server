namespace Game.Network
{
    [System.Serializable]
    public enum NetworkError : byte
    {
        UnKnownError,
        InvalidAccount,
        InvalidAccountOrPassword,
        AccountBanned,
        AlreadyLogedIn,
        InvalidCharacterId,
        AccountNotInLobby,
        NameNotAllawed,
        ChooseClass,
        ChooseGender,
        ChooseTribe,
        MaxCharacters,
        NameExists,
    }
}