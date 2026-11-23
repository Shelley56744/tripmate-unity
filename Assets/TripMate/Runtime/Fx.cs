using UnityEngine;

namespace TripMate
{
    /// <summary>粒子特效：雪地揚塵、撿到東西的閃光、定案時的彩帶。</summary>
    public static class Fx
    {
        static ParticleSystem dust, sparkle, confetti;

        static ParticleSystem Make(string name)
        {
            var go = new GameObject("TM_Fx_" + name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 600;
            var em = ps.emission;
            em.enabled = false;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = TM.ParticleMat();
            ps.Play();
            return ps;
        }

        static void Shrink(ParticleSystem ps)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));
        }

        public static void Dust(Vector3 pos, int count)
        {
            if (dust == null)
            {
                dust = Make("Dust");
                var m = dust.main;
                m.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
                m.startColor = new Color(1f, 1f, 1f, 0.9f);
                m.gravityModifier = 0.25f;
                var sh = dust.shape;
                sh.shapeType = ParticleSystemShapeType.Hemisphere;
                sh.radius = 0.35f;
                Shrink(dust);
            }
            dust.transform.position = pos;
            dust.Emit(count);
        }

        public static void Sparkle(Vector3 pos, int count)
        {
            if (sparkle == null)
            {
                sparkle = Make("Sparkle");
                var m = sparkle.main;
                m.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
                m.startColor = new ParticleSystem.MinMaxGradient(TM.Hex("#FFE27A"), Color.white);
                m.gravityModifier = 0.4f;
                var sh = sparkle.shape;
                sh.shapeType = ParticleSystemShapeType.Sphere;
                sh.radius = 0.2f;
                Shrink(sparkle);
            }
            sparkle.transform.position = pos;
            sparkle.Emit(count);
        }

        public static void Confetti(Vector3 pos, int count = 120)
        {
            if (confetti == null)
            {
                confetti = Make("Confetti");
                var m = confetti.main;
                m.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.6f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(5f, 9f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.26f);
                var g = new Gradient();
                g.mode = GradientMode.Fixed;
                g.SetKeys(
                    new[]
                    {
                        new GradientColorKey(TM.Hex("#FF6B6B"), 0.2f), new GradientColorKey(TM.Hex("#FFC93C"), 0.4f),
                        new GradientColorKey(TM.Hex("#5B8DEF"), 0.6f), new GradientColorKey(TM.Hex("#4DD9A0"), 0.8f),
                        new GradientColorKey(TM.Hex("#FF8AD8"), 1f),
                    },
                    new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
                m.startColor = new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
                m.gravityModifier = 1.1f;
                var sh = confetti.shape;
                sh.shapeType = ParticleSystemShapeType.Cone;
                sh.angle = 28f;
                sh.radius = 0.3f;
                confetti.transform.rotation = Quaternion.Euler(-90, 0, 0);
            }
            confetti.transform.position = pos;
            confetti.Emit(count);
        }
    }
}
