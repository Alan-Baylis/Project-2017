using UnityEngine;

namespace GameScripts
{
	public class FPSController : MonoBehaviour
	{

		public float mouseSensitivityX = 3.5f;
		public float mouseSensitivityY = 3.5f;
		public float walkSpeed = 8f;
		public float jumpForce = 220;
		public LayerMask groundedMask;
		private bool lookAllowed = true;

		private Rigidbody _rigidbody;
		private bool grounded;
		private Transform cameraT;
		private float verticalLookRot;
		private Vector3 moveAmount;
		private Vector3 smoothVelocity;

		public void setLookAlloowed(bool value)
		{
			lookAllowed = value;
		}
		
		// Use this for initialization
		void Awake ()
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
			cameraT = Camera.main.transform;
			_rigidbody = GetComponent<Rigidbody>();
		}
	
		// Update is called once per frame
		void Update () {

			//Camera look
			if (lookAllowed)
			{
				transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * mouseSensitivityX);
				verticalLookRot += Input.GetAxis("Mouse Y") * mouseSensitivityY;
				verticalLookRot = Mathf.Clamp(verticalLookRot, -60, 60);
				cameraT.localEulerAngles = Vector3.left * verticalLookRot;
			}

			//Movement calculation
			Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0 , Input.GetAxisRaw("Vertical")).normalized;
			Vector3 targetMoveAmount = moveDir * walkSpeed;
			moveAmount = Vector3.SmoothDamp(moveAmount, targetMoveAmount, ref smoothVelocity, .15f);

			//Jump
			if (Input.GetButtonDown("Jump"))
			{
				if(grounded) 
					_rigidbody.AddForce(transform.up * jumpForce);
			}
	   
			//Check if hit the ground
			Ray ray = new Ray(transform.position, -transform.up);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, 1 + .1f, groundedMask))
			{
				grounded = true;
			}
			else
			{
				grounded = false;
			}
		}

		void FixedUpdate()
		{
			//Apply the movement change in a fixed update
			Vector3 localMove = transform.TransformDirection(moveAmount) * Time.fixedDeltaTime;
			_rigidbody.MovePosition(_rigidbody.position + localMove);
		}
	}
}
