using System.Numerics;

namespace RandoriFight.Game;

internal sealed class Fighter
{
    public float PositionX { get; private set; }
    public int Facing { get; private set; } = 1;
    public float Health { get; private set; } = 100f;
    public FighterState State { get; private set; } = FighterState.Idle;
    public float StateTime { get; private set; }
    public bool IsPlayer { get; init; }
    public bool AttackHitApplied { get; private set; }

    private CombatMove _activeMove;
    private float _walkSpeed = 4.2f;

    public Vector3 WorldPosition => new(PositionX, FightArena.FloorY, 0f);

    public bool IsAlive => Health > 0f && State != FighterState.Ko;
    public bool IsBlocking => State == FighterState.Block;
    public bool IsInHitStun => State == FighterState.HitStun;
    public bool IsAttacking => State is FighterState.Punch or FighterState.Kick;

    public void Reset(float x, int facing)
    {
        PositionX = x;
        Facing = facing;
        Health = 100f;
        State = FighterState.Idle;
        StateTime = 0f;
        AttackHitApplied = false;
    }

    public void FaceToward(float otherX)
    {
        if (State is FighterState.Punch or FighterState.Kick or FighterState.HitStun)
            return;

        Facing = otherX >= PositionX ? 1 : -1;
    }

    public void SetWalkInput(float direction)
    {
        if (!CanMove())
            return;

        if (MathF.Abs(direction) < 0.01f)
        {
            if (State == FighterState.Walk)
                Enter(FighterState.Idle);
            return;
        }

        Facing = direction > 0f ? 1 : -1;
        State = FighterState.Walk;
    }

    public void TryBlock(bool held)
    {
        if (!IsAlive || IsInHitStun || IsAttacking)
            return;

        if (held)
        {
            State = FighterState.Block;
            StateTime = 0f;
            return;
        }

        if (State == FighterState.Block)
            Enter(FighterState.Idle);
    }

    public bool TryPunch()
    {
        if (!CanStartAttack())
            return false;

        BeginAttack(FighterState.Punch, CombatMoves.Punch);
        return true;
    }

    public bool TryKick()
    {
        if (!CanStartAttack())
            return false;

        BeginAttack(FighterState.Kick, CombatMoves.Kick);
        return true;
    }

    public void Update(float deltaSeconds, float moveInput, bool blockHeld)
    {
        StateTime += deltaSeconds;

        if (State == FighterState.Walk)
        {
            PositionX = FightArena.ClampX(PositionX + moveInput * _walkSpeed * deltaSeconds);
            if (MathF.Abs(moveInput) < 0.01f)
                Enter(FighterState.Idle);
            return;
        }

        if (State == FighterState.Block)
        {
            if (!blockHeld)
                Enter(FighterState.Idle);
            return;
        }

        if (State == FighterState.HitStun && StateTime >= _activeMove.HitStun)
            Enter(FighterState.Idle);

        if (!IsAttacking)
            return;

        if (IsInActivePhase() && !AttackHitApplied)
            return;

        if (StateTime >= _activeMove.Total)
            Enter(FighterState.Idle);
    }

    public void ApplyDamage(float amount, float hitStun)
    {
        if (!IsAlive)
            return;

        if (IsBlocking)
        {
            Health -= amount * 0.15f;
            State = FighterState.Block;
            StateTime = 0f;
            return;
        }

        Health = MathF.Max(0f, Health - amount);
        _activeMove = new CombatMove(0f, 0f, 0f, 0f, 0f, 0f, hitStun);
        State = FighterState.HitStun;
        StateTime = 0f;
        AttackHitApplied = false;

        if (Health <= 0f)
        {
            Health = 0f;
            State = FighterState.Ko;
        }
    }

    public bool TryApplyAttackHit(Fighter defender)
    {
        if (!IsAttacking || AttackHitApplied || !IsInActivePhase())
            return false;

        var center = AttackCenter();
        var dist = Vector3.Distance(center, defender.WorldPosition + new Vector3(0f, 0.9f, 0f));
        if (dist > _activeMove.Radius)
            return false;

        AttackHitApplied = true;
        defender.ApplyDamage(_activeMove.Damage, _activeMove.HitStun);
        return true;
    }

    public Vector3 AttackCenter()
    {
        var reach = _activeMove.Range * AttackPhaseT();
        return WorldPosition + new Vector3(Facing * reach, 0.95f, 0f);
    }

    public float AttackPhaseT()
    {
        if (!IsAttacking)
            return 0f;

        if (StateTime < _activeMove.Startup)
            return 0f;

        var activeT = (StateTime - _activeMove.Startup) / MathF.Max(_activeMove.Active, 1e-4f);
        return Math.Clamp(activeT, 0f, 1f);
    }

    public float MoveAnimPhase => State == FighterState.Walk ? StateTime * 9f : 0f;

    private bool CanMove() =>
        IsAlive && State is FighterState.Idle or FighterState.Walk;

    private bool CanStartAttack() =>
        IsAlive && State is FighterState.Idle or FighterState.Walk;

    private bool IsInActivePhase() =>
        StateTime >= _activeMove.Startup && StateTime <= _activeMove.Startup + _activeMove.Active;

    private void BeginAttack(FighterState state, CombatMove move)
    {
        State = state;
        _activeMove = move;
        StateTime = 0f;
        AttackHitApplied = false;
    }

    private void Enter(FighterState state)
    {
        State = state;
        StateTime = 0f;
        AttackHitApplied = false;
    }
}
