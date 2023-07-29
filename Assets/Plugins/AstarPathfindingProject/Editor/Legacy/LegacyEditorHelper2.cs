using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;

namespace Pathfinding.Legacy {
	public static class LegacyEditorHelper {
		public static void UpgradeDialog (Object[] targets, System.Type upgradeType) {
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			GUIContent gui = EditorGUIUtility.IconContent("console.warnicon");
			gui.text = "You are using the compatibility version of this component. It is recommended that you upgrade to the newer version. This may change the component's behavior.";
			EditorGUILayout.LabelField(GUIContent.none, gui, EditorStyles.wordWrappedMiniLabel);
			if (GUILayout.Button("Upgrade")) {
				Undo.RecordObjects(targets.Select(s => (s as Component).gameObject).ToArray(), "Upgrade from Legacy Component");
				foreach (Object tg in targets) {
					Component comp = tg as Component;
					Component[] components = comp.gameObject.GetComponents<Component>();
					int index = System.Array.IndexOf(components, comp);
					Component newRVO = Undo.AddComponent(comp.gameObject, upgradeType);
					foreach (FieldInfo field in newRVO.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)) {
						field.SetValue(newRVO, field.GetValue(comp));
					}
					Undo.DestroyObjectImmediate(comp);
					for (int i = components.Length - 1; i > index; i--) UnityEditorInternal.ComponentUtility.MoveComponentUp(newRVO);
				}
			}
			EditorGUILayout.EndVertical();
		}
	}
}
