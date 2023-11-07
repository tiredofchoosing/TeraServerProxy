﻿namespace TeraCore.Game.Structures
{
    [Serializable]
    public class Battleground : MatchingInstance
    {
        public Battleground(uint id, string name, int lvl) : base(id, name, lvl) { }

        public Battleground(uint id) : base(id) { }
    }
}
