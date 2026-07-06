using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;

namespace KoraxLib.Internal.Smoke;

/// <summary>
/// KoraxLib 注册链路的最小运行时验证 encounter。
/// </summary>
/// <remarks>
/// 这里故意复用原版 <see cref="Nibbit" />，而不是同时引入自定义 monster：当前验证目标是
/// <c>RegisterActEncounter</c> 到 <c>ActModel.GenerateAllEncounters()</c> 的合并链路。如果把自定义
/// monster、move state machine、视觉资源和本地化也放进同一个 smoke test，失败时很难判断根因。
/// </remarks>
internal sealed class KoraxSmokeEncounter : EncounterModel
{
    public override IEnumerable<EncounterTag> Tags => [EncounterTag.Nibbit];

    public override RoomType RoomType => RoomType.Monster;

    public override bool IsWeak => true;

    public override bool IsDebugEncounter => true;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<Nibbit>()];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var nibbit = (Nibbit)ModelDb.Monster<Nibbit>().ToMutable();
        nibbit.IsAlone = true;
        return [(nibbit, null)];
    }
}
