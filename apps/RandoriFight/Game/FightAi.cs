namespace RandoriFight.Game;

internal sealed class FightAi
{
    private float _thinkCooldown;
    private float _aggression = 0.58f;

    public void Reset() => _thinkCooldown = 0.25f;

    public void Update(Fighter self, Fighter opponent, float deltaSeconds)
    {
        if (!self.IsAlive || !opponent.IsAlive)
            return;

        self.FaceToward(opponent.PositionX);
        _thinkCooldown -= deltaSeconds;

        if (self.IsInHitStun || self.IsAttacking)
        {
            self.TryParry(false);
            return;
        }

        var dx = opponent.PositionX - self.PositionX;
        var dist = MathF.Abs(dx);
        var toward = Math.Sign(dx);

        if (opponent.IsAttacking && dist < 2.4f && _thinkCooldown <= 0f)
        {
            self.TryParry(true);
            _thinkCooldown = 0.18f;
            return;
        }

        self.TryParry(false);

        if (_thinkCooldown > 0f)
        {
            self.SetWalkInput(0f);
            return;
        }

        if (dist > 1.85f)
        {
            self.SetWalkInput(toward);
            _thinkCooldown = 0.1f;
            return;
        }

        if (dist < 1.05f)
        {
            self.SetWalkInput(-toward);
            _thinkCooldown = 0.22f;
            return;
        }

        if (dist <= 1.75f && Random.Shared.NextDouble() < _aggression)
        {
            var roll = Random.Shared.NextDouble();
            if (roll < 0.4)
                self.TryMen();
            else if (roll < 0.75)
                self.TryKesa();
            else
                self.TryThrust();

            _thinkCooldown = 0.35f + (float)Random.Shared.NextDouble() * 0.25f;
            return;
        }

        self.SetWalkInput(toward * 0.6f);
        _thinkCooldown = 0.12f;
    }
}
