﻿using SkyEditor.RomEditor.Domain.Rtdx.Constants;
using YamlDotNet.Serialization;

namespace SkyEditor.RomEditor.Domain.Rtdx.Structures.Executable
{
    public partial class PegasusActDatabase
    {
        public class ActorData
        {
            public string SymbolName { get; set; } = default!;
            public CreatureIndex PokemonIndex { get; set; }
            public PokemonFormType FormType { get; set; }
            public bool IsFemale { get; set; }
            public PegasusActorDataPartyId PartyId { get; set; }
            public PokemonFixedWarehouseId WarehouseId { get; set; } = default!;
            public TextIDHash SpecialName { get; set; } = default!;
            public string? DebugName { get; set; }

            [YamlIgnore]
            public ulong PokemonIndexOffset { get; set; }

            [YamlIgnore]
            public bool PokemonIndexEditable { get; set; }
        }
    }
}
