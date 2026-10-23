// Who an action works on (#161, catalog «target»). Stored in assets: add new ones at the end.
public enum EffectTarget
{
    // The player who has the upgrade.
    Self = 0,

    // The other player of a duo reaction: Asistencia gives back cooldown to whoever put the aura.
    // Without one, the action does nothing.
    Partner = 1,
}
