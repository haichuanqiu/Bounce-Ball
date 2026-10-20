using BallzGame.Bricks;
using BallzGame.Managers;
using UnityEngine;
using UnityEngine.Serialization;

public class SpecialBrickRush : CrisisBehaviour
{
	[SerializeField] private Brick bigBrick;
	[SerializeField] private Brick normalBrick;
	[SerializeField] private Brick blackholeBrick;
	[SerializeField] private Brick angleBrick;
	[SerializeField] private Brick treeBrick;

	public override void DoCrisis()
	{
		Debug.Log("DoCrisis");

		if (Random.value < 0.5f)
		{
			GameManager.Instance.BeforeRowSpawn.AddListener(DoSpecialBrickRush);
		}
		else
		{
			GameManager.Instance.BeforeRowSpawn.AddListener(DoBigBrickRush);
		}
		currentWave = 0;
	}

	[SerializeField]private int maxWave;
	private int currentWave = 0;
	public override int RowCount => Mathf.Max(1, maxWave);
	public override void CancelCrisis()
	{
		var manager = GameManager.Instance;
		if (manager)
		{
			manager.BeforeRowSpawn.RemoveListener(DoSpecialBrickRush);
			manager.BeforeRowSpawn.RemoveListener(DoBigBrickRush);
		}
		currentWave = 0;
	}
	public void DoSpecialBrickRush()
	{
		currentWave++;
		Debug.Log("DoSpecialBrickRush");
		SpeciBrickRush();
		if (currentWave >= maxWave)
		{
			Debug.Log("Stop");
			currentWave = 0;
			GameManager.Instance.BeforeRowSpawn.RemoveListener(DoSpecialBrickRush);
		}
	}
	public void DoBigBrickRush()
	{
		currentWave++;
		Debug.Log("DoSpecialBrickRush");
		BigBrickRush();
		if (currentWave >= maxWave)
		{
			Debug.Log("Stop");
			currentWave = 0;
			GameManager.Instance.BeforeRowSpawn.RemoveListener(DoBigBrickRush);
		}
	}


	public void SpeciBrickRush()
	{
		var spawner = GameManager.Instance.spawner;

		if (spawner == null || normalBrick == null)
		{
			Debug.LogWarning("CrisisManager: Spawner 或 BlackholeBrick 没有设置！");
			return;
		}

		int width = GameManager.Instance.width;

		spawner.NextBricks.Clear();
		spawner.NextRowIsCrisis = true;

		for (int i = 0; i < width; i++)
		{
			if (Random.value < spawner.SpawnPossibility)
			{
				if (Random.value < 0.5f)
				{
					spawner.NextBricks.Add(normalBrick);
				}
				else
				{
					// 三种特殊砖等概率
					int specialIndex = Random.Range(0, 3);

					switch (specialIndex)
					{
						case 0:
							spawner.NextBricks.Add(blackholeBrick);
							break;

						case 1:
							spawner.NextBricks.Add(angleBrick);
							break;

						case 2:
							spawner.NextBricks.Add(treeBrick);
							break;
					}
				}
			}
			else
			{
				spawner.NextBricks.Add(null);
			}

		}
	}
	public void BigBrickRush()
	{
		var spawner = GameManager.Instance.spawner;

		if (spawner == null || normalBrick == null)
		{
			Debug.LogWarning("CrisisManager: Spawner 或 BlackholeBrick 没有设置！");
			return;
		}

		int width = GameManager.Instance.width;

		spawner.NextBricks.Clear();
		spawner.NextRowIsCrisis = true;

		for (int i = 0; i < width; i++)
		{
			spawner.NextBricks.Add(bigBrick);
			// Crisis bricks must not fall below the normal HP curve at later waves.
			spawner.NextBricksHP.Add(Mathf.Max(GameManager.Instance.level * 2,
				spawner.GetRandomHPByLevel(GameManager.Instance.level)));
		}
	}

}
