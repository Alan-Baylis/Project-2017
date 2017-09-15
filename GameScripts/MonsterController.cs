using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
	private Animation anim;
	private int health = 100;
	private float damage;
	private GameObject player;
	private Mecanim_Control_melee playerTarget;
	private float speed = 7;
	private GameObject planet;
	private Vector3 gravityUp;
	private bool isDead = false;
	private bool isDamaged = false;
	
	private void Awake()
	{
		planet = GameObject.FindGameObjectWithTag("Planet");
		anim = GetComponent<Animation>();
		anim["attack1"].wrapMode = WrapMode.Once;
		anim["death"].wrapMode = WrapMode.Once;
		anim["block_hit"].wrapMode = WrapMode.Once;
		player = GameObject.Find("Player");
		playerTarget = player.GetComponent<Mecanim_Control_melee>();
		gravityUp = (transform.position - planet.transform.position).normalized;
	}

	private void Start()
	{
		StartCoroutine(ManageEnemy());
	}

	private IEnumerator ManageEnemy()
	{
		while(Application.isPlaying && !isDead)
		{
			Animate();
			yield return null;
		}
	} 

	private void Combat()
	{
		if (Aggro())
		{			
			if (playerTarget.GetTarget() != this)
			{
				playerTarget.SetTarget(this);
			}
			if (isClose())
			{
				Attack();
			}
			else
			{
				Run(player.transform);
			}
		} 		
		else
		{
			Idle();
		}
	}

	private bool Aggro()
	{
		return Vector3.Distance(transform.position, player.transform.position) <= 30;		
	}

	/*private bool isAwayFromPos()
	{
		return Vector3.Distance(transform.position, originalPos.position) >= 40;
	}*/

	private bool isClose()
	{
		return Vector3.Distance(transform.position, player.transform.position) < 5;
	}

	private void Attack()
	{
		if (!anim.IsPlaying("block_hit"))
		{
			anim.Play("attack1");
		}
	}

	public void Damage(int damage)
	{
		if (health > 0 && isClose())
		{
			isDamaged = true;	
			health -= damage;
		}
	}

	private void TakeDamage()
	{
		anim.Play("block_hit");
		isDamaged = false;
	}

	private void Run(Transform target)
	{
		if (!anim.IsPlaying("block_hit"))
		{
			float step = speed * Time.deltaTime;
			transform.position = Vector3.MoveTowards(transform.position, target.position, step);
			transform.LookAt(target, gravityUp);
			anim.Play("run");
		}
	}

	private void Idle()
	{
		anim.Play("idle");
	}

	private void Die()
	{
		anim.Play("death");
		isDead = true;
		Invoke("DestroyObject", 10);
	}

	private void DestroyObject()
	{
		StopCoroutine(ManageEnemy());
		Destroy(gameObject);		
	}

	private void Animate()
	{
		if (health > 0)
		{
			if (!isDamaged)
			{
				Combat();
			}
			else
			{
				TakeDamage();
			}
		}
		else
		{
			Die();
		}
	}
}
