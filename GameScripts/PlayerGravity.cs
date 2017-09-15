using UnityEngine;

namespace GameScripts
{
	public class PlayerGravity : MonoBehaviour
	{
		private PlanetGravity planetGravity;   
		private Rigidbody _rigidbody;

		// Use this for initialization
		void Awake ()
		{
			planetGravity = GameObject.FindGameObjectWithTag("Planet").GetComponent<PlanetGravity>();
			_rigidbody = GetComponent<Rigidbody>();
			//Turn off player's gravity and rotation since it is simulated by the planet
			_rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
			_rigidbody.useGravity = false;             
		}
	
		// Update is called once per frame
		void FixedUpdate ()
		{
			planetGravity.Attract(_rigidbody);
		}
	}
}
