using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluidAutomata
{
	public class Rotator : MonoBehaviour
	{
		public float speed = 100;

		private void Update() {

			transform.Rotate(transform.up, speed * Time.deltaTime);
		}
	}
}
