using BallzGame.Bricks;
using BallzGame.Managers;
using UnityEngine;

public class AllSpecialBrickRow : CrisisBehaviour
{
	[SerializeField] private Brick blackholeBrick;
	[SerializeField] private Brick angleBrick;
	[SerializeField] private Brick treeBrick;

	public override void DoCrisis()
	{
		Debug.Log("DoCrisis");
		var spawner = GameManager.Instance.spawner;

		int specialIndex = Random.Range(0, 3);
		Brick BrickType=null;
		switch (specialIndex)
		{
			case 0:
				BrickType = blackholeBrick;
				break;

			case 1:
				BrickType = angleBrick;
				break;

			case 2:
				BrickType = treeBrick;
				break;
		}

		int width = GameManager.Instance.width;

		spawner.NextBricks.Clear();
		spawner.NextRowIsCrisis = true;

		for (int i = 0; i < width; i++)
		{
			if (Random.value < spawner.SpawnPossibility)
			{
				spawner.NextBricks.Add(BrickType);
			}
			else
			{
				spawner.NextBricks.Add(null);
			}

		}
	}


}
