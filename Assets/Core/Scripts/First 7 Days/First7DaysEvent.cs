namespace Game
{
    [System.Serializable]
    public struct First7DaysEvent {
        public int[] signup;
        public int[] recharge;
        public First7DaysEvent(int[] signup, int[] recharge) {
            this.signup = signup;
            this.recharge = recharge;
        }
    }
}