// Which part of a reaction an OnEventBlock listens to (the catalog's «role» filter). Stored in assets:
// add new ones at the end.
public enum ReactionRole
{
    // Either part: the aura was yours, or you detonated it.
    Any = 0,

    // The aura that was consumed was yours: Relevo.
    Aura = 1,

    // Your skill detonated it: Viento a favor, Asistencia.
    Detonator = 2,
}
