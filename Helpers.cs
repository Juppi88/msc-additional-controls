using HutongGames.PlayMaker;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

static class Helpers
{
	public static string GetActionTargetName(FsmOwnerDefault gameObject, GameObject owner)
	{
		if (gameObject.OwnerOption == OwnerDefaultOption.SpecifyGameObject)
		{
			if (gameObject.GameObject.Value != null)
			{
				return gameObject.GameObject.Value.name;
			}
			else
			{
				return gameObject.GameObject.Name;
			}
		}

		return owner.name;
	}

	public static string GetObjectHierarchy(GameObject obj)
	{
		var t = obj.transform;
		var hierarchy = new List<string>();

		while (t != null)
		{
			hierarchy.Add(t.name);
			t = t.parent;
		}

		hierarchy.Reverse();

		var sb = new StringBuilder();

		for (int i = 0; i < hierarchy.Count; i++)
		{
			sb.Append(hierarchy[i]);
			if (i < hierarchy.Count - 1)
			{
				sb.Append("/");
			}
		}

		return sb.ToString();
	}
}
