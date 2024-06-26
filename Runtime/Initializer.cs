using System.Collections.Generic;
using UnityEngine;

public class Initializer : MonoBehaviour
{
	public List<MainRefs> systemRefs;
	private List<MonoBehaviour> sharedObjectsReadyList = new List<MonoBehaviour>();
	private bool isInitializeDone;

	private void Awake()
	{
#if UNITY_EDITOR
		var arr = FindObjectsOfType<Initializer>();
		if (arr.Length > 1) Debug.LogError("Singleton already exist!!!");
#endif
	}

	private void Start()
	{
		if (!isInitializeDone)
		{
			Debug.LogError("Not all shared objects initialized");
			print("Already initialized objects:");

			foreach (var item in sharedObjectsReadyList)
			{
				print(item.GetType().Name);
			}
		}
	}

	public void AddReadySharedObject(MainRefs obj)
	{
		if (isInitializeDone)
		{
			Debug.LogError($"Object arrived after initialization: {obj}");
			print("Already initialized objects:");

			foreach (var item in sharedObjectsReadyList)
			{
				print(item.GetType().Name);
			}
		}

		sharedObjectsReadyList.Add(obj);

		if (sharedObjectsReadyList.Count == systemRefs.Count)
		{
			isInitializeDone = true;
			var mainRefs = FindObjectsOfType<MainRefs>();

			foreach (var item in mainRefs)
			{
				item.OnInitializeFinished();
			}
		}
	}
}
