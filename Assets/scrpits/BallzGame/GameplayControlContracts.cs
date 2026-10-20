using BallzGame.Bricks;
using UnityEngine;

namespace BallzGame.Control
{
    /// <summary>
    /// Narrow board-control surface for scripted encounters, previews and tests.
    /// Normal gameplay does not use this interface.
    /// </summary>
    public interface IScenarioBoardControl
    {
        Brick GetBrickAt(int x, int y);
        bool TrySpawnScenarioBrick(Brick prefab, Vector2Int gridPosition, int hp, out Brick brick);
        bool TryMoveScenarioBricksDown();
        void ClearScenarioBricks();
    }

    /// <summary>
    /// Allows an external sequence to constrain the next player-confirmed launch.
    /// Passing null restores normal free aiming.
    /// </summary>
    public interface ILaunchDirectionControl
    {
        Vector2? ForcedLaunchTarget { get; }
        int ActiveBallCount { get; }
        bool IsListeningForInput { get; }
        void SetForcedLaunchTarget(Vector2? worldTarget);
    }

    /// <summary>
    /// Separates an effect's gameplay operation from its normal automatic scheduler.
    /// </summary>
    public interface IBrickEffectControl
    {
        bool AutomaticSchedulingEnabled { get; }
        bool IsEffectActive { get; }
        void SetAutomaticScheduling(bool enabled);
        void ActivateEffect();
        void DeactivateEffect();
    }

    /// <summary>
    /// An instantaneous brick ability which can also be invoked by a scripted sequence.
    /// </summary>
    public interface ITriggerableBrickEffect
    {
        void TriggerEffect();
    }
}
