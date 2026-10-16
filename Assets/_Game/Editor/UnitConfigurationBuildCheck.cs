using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace YesterdayConvoy.Editor
{
    public sealed class UnitConfigurationBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => UnitConfigurationTools.ValidateProject();
    }
}
