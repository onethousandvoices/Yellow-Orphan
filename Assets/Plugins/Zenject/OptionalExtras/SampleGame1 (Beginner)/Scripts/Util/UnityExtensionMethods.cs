using UnityEngine;

namespace Zenject.Asteroids
{
    public static class UnityExtensionMethods
    {
        // Since transforms return their position as a property,
        // you can't set the x/y/z values directly, so you have to
        // store a temporary Vector3
        // Or you can use these methods instead
        public static void SetX(this Transform transform, float x)
        {
            Vector3 pos = transform.position;
            pos.x = x;
            transform.position = pos;
        }

        public static void SetY(this Transform transform, float y)
        {
            Vector3 pos = transform.position;
            pos.y = y;
            transform.position = pos;
        }

        public static void SetZ(this Transform transform, float z)
        {
            Vector3 pos = transform.position;
            pos.z = z;
            transform.position = pos;
        }
    }
}
