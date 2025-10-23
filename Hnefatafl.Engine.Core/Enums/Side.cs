namespace Hnefatafl.Engine.Enums
{
    [Flags]
    public enum Side
    {
        None = 0,
        Attackers = 1,
        Defenders = 2,
        All = Attackers | Defenders
    }
}
