using UnityEngine;
using UnityEngine.UI;

namespace GameScripts
{
	public class ProgressBar : MonoBehaviour {

		private Image foregroundImage;
	
		public float Value
		{
			get 
			{
				if(foregroundImage != null)
					return (foregroundImage.fillAmount);	
				else
					return 0;	
			}
			set 
			{
				if(foregroundImage != null)
					foregroundImage.fillAmount = value;	
			} 
		}

		void Awake () {
			foregroundImage = gameObject.GetComponent<Image>();		
			Value = 0;
		}	
	}
}
