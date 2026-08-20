using System.Collections;
using BallzGame.Bricks;
using BallzGame.Managers;
using UnityEngine;
using Utils;


namespace BallzGame.Balls.SpecialBalls
{
	public class BrickDamageArea : MonoBehaviour
	{
		public int Damage;
		[SerializeField] private Collider2D collider;
		[SerializeField] private Vector2 ForceDirection;
		[SerializeField] private float ForceFromCenterMagnitude;
		public float DestroyAfter=-1;
		[SerializeField] private BallExtraDamage.BallType Type;
		private void Start()
		{
			StartCoroutine(WaitAndDisableCollider());
			if (DestroyAfter > 0)
			{
				StartCoroutine(DestroyGameObject());
			}
		}
		public IEnumerator DestroyGameObject()
		{
			yield return new WaitForSeconds(DestroyAfter);
			Destroy(gameObject);
		}
		public IEnumerator WaitAndDisableCollider()
		{
			yield return null;
			yield return new WaitForFixedUpdate();
			if (collider != null)
				collider.enabled = false;
		}
		private void Reset()
		{
			collider = GetComponent<Collider2D>();
		}

		private void OnTriggerEnter2D(Collider2D other)
		{

			Brick brick = other.gameObject.GetComponent<Brick>();
			if (brick != null)
			{
				if (brick.Status.Contains(Brick.BrickStatus.DisableEffect))
				{
					GameManager.DoBlockFloatText(brick.transform.position);
					return;
				}

				Vector2 force=Vector2.zero;
				var damage=Damage+GameManager.Instance.BallExtraDamageController.GetExtraDamage(Type);
				if (ForceFromCenterMagnitude > 0)
				{
					Vector2 forceDirection = other.transform.position - gameObject.transform.position;
					forceDirection.Normalize();
					force= forceDirection * ForceFromCenterMagnitude;
					brick.TakeDamage(damage,this,force);
				}
				else if(ForceDirection != Vector2.zero)
				{
					force = ForceDirection;
					brick.TakeDamage(damage,this,force);
				}
				else
				{
					brick.TakeDamage(damage,this);
				}
			}

		}
	}
}