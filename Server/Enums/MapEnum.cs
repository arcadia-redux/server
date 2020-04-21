using System.ComponentModel;

namespace Server.Enums
{
    public enum MapEnum
    {
        [Description("mines_trio")]
        MinesTrio,
        [Description("desert_duo")]
        DesertDuo,
        [Description("forest_solo")]
        ForestSolo,
        [Description("desert_quintet")]
        DesertQuintet,
        [Description("temple_quartet")]
        TempleQuartet,
        [Description("desert_octet")]
        DesertOctet,
        [Description("temple_sextet")]
        TempleSextet,
        [Description("core_quartet")]
        CoreQuartet
    }
}
