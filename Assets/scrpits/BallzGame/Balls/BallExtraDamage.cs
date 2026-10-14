using System.Collections.Generic;
using UnityEngine;

namespace BallzGame.Balls
{

	public class BallExtraDamage : MonoBehaviour
	{
		[SerializeField]
		private List<int> extraDamage = new List<int>();
		private List<int> extraBottomBounceTime = new List<int>();
		[ContextMenu("Init list")]
		private void Awake()
		{
			int count = System.Enum.GetValues(typeof(BallType)).Length;
			if (extraDamage.Count != count)
			{
				extraDamage = new List<int>(new int[count]);
			}
			if (extraBottomBounceTime.Count != count)
			{
				extraBottomBounceTime = new List<int>(new int[count]);
			}
			count = System.Enum.GetValues(typeof(EffectType)).Length;
			if (effectValues.Count != count)
			{
				effectValues = new List<int>(new int[count]);
			}
		}
		public void Reset()
		{
			for (int i = 0; i < extraBottomBounceTime.Count; i++)
			{
				extraBottomBounceTime[i] = 0;
			}
			for (int i = 0; i < extraDamage.Count; i++)
			{
				extraDamage[i] = 0;
			}
			for (int i = 0; i < effectValues.Count; i++)
			{
				effectValues[i] = 0;
			}
		}
		public int GetExtraDamage(BallType type)
		{
			int index = (int)type;

			if (index >= extraDamage.Count)
				return 0;

			return extraDamage[index];
		}
		public int GetExtraBottomBounceTime(BallType type)
		{
			int index = (int)type;

			if (index >= extraBottomBounceTime.Count)
				return 0;

			return extraBottomBounceTime[index];
		}
		public void SetExtraBottomBounceTime(BallType type, int damage)
		{
			int index = (int)type;

			while (extraBottomBounceTime.Count <= index)
			{
				extraBottomBounceTime.Add(0);
			}

			extraBottomBounceTime[index] = damage;
		}
		public void AddExtraBottomBounceTime(BallType type, int damage)
		{
			int index = (int)type;

			while (extraBottomBounceTime.Count <= index)
			{
				extraBottomBounceTime.Add(0);
			}

			extraBottomBounceTime[index] += damage;
		}
		public void SetExtraDamage(BallType type, int damage)
		{
			int index = (int)type;

			while (extraDamage.Count <= index)
			{
				extraDamage.Add(0);
			}

			extraDamage[index] = damage;
		}
		public void AddExtraDamage(BallType type, int damage)
		{
			int index = (int)type;

			while (extraDamage.Count <= index)
			{
				extraDamage.Add(0);
			}

			extraDamage[index] += damage;
		}
		[SerializeField]
		private List<int> effectValues = new List<int>();




		public int GetEffectValue(EffectType type)
		{
			int index = (int)type;
			if (index >= effectValues.Count)
				return 0;
			return effectValues[index];
		}

		public void SetEffectValue(EffectType type, int value)
		{
			int index = (int)type;
			while (effectValues.Count <= index)
			{
				effectValues.Add(0);
			}
			effectValues[index] = value;
		}

		public void AddEffectValue(EffectType type, int value)
		{
			int index = (int)type;
			while (effectValues.Count <= index)
			{
				effectValues.Add(0);
			}
			effectValues[index] += value;
		}

		public enum BallType
		{
			None,
			NormalBall,
			ExplosionBall,
			AirSlashSpawned,
			ExplosionSpawned,
			HorizontalLazerSpawned,
			VerticalLazerSpawned,
			HorizontalLazerBall,
			VerticalLazerBall,
			AirSlashBall,
			BigBall
		}
		public enum EffectType
		{
			None,
			ExplosionPossibility,
			LazerCollideExplosion,
			BigBallCritPossibility,
			BigBallCritDamage,

		}
	}
}
