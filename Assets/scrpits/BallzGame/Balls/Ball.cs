using BallzGame.Bricks;
using BallzGame.Managers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace BallzGame.Balls
{

	public class Ball : MonoBehaviour
	{
		[SerializeField]private int BaseDamage = 1;

		public UnityEvent<Brick> OnBallHit;
		public BallExtraDamage.BallType Type;
		public BallData Data;
		[SerializeField]
		private Rigidbody2D rb;
		BallSystemConfig config;
		private BallLauncher launcher;
		public int HitButtomMaxTime;
		private int HitButtomCurrentTime;
		private void Awake()
		{

			config = GameManager.Instance.BallConfig;
		}
		public void Init(BallLauncher l,Vector2 direction,BallSystemConfig config)
		{
			launcher = l;
			this.config = config;
			HitButtomCurrentTime = 0;
			HitButtomMaxTime=GameManager.Instance.BallExtraDamageController.GetExtraBottomBounceTime(Type);
			rb.linearVelocity = direction * GameManager.Instance.BallConfig.BallSpeed;
		}

		private void OnCollisionEnter2D(Collision2D collision)
		{
			if (collision.gameObject.CompareTag("Bottom"))
			{
				if (HitButtomCurrentTime < HitButtomMaxTime)
				{
					HitButtomCurrentTime += 1;
				}
				else
				{
					launcher.OnBallReturned(this);
					Destroy(gameObject);
				}
			}
			Brick brick = collision.gameObject.GetComponent<Brick>();
			if (brick != null)
			{
				var damage = BaseDamage + GameManager.Instance.BallExtraDamageController.GetExtraDamage(Type);
				var direction=brick.transform.position-transform.position;
				brick.TakeDamage(damage, this,rb.linearVelocity.magnitude*direction);
				OnBallHit.Invoke(brick);
			}

			var extraTime = launcher.ElapsedTimeSinceLaunch - config.BounceAccelerationThreshold;
			if (extraTime >= 0)
			{
				var speed = config.BallSpeed + extraTime * config.SpeedIncrementAfterBounce;
				rb.AddForce(Random.onUnitCircle * config.RandomForceScale * extraTime);
				rb.linearVelocity =
					rb.linearVelocity.normalized * speed;
			}
		}



	}

}

[System.Serializable]
public class BallSystemConfig
{
	public float BounceAccelerationThreshold=2;
	public float SpeedIncrementAfterBounce=3;
	public float BallSpeed=13;
	public float RandomForceScale=0.1f;
	public float ShootInterval = 0.1f;
}