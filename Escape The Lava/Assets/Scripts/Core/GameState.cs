namespace EscapeTheLava
{
    /// <summary>Round lifecycle. Input is only accepted in <see cref="Playing"/>.</summary>
    public enum GameState
    {
        /// <summary>Before the first round has been started.</summary>
        Boot,

        /// <summary>Board sweeping in and the GO! banner playing. The timer has not started yet.</summary>
        Intro,

        Playing,

        /// <summary>All diamonds collected before the timer ran out.</summary>
        Won,

        /// <summary>Timer hit zero, or every life was lost.</summary>
        Lost
    }

    /// <summary>Why the round ended. Drives the copy on the end screen.</summary>
    public enum EndReason
    {
        None,
        AllDiamondsCollected,
        TimeUp,
        OutOfLives
    }
}
