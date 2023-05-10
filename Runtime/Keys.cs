namespace Fromiel.LobbyPlugin
{
    public static class Keys
    { 
        private const string WaitingVal = "0"; //Code of the server if the game has not started yet
        public static string Waiting => WaitingVal;

        private const string KeyStartGameVal = "KeyStartGame"; //Key to access the value of the server to know if the game has started
        public static string KeyStartGame => KeyStartGameVal;
    }
}




