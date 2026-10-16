using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy.Editor
{
    public sealed class MovementPrototypeBuildCheck : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (scene.path == BaselineTools.EntryScene)
                MovementPrototypeTools.ValidateScene(scene);
        }
    }
}
