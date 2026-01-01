using System;

public static class GameEvents
{
    public static Action<AccountResponse> OnPlayerDataReceived;
    public static Action<string> OnUsernameSubmitted;
    public static Action<UsernameResponseType> OnUsernameSubmittedResponse;
}