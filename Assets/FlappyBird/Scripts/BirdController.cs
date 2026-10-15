using UnityEngine;

namespace FlappyBird
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class BirdController : MonoBehaviour
    {
        private Rigidbody2D body;
        private FlappyBirdGame game;
        private float flapSpeed;
        private CircleCollider2D hitbox;
        private float recoveryUntil;
        public bool Recovering { get; private set; }
        public float VerticalSpeed => body.linearVelocity.y;

        public void Initialize(FlappyBirdGame owner, float gravity, float impulse)
        {
            game = owner;
            flapSpeed = impulse;
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = gravity;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            hitbox = GetComponent<CircleCollider2D>();
            hitbox.radius = 0.25f;
            ResetBird();
        }

        public void ResetBird()
        {
            Recovering = false;
            hitbox.enabled = true;
            body.simulated = false;
            body.linearVelocity = Vector2.zero;
            transform.SetPositionAndRotation(new Vector3(-1.5f, 0, 0), Quaternion.identity);
        }

        // strength below 1 makes a weaker flap (used when the bird is out of hope).
        public void Flap(float strength = 1)
        {
            body.simulated = true;
            body.linearVelocity = new Vector2(0, flapSpeed * strength);
        }

        // Sets the vertical speed outright, such as bouncing off the floor in the boss fight.
        public void Bounce(float speed)
        {
            body.simulated = true;
            body.linearVelocity = new Vector2(0, speed);
        }

        // A sudden shove, such as a gust of wind.
        public void Push(Vector2 change)
        {
            if (body.simulated) body.linearVelocity += change;
        }

        public void Stop() { body.simulated = false; }

        public void BeginRecovery()
        {
            // Let the bird escape the obstacle it just hit without another immediate death.
            Recovering = true;
            hitbox.enabled = false;
            recoveryUntil = Time.time + 0.5f;
        }

        private void FixedUpdate()
        {
            if (!Recovering || Time.time < recoveryUntil) return;
            foreach (Collider2D obstacle in Physics2D.OverlapCircleAll(body.position, hitbox.radius + 0.05f))
                if (obstacle != hitbox && !obstacle.isTrigger) return;
            Recovering = false;
            hitbox.enabled = true;
        }

        private void Update()
        {
            bool flying = game != null && (game.State == FlappyBirdGame.PlayState.Playing
                || game.State == FlappyBirdGame.PlayState.Boss && body.simulated);
            if (flying) transform.rotation = Quaternion.Euler(0, 0, Mathf.Clamp(body.linearVelocity.y * 5, -65, 25));
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!Recovering) game.EndRun();
        }
    }
}
