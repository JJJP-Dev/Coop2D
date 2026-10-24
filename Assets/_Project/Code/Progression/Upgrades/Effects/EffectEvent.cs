// When an OnEventBlock runs its actions. Stored in assets: never renumber one, add
// new ones at the end.
public enum EffectEvent
{
    // Right when the upgrade is taken: Vitalidad's heal, Respiro's third of a reroll.
    Acquired = 0,

    // The player casts a skill and the host accepts it: Tempo. The owner predicts it.
    SkillCast = 1,

    // A reaction the player takes part in, by putting the aura or by detonating it: Viento a favor,
    // Relevo, Asistencia. Only the host knows it.
    Reaction = 2,
}
