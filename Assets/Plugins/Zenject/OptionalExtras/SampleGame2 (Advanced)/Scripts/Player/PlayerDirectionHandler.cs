using UnityEngine;

namespace Zenject.SpaceFighter
{
    public class PlayerDirectionHandler : ITickable
    {
        readonly Player _player;
        readonly Camera _mainCamera;

        public PlayerDirectionHandler(
            Camera mainCamera,
            Player player)
        {
            _player = player;
            _mainCamera = mainCamera;
        }

        public void Tick()
        {
            Ray mouseRay = _mainCamera.ScreenPointToRay(Input.mousePosition);

            Vector3 mousePos = mouseRay.origin;
            mousePos.z = 0;

            Vector3 goalDir = mousePos - _player.Position;
            goalDir.z = 0;
            goalDir.Normalize();

            _player.Rotation = Quaternion.LookRotation(goalDir) * Quaternion.AngleAxis(90, Vector3.up);
        }
    }
}
