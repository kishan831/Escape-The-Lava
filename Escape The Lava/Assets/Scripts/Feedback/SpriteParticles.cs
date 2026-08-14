using UnityEngine;

namespace EscapeTheLava
{
    /// <summary>
    /// A tiny pooled sprite particle system.
    ///
    /// Unity's built-in <c>ParticleSystem</c> would work, but it stores its tuning in serialized
    /// modules that a code-driven setup cannot express cleanly. This pool is a few hundred lines
    /// smaller in the scene file, allocates once at startup, and every effect in the game is one
    /// readable <see cref="Burst"/> struct.
    /// </summary>
    public class SpriteParticles : MonoBehaviour
    {
        /// <summary>Description of one emission. Every field has a sane default so callers only set what matters.</summary>
        public struct Burst
        {
            public Sprite sprite;
            public Material material;
            public int count;

            /// <summary>Emission direction cone, in degrees. 0..360 for a full circle.</summary>
            public float angleMin, angleMax;

            public float speedMin, speedMax;
            public float lifeMin, lifeMax;
            public float sizeMin, sizeMax;

            /// <summary>Size multiplier reached at the end of life. 0 shrinks away, >1 expands.</summary>
            public float endSizeScale;

            public Color colorA, colorB;

            /// <summary>Colour reached at the end of life. Alpha 0 fades the particle out.</summary>
            public Color endColor;

            /// <summary>World units/second^2 pulling the particle down (negative floats it up).</summary>
            public float gravity;

            /// <summary>Velocity damping per second. 0 = no drag, 6 = stops quickly.</summary>
            public float drag;

            /// <summary>Random spin range in degrees/second.</summary>
            public float spin;

            /// <summary>Random spawn offset around the emission point.</summary>
            public float spawnRadius;

            public int sortingOrder;

            public static Burst Default(Sprite sprite, Material material) => new Burst
            {
                sprite = sprite,
                material = material,
                count = 8,
                angleMin = 0f,
                angleMax = 360f,
                speedMin = 1.5f,
                speedMax = 3.5f,
                lifeMin = 0.35f,
                lifeMax = 0.7f,
                sizeMin = 0.12f,
                sizeMax = 0.24f,
                endSizeScale = 0.1f,
                colorA = Color.white,
                colorB = Color.white,
                endColor = new Color(1f, 1f, 1f, 0f),
                gravity = 0f,
                drag = 2.2f,
                spin = 90f,
                spawnRadius = 0.1f,
                sortingOrder = 40
            };
        }

        [Tooltip("Maximum simultaneous particles. Older particles are recycled once the pool is full.")]
        public int poolSize = 320;

        Item[] _items;
        int _cursor;

        class Item
        {
            public Transform tr;
            public SpriteRenderer sr;
            public bool active;
            public float life, maxLife;
            public Vector2 velocity;
            public float gravity, drag, spin, rotation;
            public float startSize, endSize;
            public Color startColor, endColor;
        }

        void Awake()
        {
            _items = new Item[Mathf.Max(16, poolSize)];
            for (int i = 0; i < _items.Length; i++)
            {
                var go = new GameObject("particle");
                go.transform.SetParent(transform, false);
                go.SetActive(false);
                _items[i] = new Item
                {
                    tr = go.transform,
                    sr = go.AddComponent<SpriteRenderer>()
                };
            }
        }

        /// <summary>Spawns <c>burst.count</c> particles at <paramref name="position"/>.</summary>
        public void Emit(Vector3 position, in Burst burst)
        {
            for (int i = 0; i < burst.count; i++) Spawn(position, burst);
        }

        /// <summary>Spawns a single particle. Used for slow ambient effects such as lava bubbles.</summary>
        public void EmitSingle(Vector3 position, in Burst burst) => Spawn(position, burst);

        void Spawn(Vector3 position, in Burst burst)
        {
            Item item = Take();
            if (item == null) return;

            float angle = Random.Range(burst.angleMin, burst.angleMax) * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            item.active = true;
            item.maxLife = Mathf.Max(0.01f, Random.Range(burst.lifeMin, burst.lifeMax));
            item.life = item.maxLife;
            item.velocity = direction * Random.Range(burst.speedMin, burst.speedMax);
            item.gravity = burst.gravity;
            item.drag = burst.drag;
            item.spin = Random.Range(-burst.spin, burst.spin);
            item.rotation = Random.Range(0f, 360f);
            item.startSize = Random.Range(burst.sizeMin, burst.sizeMax);
            item.endSize = item.startSize * burst.endSizeScale;
            item.startColor = Color.Lerp(burst.colorA, burst.colorB, Random.value);
            item.endColor = burst.endColor;

            Vector2 offset = Random.insideUnitCircle * burst.spawnRadius;
            item.tr.position = position + (Vector3)offset;
            item.tr.localScale = Vector3.one * item.startSize;
            item.tr.localRotation = Quaternion.Euler(0f, 0f, item.rotation);

            item.sr.sprite = burst.sprite;
            if (burst.material) item.sr.sharedMaterial = burst.material;
            item.sr.color = item.startColor;
            item.sr.sortingOrder = burst.sortingOrder;
            item.tr.gameObject.SetActive(true);
        }

        /// <summary>Round-robin allocation: the oldest particle is stolen when the pool is saturated.</summary>
        Item Take()
        {
            if (_items == null) return null;

            for (int i = 0; i < _items.Length; i++)
            {
                _cursor = (_cursor + 1) % _items.Length;
                if (!_items[_cursor].active) return _items[_cursor];
            }

            _cursor = (_cursor + 1) % _items.Length;
            return _items[_cursor];
        }

        void Update()
        {
            if (_items == null) return;
            float dt = Time.deltaTime;

            for (int i = 0; i < _items.Length; i++)
            {
                Item item = _items[i];
                if (!item.active) continue;

                item.life -= dt;
                if (item.life <= 0f)
                {
                    item.active = false;
                    item.tr.gameObject.SetActive(false);
                    continue;
                }

                float t = 1f - item.life / item.maxLife;

                item.velocity.y -= item.gravity * dt;
                item.velocity *= 1f / (1f + item.drag * dt);

                item.tr.position += (Vector3)(item.velocity * dt);
                item.rotation += item.spin * dt;
                item.tr.localRotation = Quaternion.Euler(0f, 0f, item.rotation);
                item.tr.localScale = Vector3.one * Mathf.LerpUnclamped(item.startSize, item.endSize, t);
                item.sr.color = Color.LerpUnclamped(item.startColor, item.endColor, t);
            }
        }

        /// <summary>Hides every live particle. Called when a round restarts.</summary>
        public void Clear()
        {
            if (_items == null) return;
            foreach (Item item in _items)
            {
                item.active = false;
                if (item.tr) item.tr.gameObject.SetActive(false);
            }
        }
    }
}
