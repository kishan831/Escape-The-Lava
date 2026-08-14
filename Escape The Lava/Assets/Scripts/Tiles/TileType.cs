namespace EscapeTheLava
{
    /// <summary>The three tile kinds from the brief.</summary>
    public enum TileType
    {
        /// <summary>Green Island - a purely safe visual tile. Tapping it does nothing.</summary>
        Island = 0,

        /// <summary>Red Lava - tapping it costs one life.</summary>
        Lava = 1,

        /// <summary>Blue Diamond - tapping it collects the diamond and scores.</summary>
        Diamond = 2
    }
}
