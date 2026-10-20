using System.Collections.Generic;
using BallzGame.Managers;
using UnityEngine;
using Object = System.Object;

namespace BallzGame.Bricks.SpecialBricks
{
    public class SpecialBrick : MonoBehaviour
    {
        protected IEnumerable<Brick> GetSurroundingBricks()
        {
            var manager = GameManager.Instance;
            var center = GetComponentInParent<Brick>();
            if (!manager || !center ||
                manager.GetBrickAt(center.GridPosition.x, center.GridPosition.y) != center)
                yield break;

            Vector2Int position = center.GridPosition;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    Brick brick = manager.GetBrickAt(position.x + dx, position.y + dy);
                    if (brick)
                        yield return brick;
                }
            }
        }

        public virtual void OnRowMoved()
        {

        }



        public virtual void OnHit(bool alive, object sender)
        {

        }
    }
}