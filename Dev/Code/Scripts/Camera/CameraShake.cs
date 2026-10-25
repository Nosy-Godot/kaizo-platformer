using Godot;

public class CameraShake
{
    private readonly FastNoiseLite _noise = new() { NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex };
    private float _time;

    public float Trauma { get; private set; }

    public void Add(float amount) => Trauma = Mathf.Clamp(Trauma + amount, 0f, 1f);

    public Vector2 Tick(float delta, float decay, float maxOffset, float frequency)
    {
        Trauma = Mathf.MoveToward(Trauma, 0f, decay * delta);
        if (Trauma == 0f) return Vector2.Zero;

        _time += delta * frequency;
        var strength = Trauma * Trauma * maxOffset;
        return new Vector2(_noise.GetNoise2D(_time, 0f), _noise.GetNoise2D(0f, _time)) * strength;
    }
}
