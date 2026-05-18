namespace RandoriFight.Game;

internal sealed class FightAi
{
    private float _thinkCooldown;
    private float _aggression = 0.55f;

    public void Reset() => _thinkCooldown = 0.2f;

    public void Update(Fighter self, Fighter opponent, float deltaSeconds)
    {
        if (!self.IsAlive || !opponent.IsAlive)
            return;

        self.FaceToward(opponent.PositionX);
        _thinkCooldown -= deltaSeconds;

        if (self.IsInHitStun || self.IsAttacking)
        {
            self.TryBlock(false);
            return;
        }

        var dx = opponent.PositionX - self.PositionX;
        var dist = MathF.Abs(dx);
        var toward = Math.Sign(dx);

        if (opponent.IsAttacking && dist < 2.2f && _thinkCooldown <= 0f)
        {
            self.TryBlock(true);
            _thinkCooldown = 0.15f;
            return;
        }

        self.TryBlock(false);

        if (_thinkCooldown > 0f)
        {
            self.SetWalkInput(0f);
            return;
        }

        if (dist > 1.65f)
        {
            self.SetWalkInput(toward);
            _thinkCooldown = 0.08f;
            return;
        }

        if (dist < 0.95f && Random.Shared.NextDouble() < 0.35)
        {
            self.SetWalkInput(-toward);
            _thinkCooldown = 0.2f;
            return;
        }

        if (dist <= 1.55f)
        {
            if (Random.Shared.NextDouble() < _aggression)
            {
                if (Random.Shared.NextDouble() < 0.55)
                    self.TryKick();
                else
                    self.TryPunch();
            }
            else
            {
                self.SetWalkInput(-toward * 0.5f);
            }

            _thinkCooldown = 0.28f + (float)Random.Shared.NextDouble() * 0.2f;
            return;
        }

        self.SetWalkInput(toward);
        _thinkCooldown = 0.1f;
    }
}
