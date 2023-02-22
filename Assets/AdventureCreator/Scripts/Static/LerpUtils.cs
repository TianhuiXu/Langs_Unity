/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"LerpUtils.cs"
 * 
 *	A collection of sub-classes that allow for framerate-independent lerping
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * <summary>A collection of sub-classes that allow for framerate-independent lerping</summary>
	 */
	public static class LerpUtils
	{

		public class FloatLerp
		{

			private bool linearMotion;

			private float moveSpeed;
			private bool isRunning = false;
			private float percentageComplete;

			private float startValue;
			private float targetValue;


			public FloatLerp (bool _linearMotion = false)
			{
				linearMotion = _linearMotion;
			}


			public float Update (float currentValue, float newTargetValue, float newMoveSpeed)
			{
				if (Mathf.Approximately (newTargetValue, currentValue))
				{
					return End (currentValue, newTargetValue);
				}

				if (isRunning && (!Mathf.Approximately (targetValue, newTargetValue) || !Mathf.Approximately (moveSpeed, newMoveSpeed)))
				{
					// Values changed, so reset
					isRunning = false;
				}

				if (!isRunning)
				{
					moveSpeed = newMoveSpeed;
					targetValue = newTargetValue;
					isRunning = true;
					startValue = currentValue;
					percentageComplete = 0f;
				}

				if (percentageComplete <= 1f)
				{
					percentageComplete += (Time.deltaTime * (moveSpeed / 2f));

					if (percentageComplete > 1f)
					{
						percentageComplete = 1f;
					}
				}

				if (percentageComplete >= 1f)
				{
					return End (currentValue, targetValue);
				}
				else
				{
					float t = 0f;
					if (linearMotion)
					{
						t = percentageComplete;
					}
					else
					{
						t = 1f - ((percentageComplete - 1f) * (percentageComplete - 1f));
					}

					return Mathf.Lerp (startValue, targetValue, t);
				}
			}


			public void Reset ()
			{
				isRunning = false;
			}


			private float End (float currentValue, float _targetValue)
			{
				if (isRunning)
				{
					currentValue = _targetValue;
					isRunning = false;
				}

				return currentValue;
			}

		}


		public class Vector2Lerp
		{

			private bool linearMotion;

			private float moveSpeed;
			private bool isRunning = false;
			private float percentageComplete;

			private Vector2 startValue;
			private Vector2 targetValue;


			public Vector2Lerp (bool _linearMotion = false)
			{
				linearMotion = _linearMotion;
			}


			public Vector2 Update (Vector2 currentValue, Vector2 newTargetValue, float newMoveSpeed)
			{
				if (newTargetValue == currentValue)
				{
					return End (currentValue, newTargetValue);
				}

				if (targetValue != newTargetValue || !Mathf.Approximately (moveSpeed, newMoveSpeed))
				{
					isRunning = false;
				}

				if (!isRunning)
				{
					targetValue = newTargetValue;
					moveSpeed = newMoveSpeed;
					isRunning = true;
					startValue = currentValue;
					percentageComplete = 0f;
				}

				if (percentageComplete <= 1f)
				{
					percentageComplete += (Time.deltaTime * (moveSpeed / 2f));

					if (percentageComplete > 1f)
					{
						percentageComplete = 1f;
					}
				}

				if (percentageComplete >= 1f)
				{
					return End (currentValue, targetValue);
				}
				else
				{
					float t = 0f;
					if (linearMotion)
					{
						t = percentageComplete;
					}
					else
					{
						t = 1f - ((percentageComplete - 1f) * (percentageComplete - 1f));
					}

					return Vector2.Lerp (startValue, targetValue, t);
				}
			}


			private Vector2 End (Vector2 currentValue, Vector2 _targetValue)
			{
				if (isRunning)
				{
					currentValue = _targetValue;
					isRunning = false;
				}

				return currentValue;
			}

		}


		public class Vector3Lerp
		{

			private bool linearMotion;

			private float moveSpeed;
			private bool isRunning = false;
			private float percentageComplete;

			private Vector3 startValue;
			private Vector3 targetValue;


			public Vector3Lerp (bool _linearMotion = false)
			{
				linearMotion = _linearMotion;
			}


			public Vector3 Update (Vector3 currentValue, Vector3 newTargetValue, float newMoveSpeed)
			{
				if (newTargetValue == currentValue)
				{
					return End (currentValue, newTargetValue);
				}

				if (targetValue != newTargetValue || !Mathf.Approximately (moveSpeed, newMoveSpeed))
				{
					isRunning = false;
				}

				if (!isRunning)
				{
					targetValue = newTargetValue;
					moveSpeed = newMoveSpeed;
					isRunning = true;
					startValue = currentValue;
					percentageComplete = 0f;
				}

				if (percentageComplete <= 1f)
				{
					percentageComplete += (Time.deltaTime * (moveSpeed / 2f));

					if (percentageComplete > 1f)
					{
						percentageComplete = 1f;
					}
				}

				if (percentageComplete >= 1f)
				{
					return End (currentValue, targetValue);
				}
				else
				{
					float t = (linearMotion) ? 
						percentageComplete :
						1f - ((percentageComplete - 1f) * (percentageComplete - 1f));

					return Vector3.Lerp (startValue, targetValue, t);
				}
			}


			private Vector3 End (Vector3 currentValue, Vector3 _targetValue)
			{
				if (isRunning)
				{
					currentValue = _targetValue;
					isRunning = false;
				}

				return currentValue;
			}

		}


		public class QuaternionLerp
		{

			private bool linearMotion;
			private bool useSlerp;

			private float moveSpeed;
			private bool isRunning = false;
			private float percentageComplete;

			private Quaternion startValue;
			private Quaternion targetValue;


			public QuaternionLerp (bool _linearMotion = false, bool _useSlerp = false)
			{
				linearMotion = _linearMotion;
				useSlerp = _useSlerp;
			}


			public Quaternion Update (Quaternion currentValue, Quaternion newTargetValue, float newMoveSpeed)
			{
				if (newTargetValue == currentValue)
				{
					return End (currentValue, newTargetValue);
				}

				if (targetValue != newTargetValue || !Mathf.Approximately (moveSpeed, newMoveSpeed))
				{
					isRunning = false;
				}

				if (!isRunning)
				{
					targetValue = newTargetValue;
					moveSpeed = newMoveSpeed;
					isRunning = true;
					startValue = currentValue;
					percentageComplete = 0f;
				}

				if (percentageComplete <= 1f)
				{
					percentageComplete += (Time.deltaTime * (moveSpeed / 2f));

					if (percentageComplete > 1f)
					{
						percentageComplete = 1f;
					}
				}

				if (percentageComplete >= 1f)
				{
					return End (currentValue, targetValue);
				}
				else
				{
					float t = 0f;
					if (linearMotion)
					{
						t = percentageComplete;
					}
					else
					{
						t = 1f - ((percentageComplete - 1f) * (percentageComplete - 1f));
					}

					if (useSlerp)
					{
						return Quaternion.Slerp (startValue, targetValue, t);
					}
					return Quaternion.Lerp (startValue, targetValue, t);
				}
			}


			private Quaternion End (Quaternion currentValue, Quaternion _targetValue)
			{
				if (isRunning)
				{
					currentValue = _targetValue;
					isRunning = false;
				}

				return currentValue;
			}

		}

	}

}
