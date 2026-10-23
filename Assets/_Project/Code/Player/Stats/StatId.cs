// A stat of a player (#157), from the progression catalog's list (A.2). Upgrades change them with
// StatModifier blocks (#161), and the systems that use them ask PlayerStats for the final value.
// The numbers travel over the network and are stored in assets: never renumber one, add new ones at the end.
public enum StatId : byte
{
    // max_hp: maximum health.
    MaxHealth = 0,

    // move_speed: units per second.
    MoveSpeed = 1,

    // damage_taken: a multiplier on every hit the player takes. Its base is 1.
    DamageTaken = 2,

    // pickup_radius: how close an experience orb must be to fly to the player.
    PickupRadius = 3,

    // basic_damage, basic_interval, basic_range: the basic attack's damage, seconds between shots and reach.
    BasicDamage = 4,
    BasicInterval = 5,
    BasicRange = 6,

    // basic_projectiles: projectiles per basic attack. Its base is 1 (#161, Doble disparo).
    BasicProjectiles = 7,

    // The degrees those projectiles fan out across. Its base is 0. Not in the catalog's list: it's how
    // AttackModifier says how wide a volley is.
    BasicSpread = 8,

    // aura_duration: seconds the auras this player applies last, one stat for each strength. Their bases
    // are in AuraSettings (#161, Auras tenaces).
    StrongAuraDuration = 9,
    WeakAuraDuration = 10,

    // How far around a skill's circle its aura also reaches, and on how many more enemies, without
    // damage and without detonating. Both have base 0 (#161, Aura expansiva).
    SkillAuraExtraRadius = 11,
    SkillAuraExtraTargets = 12,
}
