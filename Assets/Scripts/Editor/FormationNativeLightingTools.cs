using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace YesterdayConvoy.Editor
{
    // Rotates the saved source probe only. Does not bake, save scenes, or alter project lighting settings.
    public static class FormationNativeLightingTools
    {
        const string SourcePath = FormationNativeAssetTools.ArtPath + "/ls5-scene-settings.json";
        const int Coefficients = 9;
        const double Tolerance = .00002;
        public static ProbeReport LastReport { get; private set; }

        [Serializable] public sealed class ProbeReport
        {
            public string source = SourcePath;
            public string conversion = "source (x,y,z) to project (x,-z,y); inverse project direction (x,z,-y)";
            public string ambientMode = "Custom";
            public float sourceAmbientIntensity;
            public float appliedCoefficientMultiplier = 1;
            public float[] sourceCoefficients, rotatedCoefficients;
            public float maxBasisFitError, maxIndependentEvaluationError, maxBandEnergyError, maxAppliedCoefficientError;
            public Color originalProjectUp, rotatedProjectUp, originalProjectDown, rotatedProjectDown;
        }

        [MenuItem("Yesterday Convoy/Formation Native/Apply Original Ambient Probe")]
        public static void ApplySourceAmbientProbe()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play before changing the saved ambient probe.");
            var rotated = RotateSourceProbe(out var report);
            // Unity 6000.6.5f1 exposes Custom. AmbientIntensity scales generated skybox lighting,
            // not a manually supplied custom probe. The source has no skybox material: do not multiply by 2.
            // https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/rendersettings/ambientintensity
            RenderSettings.ambientMode = AmbientMode.Custom;
            RenderSettings.ambientIntensity = 1;
            RenderSettings.ambientProbe = rotated;
            var applied = RenderSettings.ambientProbe;
            for (int rgb = 0; rgb < 3; rgb++) for (int coefficient = 0; coefficient < Coefficients; coefficient++)
                report.maxAppliedCoefficientError = Mathf.Max(report.maxAppliedCoefficientError,
                    Mathf.Abs(applied[rgb, coefficient] - rotated[rgb, coefficient]));
            if (report.maxAppliedCoefficientError > Tolerance)
                throw new InvalidOperationException("Unity changed the supplied source ambient probe coefficients.");
            LastReport = report;
            Debug.Log("FORMATION_NATIVE_AMBIENT_APPLIED: " + JsonUtility.ToJson(report));
        }

        [MenuItem("Yesterday Convoy/Formation Native/Validate Ambient Probe Rotation")]
        public static void Validate()
        {
            RotateSourceProbe(out var report);
            LastReport = report;
            Debug.Log("FORMATION_NATIVE_AMBIENT_ROTATION_PASS: " + JsonUtility.ToJson(report));
        }

        public static SphericalHarmonicsL2 RotateSourceProbe(out ProbeReport report)
        {
            var source = ReadSourceProbe(out var sourceIntensity);
            var directions = Sphere(256, .5);
            var inverse = directions.Select(InverseYUp).ToArray();
            var basis = UnityBasis(directions);
            var transformedBasis = UnityBasis(inverse);
            var normal = new double[Coefficients, Coefficients];
            for (int a = 0; a < Coefficients; a++) for (int b = 0; b < Coefficients; b++)
                for (int i = 0; i < directions.Length; i++) normal[a, b] += basis[i, a] * basis[i, b];
            var rotation = new double[Coefficients, Coefficients];
            double maxBasisError = 0;
            for (int sourceCoefficient = 0; sourceCoefficient < Coefficients; sourceCoefficient++)
            {
                var rhs = new double[Coefficients];
                for (int a = 0; a < Coefficients; a++) for (int i = 0; i < directions.Length; i++)
                    rhs[a] += basis[i, a] * transformedBasis[i, sourceCoefficient];
                var fitted = Solve(normal, rhs);
                for (int a = 0; a < Coefficients; a++) rotation[a, sourceCoefficient] = fitted[a];
                for (int i = 0; i < directions.Length; i++)
                {
                    double actual = 0;
                    for (int a = 0; a < Coefficients; a++) actual += basis[i, a] * fitted[a];
                    maxBasisError = Math.Max(maxBasisError, Math.Abs(actual - transformedBasis[i, sourceCoefficient]));
                }
            }
            if (maxBasisError > Tolerance)
                throw new InvalidOperationException("Unity SH evaluation did not yield an exact L2 rotation: " + maxBasisError);
            var result = new SphericalHarmonicsL2();
            for (int rgb = 0; rgb < 3; rgb++) for (int a = 0; a < Coefficients; a++)
            {
                double coefficient = 0;
                for (int b = 0; b < Coefficients; b++) coefficient += rotation[a, b] * source[rgb, b];
                result[rgb, a] = (float)coefficient;
            }
            report = new ProbeReport
            {
                sourceAmbientIntensity = sourceIntensity,
                sourceCoefficients = Flatten(source), rotatedCoefficients = Flatten(result),
                maxBasisFitError = (float)maxBasisError
            };
            // Independent directions, including the 6 axes, catch axis/sign errors and fit overfitting.
            var holdout = new[] { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back }
                .Concat(Sphere(613, .173)).ToArray();
            var expected = new Color[holdout.Length]; var actualColors = new Color[holdout.Length];
            source.Evaluate(holdout.Select(InverseYUp).ToArray(), expected);
            result.Evaluate(holdout, actualColors);
            for (int i = 0; i < holdout.Length; i++) for (int rgb = 0; rgb < 3; rgb++)
                report.maxIndependentEvaluationError = Mathf.Max(report.maxIndependentEvaluationError,
                    Mathf.Abs(expected[i][rgb] - actualColors[i][rgb]));
            report.originalProjectUp = expected[0]; report.rotatedProjectUp = actualColors[0];
            report.originalProjectDown = expected[1]; report.rotatedProjectDown = actualColors[1];
            // Exact spherical moments through degree 4: six axes have weight 1/15 each,
            // eight normalized cube corners have weight 3/40 each (weights sum to 1).
            // This integrates each squared L2 band exactly without assuming Unity's coefficients
            // are uniformly normalized. Derive signed values from UnityBasis, not clamped colors.
            var energyDirections = new[] { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back }
                .Concat(from x in new[] { -1, 1 } from y in new[] { -1, 1 } from z in new[] { -1, 1 }
                    select new Vector3(x, y, z).normalized).ToArray();
            var energyBasis = UnityBasis(energyDirections);
            for (int rgb = 0; rgb < 3; rgb++)
            {
                // The integral of the evaluated band's squared signal is rotation invariant.
                foreach (var range in new[] { new[] { 0, 1 }, new[] { 1, 4 }, new[] { 4, 9 } })
                {
                    double before = 0, after = 0;
                    for (int sample = 0; sample < energyDirections.Length; sample++)
                    {
                        double originalBand = 0, rotatedBand = 0;
                        for (int i = range[0]; i < range[1]; i++)
                        {
                            originalBand += energyBasis[sample, i] * source[rgb, i];
                            rotatedBand += energyBasis[sample, i] * result[rgb, i];
                        }
                        double weight = sample < 6 ? 1.0 / 15 : 3.0 / 40;
                        before += weight * originalBand * originalBand;
                        after += weight * rotatedBand * rotatedBand;
                    }
                    report.maxBandEnergyError = Mathf.Max(report.maxBandEnergyError, (float)Math.Abs(before - after));
                }
            }
            if (report.maxIndependentEvaluationError > Tolerance || report.maxBandEnergyError > Tolerance)
                throw new InvalidOperationException("Source ambient probe rotation verification failed: " + JsonUtility.ToJson(report));
            return result;
        }

        static SphericalHarmonicsL2 ReadSourceProbe(out float intensity)
        {
            string json = File.ReadAllText(SourcePath);
            var block = Regex.Match(json, "\"m_AmbientProbe\"\\s*:\\s*\\{(?<values>[^}]+)\\}");
            if (!block.Success) throw new InvalidDataException("Source LS-5 ambient probe is missing.");
            var matches = Regex.Matches(block.Groups["values"].Value,
                "\"sh\\[\\s*(?<index>\\d+)\\]\"\\s*:\\s*(?<value>[-+0-9.eE]+)");
            if (matches.Count != 27) throw new InvalidDataException("Expected exactly 27 original ambient coefficients.");
            var probe = new SphericalHarmonicsL2(); var seen = new bool[27];
            foreach (Match match in matches)
            {
                int index = int.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture);
                float value = float.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
                if (index < 0 || index >= 27 || seen[index] || float.IsNaN(value) || float.IsInfinity(value))
                    throw new InvalidDataException("Invalid or repeated original ambient coefficient.");
                seen[index] = true; probe[index / 9, index % 9] = value;
            }
            var sourceIntensity = Regex.Match(json, "\"m_AmbientIntensity\"\\s*:\\s*(?<value>[-+0-9.eE]+)");
            if (!sourceIntensity.Success) throw new InvalidDataException("Original ambient intensity is missing.");
            intensity = float.Parse(sourceIntensity.Groups["value"].Value, CultureInfo.InvariantCulture);
            return probe;
        }

        static Vector3 InverseYUp(Vector3 direction) => new Vector3(direction.x, direction.z, -direction.y);
        static Vector3[] Sphere(int count, double offset)
        {
            var result = new Vector3[count];
            double goldenAngle = Math.PI * (3 - Math.Sqrt(5));
            for (int i = 0; i < count; i++)
            {
                double y = 1 - 2 * (i + offset) / count, radius = Math.Sqrt(Math.Max(0, 1 - y * y));
                result[i] = new Vector3((float)(radius * Math.Cos(i * goldenAngle)), (float)y,
                    (float)(radius * Math.Sin(i * goldenAngle))).normalized;
            }
            return result;
        }

        static double[,] UnityBasis(Vector3[] directions)
        {
            // Derive Unity's real SH basis rather than assuming coefficient ordering or normalization.
            // A positive constant offset prevents a possible Evaluate negative clamp from losing signs.
            var constant = new SphericalHarmonicsL2(); constant[0, 0] = 4;
            var baseline = new Color[directions.Length]; constant.Evaluate(directions, baseline);
            var result = new double[directions.Length, Coefficients];
            for (int coefficient = 0; coefficient < Coefficients; coefficient++)
            {
                var unit = constant; unit[0, coefficient] += 1;
                var colors = new Color[directions.Length]; unit.Evaluate(directions, colors);
                for (int i = 0; i < directions.Length; i++) result[i, coefficient] = colors[i].r - baseline[i].r;
            }
            return result;
        }

        static double[] Solve(double[,] matrix, double[] rhs)
        {
            int n = rhs.Length; var a = (double[,])matrix.Clone(); var b = (double[])rhs.Clone();
            for (int col = 0; col < n; col++)
            {
                int pivot = col;
                for (int row = col + 1; row < n; row++) if (Math.Abs(a[row, col]) > Math.Abs(a[pivot, col])) pivot = row;
                if (Math.Abs(a[pivot, col]) < 1e-10) throw new InvalidOperationException("Unity SH basis is singular.");
                if (pivot != col)
                {
                    for (int j = 0; j < n; j++) { double tmp = a[col, j]; a[col, j] = a[pivot, j]; a[pivot, j] = tmp; }
                    double tmpB = b[col]; b[col] = b[pivot]; b[pivot] = tmpB;
                }
                double divisor = a[col, col];
                for (int j = col; j < n; j++) a[col, j] /= divisor;
                b[col] /= divisor;
                for (int row = 0; row < n; row++) if (row != col)
                {
                    double factor = a[row, col];
                    for (int j = col; j < n; j++) a[row, j] -= factor * a[col, j];
                    b[row] -= factor * b[col];
                }
            }
            return b;
        }
        static float[] Flatten(SphericalHarmonicsL2 probe) => Enumerable.Range(0, 27).Select(i => probe[i / 9, i % 9]).ToArray();
    }
}
