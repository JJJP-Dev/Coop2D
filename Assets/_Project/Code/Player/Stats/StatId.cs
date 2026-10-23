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
}
