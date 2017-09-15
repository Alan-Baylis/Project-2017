using UnityEngine;

namespace GameScripts
{
    public class PlanetGravity : MonoBehaviour
    {

        public float Gravity = -9.81f;

        public void Attract(Rigidbody body)
        {
  
            var gravityUp = (body.position - transform.position).normalized;
            var bodyUp = body.transform.up;

            //Apply downwards gravity to body
            body.AddForce(gravityUp * Gravity);

            //Alling bodies up axis with the centre of the planet
            body.rotation = Quaternion.FromToRotation(bodyUp, gravityUp) * body.rotation;
        }
    }
}
