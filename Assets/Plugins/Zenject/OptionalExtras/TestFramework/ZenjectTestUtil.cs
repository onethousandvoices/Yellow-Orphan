using ModestTree;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zenject.Internal
{
    public static class ZenjectTestUtil
    {
        public const string UnitTestRunnerGameObjectName = "Code-based tests runner";

        public static void DestroyEverythingExceptTestRunner(bool immediate)
        {
            GameObject testRunner = GameObject.Find(UnitTestRunnerGameObjectName);
            Assert.IsNotNull(testRunner);
            GameObject.DontDestroyOnLoad(testRunner);

            // We want to clear all objects across all scenes to ensure the next test is not affected
            // at all by previous tests
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                foreach (GameObject obj in SceneManager.GetSceneAt(i).GetRootGameObjects())
                {
                    GameObject.DestroyImmediate(obj);
                }
            }

            if (ProjectContext.HasInstance)
            {
                GameObject[] dontDestroyOnLoadRoots = ProjectContext.Instance.gameObject.scene
                                                                    .GetRootGameObjects();

                foreach (GameObject rootObj in dontDestroyOnLoadRoots)
                {
                    if (rootObj.name != UnitTestRunnerGameObjectName)
                    {
                        if (immediate)
                        {
                            GameObject.DestroyImmediate(rootObj);
                        }
                        else
                        {
                            GameObject.Destroy(rootObj);
                        }
                    }
                }
            }
        }
    }
}
