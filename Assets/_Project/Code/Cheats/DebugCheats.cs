using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

// Cheats for testing, in development builds only. Any player can ask for them,
// and the host checks the build and carries them out, like any other action.
public class DebugCheats : NetworkBehaviour
{
    [SerializeField] private DebugCheatsSettings _settings;

    [Tooltip("Optional: without it, Next Wave and Restart Waves only say there are no waves.")]
    [SerializeField] private WaveSpawner _waveSpawner;

    public DebugCheatsSettings Settings => _settings;

    private void Awake()
    {
        if (_settings == null)
            Debug.LogError("[DebugCheats] Cheats settings are not assigned.", this);
    }

    // Called by the panel on its own client. Each one asks the host.
    public void SpawnEnemy(int enemyIndex, int amountIndex)
    {
        if (IsClientInitialized)
            ServerSpawnEnemy(enemyIndex, amountIndex);
    }

    public void KillAllEnemies()
    {
        if (IsClientInitialized)
            ServerKillAllEnemies();
    }

    public void SkipToNextWave()
    {
        if (IsClientInitialized)
            ServerSkipToNextWave();
    }

    public void RestartWaves()
    {
        if (IsClientInitialized)
            ServerRestartWaves();
    }

    public void ToggleInvulnerable()
    {
        if (IsClientInitialized)
            ServerToggleInvulnerable();
    }

    public void DamageAll()
    {
        if (IsClientInitialized)
            ServerDamageAll();
    }

    public void HealAll()
    {
        if (IsClientInitialized)
            ServerHealAll();
    }

    public void GiveXp()
    {
        if (IsClientInitialized)
            ServerGiveXp();
    }

    public void GiveReroll()
    {
        if (IsClientInitialized)
            ServerGiveReroll();
    }

    public void GiveSkill()
    {
        if (IsClientInitialized)
            ServerGiveSkill();
    }

    public void SetTestSkills()
    {
        if (IsClientInitialized)
            ServerSetTestSkills();
    }

    // Nobody owns this scene object, so it has to allow calls from anyone.
    // FishNet fills in caller with the connection that sent it.
    [ServerRpc(RequireOwnership = false)]
    private void ServerSpawnEnemy(int enemyIndex, int amountIndex, NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        // Only indexes travel: the host takes the prefab and the amount from its own copy of the settings,
        // so nobody can ask for a thousand at once.
        IReadOnlyList<Enemy> enemies = _settings.Enemies;
        IReadOnlyList<int> amounts = _settings.SpawnAmounts;
        if (enemyIndex < 0 || enemyIndex >= enemies.Count || enemies[enemyIndex] == null ||
            amountIndex < 0 || amountIndex >= amounts.Count)
        {
            Debug.LogWarning($"[DebugCheats] Client {caller.ClientId} asked for enemy {enemyIndex} and amount {amountIndex}, " +
                             "which aren't in the settings.", this);
            return;
        }

        Enemy prefab = enemies[enemyIndex];
        int amount = Mathf.Max(1, amounts[amountIndex]);

        for (int i = 0; i < amount; i++)
        {
            Enemy enemy = Instantiate(prefab, PickSpawnPosition(caller), Quaternion.identity);

            // Into the arena's scene, so it goes away with it, like the waves' enemies.
            Spawn(enemy.gameObject, null, gameObject.scene);
        }

        LogCheat(caller, $"spawned {amount} {prefab.name}");

        string enemyId = prefab.Definition != null && !string.IsNullOrEmpty(prefab.Definition.Id) ? prefab.Definition.Id : "unknown";
        ReportCheat(caller, amount > 1 ? $"spawn_{enemyId}_{amount}" : $"spawn_{enemyId}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerKillAllEnemies(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        LogCheat(caller, $"killed every enemy ({KillEveryEnemy()})");
        ReportCheat(caller, "kill_all");
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerSkipToNextWave(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        if (_waveSpawner == null)
        {
            TargetReport(caller, "No hay oleadas en esta escena", ToastType.Warning);
            return;
        }

        if (_waveSpawner.TrySkipToNextWave(out float startTime))
        {
            TargetReport(caller, $"Saltas a la oleada de {FormatTime(startTime)}", ToastType.Info);
            LogCheat(caller, $"skipped to the wave at {FormatTime(startTime)}");
            ReportCheat(caller, "next_wave");
        }
        else
        {
            TargetReport(caller, "No quedan más oleadas", ToastType.Warning);
        }
    }

    // Every enemy dies first, so the waves' count of living enemies is back to zero when they start again.
    [ServerRpc(RequireOwnership = false)]
    private void ServerRestartWaves(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        if (_waveSpawner == null)
        {
            TargetReport(caller, "No hay oleadas en esta escena", ToastType.Warning);
            return;
        }

        int killed = KillEveryEnemy();
        _waveSpawner.RestartWaves();

        TargetReport(caller, "Oleadas reiniciadas", ToastType.Info);
        LogCheat(caller, $"restarted the waves ({killed} enemies killed)");
        ReportCheat(caller, "reset_waves");
    }

    // Only the player who asks: each one decides about their own.
    [ServerRpc(RequireOwnership = false)]
    private void ServerToggleInvulnerable(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        NetworkObject player = caller.FirstObject;
        if (player == null || !player.TryGetComponent(out Health health))
            return;

        health.SetInvulnerable(!health.IsInvulnerable);
        LogCheat(caller, health.IsInvulnerable ? "invulnerable on" : "invulnerable off");
        ReportCheat(caller, "invulnerable");
    }

    // Players, enemies and dummies alike. The invulnerable ones ignore it.
    [ServerRpc(RequireOwnership = false)]
    private void ServerDamageAll(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        foreach (Health health in FindObjectsByType<Health>(FindObjectsSortMode.None))
        {
            if (health.IsSpawned)
                health.TryTakeDamage(_settings.DamageAllAmount, null);
        }

        LogCheat(caller, $"damaged everything by {_settings.DamageAllAmount}");
        ReportCheat(caller, "damage_all");
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerHealAll(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        foreach (Health health in FindObjectsByType<Health>(FindObjectsSortMode.None))
        {
            if (health.IsSpawned)
                health.RestoreFull();
        }

        LogCheat(caller, "healed everything");
        ReportCheat(caller, "heal_all");
    }

    // To the whole group: the experience is shared (#76).
    [ServerRpc(RequireOwnership = false)]
    private void ServerGiveXp(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        RunExperience experience = RunExperience.Current;
        if (experience == null)
        {
            TargetReport(caller, "Esta escena no tiene experiencia", ToastType.Warning);
            return;
        }

        experience.AddExperience(_settings.GiveXpAmount);
        LogCheat(caller, $"gave the group {_settings.GiveXpAmount} experience");
        ReportCheat(caller, "give_xp");
    }

    // To every player, within the limit of 2 per match: it tests rerolls without waiting for minute 4 (#78).
    [ServerRpc(RequireOwnership = false)]
    private void ServerGiveReroll(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        UpgradeOffers offers = UpgradeOffers.Current;
        if (offers == null)
        {
            TargetReport(caller, "Esta escena no tiene mejoras", ToastType.Warning);
            return;
        }

        offers.GiveRerollToAll();
        LogCheat(caller, "gave every player a reroll");
        ReportCheat(caller, "give_reroll");
    }

    // Only to whoever asked (#162): the next skill of the library they don't have, so pressing it again goes
    // through every skill. Into a free slot, or in place of the last one when the three are taken.
    [ServerRpc(RequireOwnership = false)]
    private void ServerGiveSkill(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller) || !TryGetSkills(caller, out PlayerSkills skills))
            return;

        IReadOnlyList<SkillDefinition> all = skills.Library.Skills;
        SkillInstance last = skills.Count > 0 ? skills.GetSkill((AttackSlot)((int)AttackSlot.Skill1 + skills.Count - 1)) : null;
        int start = last != null ? IndexOf(all, last.Definition) + 1 : 0;

        SkillDefinition next = null;
        for (int i = 0; i < all.Count && next == null; i++)
        {
            SkillDefinition candidate = all[(start + i) % all.Count];
            if (candidate != null && skills.GetLevel(candidate.Id) == 0)
                next = candidate;
        }

        if (next == null)
        {
            TargetReport(caller, "No quedan habilidades que darte", ToastType.Warning);
            return;
        }

        if (!skills.TryAddSkill(next))
        {
            // The three slots are taken: the new one replaces the last, and the others keep their place.
            List<SkillDefinition> kept = new();
            for (int slot = 0; slot < skills.Count - 1; slot++)
                kept.Add(skills.GetSkill((AttackSlot)((int)AttackSlot.Skill1 + slot)).Definition);

            kept.Add(next);
            skills.SetSkills(kept);
        }

        TargetReport(caller, $"Habilidad: {next.DisplayName}", ToastType.Info);
        LogCheat(caller, $"gave themselves {next.Id}");
        ReportCheat(caller, "give_skill");
    }

    // Estallido and Marca for the reaction tests, which were written for them (#162).
    [ServerRpc(RequireOwnership = false)]
    private void ServerSetTestSkills(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller) || !TryGetSkills(caller, out PlayerSkills skills))
            return;

        skills.SetSkills(_settings.TestSkills);
        TargetReport(caller, "Habilidades de prueba", ToastType.Info);
        LogCheat(caller, "set the test skills");
        ReportCheat(caller, "test_skills");
    }

    private bool TryGetSkills(NetworkConnection caller, out PlayerSkills skills)
    {
        skills = null;
        NetworkObject player = caller.FirstObject;
        if (player != null && player.TryGetComponent(out skills) && skills.Library != null)
            return true;

        TargetReport(caller, "No tienes habilidades", ToastType.Warning);
        return false;
    }

    private static int IndexOf(IReadOnlyList<SkillDefinition> skills, SkillDefinition skill)
    {
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] == skill)
                return i;
        }

        return -1;
    }

    // Only to whoever asked: the others don't need to know.
    [TargetRpc]
    private void TargetReport(NetworkConnection target, string message, ToastType type)
    {
        GameServices.Instance.ToastService.Show(message, type);
    }

    // For the launcher: every client writes cheat_used, so each one knows the match had a cheat in it.
    // The id is one per button of the panel (docs/launcher-events.md).
    private void ReportCheat(NetworkConnection caller, string cheatId)
    {
        ObserversCheatUsed(cheatId, caller.ClientId);
    }

    [ObserversRpc]
    private void ObserversCheatUsed(string cheatId, int callerClientId)
    {
        LauncherEvents.CheatUsed(cheatId, callerClientId == LocalConnection.ClientId);
    }

    // Through their health, so whatever listens for deaths hears them, like the waves' count.
    private static int KillEveryEnemy()
    {
        int killed = 0;

        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy.IsSpawned && enemy.TryGetComponent(out Health health) && !health.IsDead)
            {
                health.Kill();
                killed++;
            }
        }

        return killed;
    }

    // One line each, without a call stack, so a test's log shows when someone used one.
    private void LogCheat(NetworkConnection caller, string what)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, this, "[DebugCheats] Client {0}: {1}.", caller.ClientId, what);
    }

    // A release build ignores cheats, even from a modified client.
    private bool IsAllowed(NetworkConnection caller)
    {
        if (!Debug.isDebugBuild)
        {
            Debug.LogWarning($"[DebugCheats] Ignored a cheat from client {caller.ClientId}: this isn't a development build.", this);
            return false;
        }

        return _settings != null;
    }

    private Vector2 PickSpawnPosition(NetworkConnection caller)
    {
        // Each connection's first object is its player (see PlayerSpawnService).
        NetworkObject player = caller.FirstObject;
        if (player == null)
            return transform.position;

        float angle = Random.Range(0f, 2f * Mathf.PI);
        Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
        return (Vector2)player.transform.position + direction * _settings.SpawnDistance;
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60}:{total % 60:00}";
    }
}
