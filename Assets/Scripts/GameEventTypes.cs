/// <summary>
/// 统一事件类型枚举定义（集中管理，便于解耦与维护）
/// </summary>
public static class GameEventTypes
{
    /// <summary>
    /// 统一事件参数约定（跨模板统一）：
    /// 1) 对象级事件：parameters[0] 固定为 int instanceID（来自 GameObject.GetInstanceID()）。
    /// 2) 系统级事件：parameters[0] 固定为 string systemID（如 "UI"、"TimeOfDay"）。
    /// 3) 关键状态切换事件建议使用 SendEventImmediate（如 JumpscareTriggered、DayPhaseChanged）。
    /// 4) 高频事件建议节流发送（如 TimeChanged、HUD 更新类事件）。
    /// </summary>

    /// <summary>
    /// 玩家事件类型
    /// </summary>
    public enum PlayerEventType
    {
        /// <summary>玩家开始移动时发送。参数：(playerID, moveDirection, speed)。监听方可用于移动音效、足迹等。</summary>
        MovementStarted,
        /// <summary>玩家停止移动时发送。参数：(playerID, position)。</summary>
        MovementStopped,
        /// <summary>玩家跳跃时发送。参数：(playerID, position, jumpForce)。</summary>
        Jump,
        /// <summary>玩家落地时发送。参数：(playerID, position, landingVelocity)。</summary>
        Landed,
        /// <summary>玩家开始下蹲时发送。参数：(playerID, crouchSpeed)。</summary>
        CrouchStarted,
        /// <summary>玩家结束下蹲时发送。参数：(playerID)。</summary>
        CrouchEnded,
        /// <summary>玩家开始冲刺时发送。参数：(playerID, runSpeed)。</summary>
        SprintStarted,
        /// <summary>玩家结束冲刺时发送。参数：(playerID)。</summary>
        SprintEnded,
        /// <summary>玩家执行近战攻击时发送。参数：(playerID, damage, range)。</summary>
        MeleeAttack,
        /// <summary>玩家近战攻击命中目标时发送。参数：(playerID, damage, targetName, targetPosition)。</summary>
        MeleeAttackHit,
        /// <summary>玩家发射投掷物时发送。参数：(playerID, projectileName, damage, spawnPosition, direction, speed)。监听方可用于投掷音效、轨迹等。</summary>
        ProjectileFired,
        /// <summary>玩家血量变化时发送。参数：(playerID, currentHealth, maxHealth, percentage)。</summary>
        HealthChanged,
        /// <summary>玩家受到伤害时发送。参数：(playerID, damage, oldHealth, newHealth)。</summary>
        DamageTaken,
        /// <summary>玩家恢复生命时发送。参数：(playerID, healAmount, oldHealth, newHealth)。</summary>
        Healed,
        /// <summary>玩家死亡时发送。应使用 SendEventImmediate 立即广播。参数：(playerID, position, finalHealth)。监听方可用于 UI、音效、游戏结束等。</summary>
        Death,
        /// <summary>玩家进入无敌状态时发送。参数：(playerID, duration)。</summary>
        InvincibilityStarted,
        /// <summary>玩家结束无敌状态时发送。参数：(playerID)。</summary>
        InvincibilityEnded,
        /// <summary>玩家动画状态改变时发送。参数：(playerID, oldState, newState)。</summary>
        AnimationStateChanged,
        /// <summary>玩家初始化完成时发送。参数：(playerID, position, currentHealth, maxHealth)。</summary>
        Initialized
    }

    /// <summary>
    /// NPC事件类型
    /// </summary>
    public enum NPCEventType
    {
        /// <summary>NPC 状态切换时发送。参数：(instanceID, oldState, newState)。</summary>
        StateChanged,
        /// <summary>NPC 进入待机状态时发送。参数：(instanceID, position)。</summary>
        StateEnterIdle,
        /// <summary>NPC 进入巡逻状态时发送。参数：(instanceID, patrolTarget, patrolRadius)。</summary>
        StateEnterPatrol,
        /// <summary>NPC 进入追逐状态时发送。参数：(instanceID, targetPosition, distance)。</summary>
        StateEnterChase,
        /// <summary>NPC 进入攻击状态时发送。参数：(instanceID, attackType, attackRange)。</summary>
        StateEnterAttack,
        /// <summary>NPC 退出某状态时发送。参数：(instanceID, exitedState)。</summary>
        StateExit,
        /// <summary>NPC 检测到玩家时发送。参数：(instanceID, playerPosition, distance)。</summary>
        PlayerDetected,
        /// <summary>NPC 失去玩家时发送。参数：(instanceID, npcPosition)。</summary>
        PlayerLost,
        /// <summary>NPC 执行攻击时发送。参数：(instanceID, attackType, distance)。</summary>
        AttackPerformed,
        /// <summary>NPC 执行近战攻击时发送。参数：(instanceID, damage, targetName)。</summary>
        MeleeAttack,
        /// <summary>NPC 执行投掷物攻击时发送。参数：(instanceID, projectileName, damage)。</summary>
        ProjectileAttack,
        /// <summary>NPC 发射投掷物时发送。参数：(instanceID, projectileName, spawnPosition, direction, speed)。</summary>
        ProjectileFired,
        /// <summary>NPC 血量变化时发送。参数：(instanceID, currentHealth, maxHealth, percentage)。</summary>
        HealthChanged,
        /// <summary>NPC 受到伤害时发送。参数：(instanceID, damage, oldHealth, newHealth)。</summary>
        DamageTaken,
        /// <summary>NPC 死亡时发送。应使用 SendEventImmediate 立即广播。参数：(instanceID, deathPosition, finalHealth)。</summary>
        Death,
        /// <summary>NPC 设置巡逻目标时发送。参数：(instanceID, targetPosition, distance)。</summary>
        PatrolTargetSet,
        /// <summary>NPC 到达巡逻点时发送。参数：(instanceID, reachedPosition)。</summary>
        PatrolTargetReached,
        /// <summary>NPC 到达追逐目标时发送。参数：(instanceID, targetPosition, distance)。</summary>
        ChaseTargetReached,
        /// <summary>NPC 初始化完成时发送。参数：(instanceID, position, currentHealth, maxHealth)。</summary>
        Initialized,
        /// <summary>NPC 销毁时发送。参数：(instanceID, lastPosition)。</summary>
        Destroyed
    }

    /// <summary>
    /// 攻击输入请求结果（可被监听方修改，用于解耦攻击输入与处理方）
    /// </summary>
    public class AttackInputResult
    {
        public bool handled;
    }

    /// <summary>
    /// 游戏系统级事件类型（与具体角色解耦，如伤害请求）
    /// </summary>
    public enum GameSystemEventType
    {
        /// <summary>
        /// 伤害请求。由攻击方发送，监听方判断 targetInstanceID 是否为自己后执行扣血。
        /// 负载约定：parameters[0]=int targetInstanceID, parameters[1]=float amount, parameters[2]=GameObject source（造成伤害的来源，可为 null）。
        /// 必须使用 SendEventImmediate 立即广播。targetInstanceID 需用目标 GameObject 的 GetInstanceID() 获取。
        /// </summary>
        DamageRequest,

        /// <summary>
        /// 攻击输入请求。由 Player/NPC 发送，WeaponController 等可监听并处理；若 result.handled 仍为 false 则发送方执行内置攻击。
        /// 负载约定：parameters[0]=int instanceID（请求方 GameObject 的 GetInstanceID()），parameters[1]=AttackInputResult result（监听方处理成功后设置 result.handled=true）。
        /// 必须使用 SendEventImmediate 立即广播。
        /// </summary>
        AttackInputRequest
    }

    /// <summary>
    /// 相机/镜头效果事件类型（供 CameraEffectController 等组件监听）
    /// </summary>
    public enum CameraEventType
    {
        /// <summary>
        /// 通用镜头效果请求。
        /// 负载约定：parameters[0]=string effectType（如 "shake"/"screenshake"/"vignette"），parameters[1]=float intensity（强度 0-1），parameters[2]=float duration（秒）。
        /// 发送示例：EventDispatcher.SendEvent(CameraEventType.EffectRequest, "shake", 0.5f, 0.3f);
        /// </summary>
        EffectRequest,
        /// <summary>
        /// 恐怖惊吓请求（Jumpscare、Glitch 等）。
        /// 负载约定：parameters[0]=float intensity（强度 0-1），parameters[1]=float duration（秒）。
        /// 发送示例：EventDispatcher.SendEvent(CameraEventType.ScareRequest, 1f, 0.5f);
        /// </summary>
        ScareRequest,
        /// <summary>
        /// 速度反馈请求（赛车运动模糊、FOV 拉伸等）。
        /// 负载约定：parameters[0]=float speed（当前速度），parameters[1]=float maxSpeed（参考最大速度，用于计算比例）。
        /// 发送示例：EventDispatcher.SendEvent(CameraEventType.SpeedRequest, 80f, 100f);
        /// </summary>
        SpeedRequest,
        /// <summary>
        /// 升级反馈请求（肉鸽升级时的慢动作/静止）。
        /// 负载约定：parameters[0]=float duration（冻结/慢动作持续时间，秒）。
        /// 发送示例：EventDispatcher.SendEvent(CameraEventType.LevelUpRequest, 1.5f);
        /// </summary>
        LevelUpRequest,
        /// <summary>
        /// 相机旋转请求（由 PlayerInputController 等发送，CameraFollowController 监听）。
        /// 负载约定：parameters[0]=float yaw（水平旋转角度，度），parameters[1]=float pitch（垂直旋转角度，度）。
        /// 发送示例：EventDispatcher.SendEvent(CameraEventType.CameraRotationRequest, 45f, -10f);
        /// </summary>
        CameraRotationRequest,
        /// <summary>
        /// 相机视角预设切换请求。
        /// 负载约定：parameters[0]=string presetName（"FirstPerson"/"ThirdPerson"/"GlobalView"/"TopDown"/"Arena"/"Racing"/"None"）。
        /// 发送示例：EventDispatcher.SendEvent(CameraEventType.CameraModeChangeRequest, "ThirdPerson");
        /// </summary>
        CameraModeChangeRequest
    }

    /// <summary>
    /// Boss 事件类型（供 BossBehaviorController 发送，UI 血条、镜头效果、音效等监听）
    /// </summary>
    public enum BossEventType
    {
        /// <summary>Boss 阶段切换时发送。参数：(instanceID, oldPhaseIndex, newPhaseIndex, hpPercentage)。</summary>
        PhaseChanged,
        /// <summary>Boss 技能预警开始时发送。参数：(instanceID, attackPatternName, warningDuration, damageTime)。</summary>
        AttackWarning,
        /// <summary>Boss 技能执行/命中时发送。参数：(instanceID, attackPatternName, damage, targetPosition)。</summary>
        AttackExecuted,
        /// <summary>Boss 受到伤害时发送。参数：(instanceID, damage, oldHealth, newHealth)。</summary>
        DamageTaken,
        /// <summary>Boss 死亡时发送。应使用 SendEventImmediate 立即广播。参数：(instanceID, deathPosition, finalHealth)。</summary>
        Death,
        /// <summary>Boss 初始化完成时发送。参数：(instanceID, position, currentHealth, maxHealth)。</summary>
        Initialized
    }

    /// <summary>
    /// 武器事件类型（供 WeaponController 发送，UI、音效等监听）
    /// </summary>
    public enum WeaponEventType
    {
        /// <summary>开火/挥击时发送。参数：(ownerInstanceID, weaponSlotIndex, damage)。</summary>
        Fired,
        /// <summary>开始换弹时发送。参数：(ownerInstanceID, weaponSlotIndex, reloadDuration)。</summary>
        ReloadStarted,
        /// <summary>换弹完成时发送。参数：(ownerInstanceID, weaponSlotIndex, currentAmmo, maxAmmo)。</summary>
        ReloadEnded,
        /// <summary>命中目标时发送。参数：(ownerInstanceID, targetInstanceID, damage, hitPosition)。</summary>
        Hit,
        /// <summary>弹药变化时发送。参数：(ownerInstanceID, weaponSlotIndex, currentAmmo, maxAmmo)。</summary>
        AmmoChanged,
        /// <summary>切换武器时发送。参数：(ownerInstanceID, fromSlotIndex, toSlotIndex)。</summary>
        WeaponSwitched
    }

    /// <summary>
    /// 生成事件类型（供 SpawnController 发送，UI、任务、难度系统等监听）
    /// </summary>
    public enum SpawnEventType
    {
        /// <summary>生成单位时发送。参数：(spawnerInstanceID, spawnedInstanceID, prefabName, spawnPosition)。</summary>
        Spawned,
        /// <summary>波次开始时发送。参数：(spawnerInstanceID, waveIndex, totalWaves)。</summary>
        WaveStarted,
        /// <summary>波次结束时发送。参数：(spawnerInstanceID, waveIndex, totalWaves)。</summary>
        WaveEnded,
        /// <summary>生成点清场完成时发送（如本波全部死亡可下一波）。参数：(spawnerInstanceID, waveIndex)。</summary>
        SpawnPointCleared,
        /// <summary>达到生成数量上限时发送。参数：(spawnerInstanceID, currentCount, maxCount)。</summary>
        SpawnLimitReached
    }

    /// <summary>
    /// 载具事件类型（供 VehicleController 发送，UI 仪表盘、镜头效果等监听）
    /// </summary>
    public enum VehicleEventType
    {
        /// <summary>加速时发送。参数：(instanceID, currentSpeed, maxSpeed)。</summary>
        Accelerating,
        /// <summary>刹车时发送。参数：(instanceID, currentSpeed, brakeForce)。</summary>
        Braking,
        /// <summary>发生碰撞时发送。参数：(instanceID, collisionForce, contactPoint)。</summary>
        Collision,
        /// <summary>使用氮气时发送。参数：(instanceID, remainingNitro, nitroCapacity)。</summary>
        NitroUsed,
        /// <summary>速度变化时发送。参数：(instanceID, currentSpeed, maxSpeed)。</summary>
        SpeedChanged
    }

    /// <summary>
    /// UI 事件类型（供 UI 绑定器、主题系统、HUD 模块监听）
    /// </summary>
    public enum UIEventType
    {
        /// <summary>HUD 初始化完成。参数：(systemID, gameType, themeName, bindTargetCount)。</summary>
        HUDInitialized,
        /// <summary>准星状态变化。参数：(systemID, stateName, spreadOrScale)。</summary>
        CrosshairStateChanged,
        /// <summary>玩家 HUD 更新。参数：(instanceID, currentHealth, maxHealth, extraValue)。</summary>
        PlayerHUDUpdated,
        /// <summary>Boss HUD 更新。参数：(instanceID, currentHealth, maxHealth, phaseIndex)。</summary>
        BossHUDUpdated,
        /// <summary>武器 HUD 更新。参数：(instanceID, weaponName, currentAmmo, reserveAmmo, isReloading)。</summary>
        WeaponHUDUpdated,
        /// <summary>载具 HUD 更新。参数：(instanceID, currentSpeed, maxSpeed, nitroValue, nitroCapacity)。</summary>
        VehicleHUDUpdated,
        /// <summary>赛车 HUD 更新。参数：(systemID, lapTime, rank, progress01)。</summary>
        RaceHUDUpdated,
        /// <summary>UI 主题切换。参数：(systemID, oldTheme, newTheme)。</summary>
        UIThemeChanged,
        /// <summary>UI 提示请求。参数：(systemID, message, duration)。</summary>
        UIToastRequest
    }

    /// <summary>
    /// JumpScare 事件类型（供恐怖玩法流程、镜头与音效联动）
    /// </summary>
    public enum JumpScareEventType
    {
        /// <summary>惊吓序列开始。建议 SendEventImmediate。参数：(instanceID, triggerType, sequenceName)。</summary>
        JumpscareTriggered,
        /// <summary>惊吓步骤开始。参数：(instanceID, stepIndex, stepType, stepTime)。</summary>
        JumpscareStepStarted,
        /// <summary>惊吓步骤完成。参数：(instanceID, stepIndex, stepType)。</summary>
        JumpscareStepCompleted,
        /// <summary>惊吓被中断。参数：(instanceID, reason)。</summary>
        JumpscareInterrupted,
        /// <summary>惊吓序列结束。建议 SendEventImmediate。参数：(instanceID, finalState)。</summary>
        JumpscareEnded
    }

    /// <summary>
    /// 物理预设事件类型（供物理治理与调试系统监听）
    /// </summary>
    public enum PhysicsPresetEventType
    {
        /// <summary>物理预设应用成功。参数：(systemID, targetInstanceID, presetName, applyMode)。</summary>
        PhysicsPresetApplied,
        /// <summary>物理预设应用失败。参数：(systemID, targetInstanceID, presetName, reason)。</summary>
        PhysicsPresetApplyFailed,
        /// <summary>运行时纠偏触发。参数：(systemID, targetInstanceID, correctionType, valueBefore, valueAfter)。</summary>
        PhysicsRuntimeCorrected,
        /// <summary>碰撞层规则变更。参数：(systemID, profileName, changeSummary)。</summary>
        CollisionLayerRuleChanged
    }

    /// <summary>
    /// 昼夜系统事件类型（供刷怪、任务、UI 等系统监听）
    /// </summary>
    public enum TimeOfDayEventType
    {
        /// <summary>时间变化（建议节流发送）。参数：(systemID, dayIndex, time24h, normalized01)。</summary>
        TimeChanged,
        /// <summary>昼夜阶段变化。建议 SendEventImmediate。参数：(systemID, oldPhase, newPhase, time24h)。</summary>
        DayPhaseChanged,
        /// <summary>时间倍率变化。参数：(systemID, oldScale, newScale, isPaused)。</summary>
        TimeScaleChanged,
        /// <summary>光照参数更新。参数：(systemID, mainLightIntensity, ambientIntensity, phaseName)。</summary>
        SunLightingUpdated,
        /// <summary>完成一轮昼夜循环。参数：(systemID, dayIndex)。</summary>
        TimeLoopCompleted
    }
}
