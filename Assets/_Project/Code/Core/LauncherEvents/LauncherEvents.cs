// Every event of the launcher contract (docs/launcher-events.md, v1), one method each, with the ids it allows.
// The game tells facts from this player's point of view; it knows nothing about missions.
// Rules of the contract: never rename or remove an event, a field or an id, or change what it means.
// Adding new ones is always fine.
public static class LauncherEvents
{
    // Values of `by`.
    public const string ByMe = "me";
    public const string ByAlly = "ally";
    public const string ByNone = "none";

    // Values of `me` in a reaction.
    public const string RoleAura = "aura";
    public const string RoleDetonator = "detonator";
    public const string RoleBoth = "both";
    public const string RoleNone = "none";

    // --- Connection ---

    /// <param name="result">ok, port_in_use or error.</param>
    public static void HostStarted(string result)
    {
        LauncherEventWriter.Write("host_started", ("result", result));
    }

    /// <param name="target">loopback, lan, vpn, public or invalid. Never the address itself.</param>
    /// <param name="via">button or enter.</param>
    public static void JoinAttempt(string target, string via)
    {
        LauncherEventWriter.Write("join_attempt", ("target", target), ("via", via));
    }

    /// <param name="result">ok, invalid_ip, not_found, rejected or error.</param>
    public static void JoinResult(string result, string target, int milliseconds)
    {
        LauncherEventWriter.Write("join_result", ("result", result), ("target", target), ("ms", milliseconds));
    }

    // --- Match ---

    /// <param name="slot">From 1 to 4, as players count them.</param>
    /// <param name="seed">The match's seed in 8 hexadecimal characters. Null writes none.</param>
    /// <param name="character">The local player's character id: ember, tide, volt. Null writes none.</param>
    public static void MatchJoined(bool isHost, int slot, int players, int matchTime, bool late, string seed,
                                   string character)
    {
        LauncherEventWriter.Write("match_joined",
            ("role", isHost ? "host" : "client"), ("slot", slot), ("players", players),
            ("match_time", matchTime), ("late", late), ("seed", seed), ("character", character));
    }

    public static void PlayerJoined(int slot, int players)
    {
        LauncherEventWriter.Write("player_joined", ("slot", slot), ("players", players));
    }

    public static void PlayerLeft(int slot, int players)
    {
        LauncherEventWriter.Write("player_left", ("slot", slot), ("players", players));
    }

    /// <param name="reason">quit, host_closed, disconnected or error.</param>
    public static void MatchLeft(string reason, int duration)
    {
        LauncherEventWriter.Write("match_left", ("reason", reason), ("duration", duration));
    }

    /// <param name="index">From 1, in the order the waves start.</param>
    public static void WaveStarted(int index, int matchTime)
    {
        LauncherEventWriter.Write("wave_started", ("index", index), ("match_time", matchTime));
    }

    public static void WavesFinished(int matchTime, bool alive)
    {
        LauncherEventWriter.Write("waves_finished", ("match_time", matchTime), ("alive", alive));
    }

    /// <param name="level">The group's new level, from 2.</param>
    public static void LevelUp(int level, int matchTime)
    {
        LauncherEventWriter.Write("level_up", ("level", level), ("match_time", matchTime));
    }

    /// <param name="upgrade">Its id, as in the catalog. econ_breather is Respiro.</param>
    /// <param name="rarity">common, rare, epic, legendary, duo or evolution.</param>
    /// <param name="level">Level of the level up whose offer it came from.</param>
    public static void UpgradePicked(string upgrade, string rarity, int level, int matchTime)
    {
        LauncherEventWriter.Write("upgrade_picked", ("upgrade", upgrade), ("rarity", rarity), ("level", level),
                                  ("match_time", matchTime));
    }

    /// <param name="action">reroll, banish or skip.</param>
    /// <param name="upgrade">The banished upgrade's id. Null writes none: reroll and skip have none.</param>
    /// <param name="ability">With banish, the skill banished from an offer; null otherwise writes none.</param>
    public static void OfferAction(string action, string upgrade, int level, int matchTime, string ability = null)
    {
        LauncherEventWriter.Write("offer_action", ("action", action), ("upgrade", upgrade), ("level", level),
                                  ("match_time", matchTime), ("ability", ability));
    }

    /// <param name="ability">The skill's id: tide_bubble, ember_flare...</param>
    /// <param name="abilityLevel">1 for a new skill, 2 or 3 for a level of one the player has.</param>
    /// <param name="level">Level of the group's level up the offer came from.</param>
    public static void SkillGained(string ability, int abilityLevel, int level, int matchTime)
    {
        LauncherEventWriter.Write("skill_gained", ("ability", ability), ("ability_level", abilityLevel),
                                  ("level", level), ("match_time", matchTime));
    }

    /// <param name="result">defeat. Others (victory) come with the boss.</param>
    /// <param name="waves">Waves that had started.</param>
    public static void MatchOver(string result, int matchTime, int waves)
    {
        LauncherEventWriter.Write("match_over", ("result", result), ("match_time", matchTime), ("waves", waves));
    }

    /// <param name="slot">The downed player's, from 1 to 4.</param>
    /// <param name="me">True when it's the local player.</param>
    public static void PlayerDowned(int slot, bool me)
    {
        LauncherEventWriter.Write("player_downed", ("slot", slot), ("me", me));
    }

    /// <param name="slot">The revived player's, from 1 to 4.</param>
    /// <param name="me">True when it's the local player.</param>
    /// <param name="by">me, ally or none (a cheat).</param>
    /// <param name="chargesLeft">The group's revives left after this one. Negative writes none.</param>
    public static void PlayerRevived(int slot, bool me, string by, int chargesLeft)
    {
        LauncherEventWriter.Write("player_revived", ("slot", slot), ("me", me), ("by", by),
                                  ("left", chargesLeft >= 0 ? (object)chargesLeft : null));
    }

    // --- Combat ---

    /// <param name="source">character (the character's own element), debug_key (F6), or slot (older builds).</param>
    public static void ElementSet(ElementType element, string source)
    {
        LauncherEventWriter.Write("element_set", ("element", ElementId(element)), ("source", source));
    }

    /// <param name="ability">The skill's id: burst, mark...</param>
    public static void AbilityCast(string ability, ElementType element)
    {
        LauncherEventWriter.Write("ability_cast", ("ability", ability), ("element", ElementId(element)));
    }

    /// <param name="me">aura, detonator, both or none.</param>
    public static void Reaction(string variant, bool duo, string me, int targets)
    {
        LauncherEventWriter.Write("reaction", ("variant", variant), ("duo", duo), ("me", me), ("targets", targets));
    }

    /// <param name="enemy">chaser, dummy...</param>
    /// <param name="by">me, ally or none.</param>
    public static void EnemyKilled(string enemy, string by, DamageKind kind)
    {
        LauncherEventWriter.Write("enemy_killed", ("enemy", enemy), ("by", by), ("source", SourceId(kind, by)));
    }

    /// <param name="cheat">spawn_chaser, kill_all, element_cycle...</param>
    public static void CheatUsed(string cheat, bool byMe)
    {
        LauncherEventWriter.Write("cheat_used", ("cheat", cheat), ("by", byMe ? ByMe : ByAlly));
    }

    // --- Diagnostics ---

    public static void Hitch(int milliseconds, int enemies)
    {
        LauncherEventWriter.Write("hitch", ("ms", milliseconds), ("enemies", enemies));
    }

    public static void Perf(double averageFps, int worstFrameMs, int enemies)
    {
        LauncherEventWriter.Write("perf", ("fps_avg", averageFps), ("frame_worst_ms", worstFrameMs), ("enemies", enemies));
    }

    public static void Net(int roundTripMs)
    {
        LauncherEventWriter.Write("net", ("rtt_ms", roundTripMs));
    }

    /// <param name="type">error, exception or assert.</param>
    /// <param name="signature">8 hex characters that group errors without writing them.</param>
    public static void LogError(string type, string signature, int count)
    {
        LauncherEventWriter.Write("log_error", ("type", type), ("sig", signature), ("count", count));
    }

    // --- Ids ---

    public static string ElementId(ElementType element)
    {
        return element switch
        {
            ElementType.Fire => "fire",
            ElementType.Water => "water",
            ElementType.Electric => "electric",
            _ => "none",
        };
    }

    public static string RarityId(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Rare => "rare",
            UpgradeRarity.Epic => "epic",
            UpgradeRarity.Legendary => "legendary",
            UpgradeRarity.Duo => "duo",
            UpgradeRarity.Evolution => "evolution",
            _ => "common",
        };
    }

    // What dealt the killing blow. Without an attacker, the only thing that kills today is a cheat.
    private static string SourceId(DamageKind kind, string by)
    {
        return kind switch
        {
            DamageKind.Basic => "basic",
            DamageKind.Skill => "ability",
            DamageKind.Reaction => "reaction",
            _ => by == ByNone ? "cheat" : "other",
        };
    }
}
