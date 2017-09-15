using UnityEngine;

namespace GameScripts
{
    public class DayNightCycle : MonoBehaviour
    {
        public float speed = 3f;

        void FixedUpdate () {
            transform.Rotate(Vector3.right * Time.deltaTime * 0.4f * speed); //Rotation around its axis by axis constant times speed
        }
    }
}
