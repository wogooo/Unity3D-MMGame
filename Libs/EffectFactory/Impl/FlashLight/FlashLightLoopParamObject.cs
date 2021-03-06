﻿using UnityEngine;
using DG.Tweening;

namespace MMGame.EffectFactory.FlashLight
{
    public class FlashLightLoopParamObject : PlayLoopParamObject
    {
        // public
        public float Alpha;

        // private
        private FlashLightLoopParamFactory factory;
        private Transform groundLightXform;
        private Renderer groundLightRenderer;
        private Sequence seqLight;
        private Sequence seqGroundLight;
        private Light lightSource;
        // tween params
        private float fadeInTime;
        private float fadeOutTime;
        private float livingTime;
        private float lightIntensity;
        private float groundFadeInTime;
        private float groundFadeOutTime;
        private float groundLivingTime;
        private float groundLightAlpha;

        // ------------------------------------------------------

        public void SetParameters(FlashLightLoopParamFactory factory)
        {
            this.factory = factory;
        }

        void Awake()
        {
            lightSource = GetComponent<Light>();
        }

        void OnEnable()
        {
            lightSource.intensity = 0;
        }

        // ------------------------------------------------------

        public override void Loop()
        {
            if (factory.IsNull())
            {
                return;
            }

            PlayDynamicLight();

            if (factory.EnableGroundLight)
            {
                PlayFakeGroundLighting();
            }
        }

        public override void Stop()
        {
            lightSource.intensity = 0;

            if (seqLight != null)
            {
                seqLight.Pause();
            }

            if (seqGroundLight != null)
            {
                seqGroundLight.Pause();
            }

            if (groundLightXform)
            {
                groundLightXform.gameObject.SetActive(false);
            }
        }

        public override void SmoothStop()
        {
            Stop();
        }

        public override void Destroy()
        {
            ReleaseResources();
            PoolManager.Despawn(transform);
        }

        public override void SmoothDestroy()
        {
            Destroy();
        }

        // ------------------------------------------------------

        private void PlayDynamicLight()
        {
            InitDynamicLight();

            if (seqLight != null && !DynamicLightTweenParamsIsChanged())
            {
                seqLight.Restart();
            }
            else
            {
                SaveDynamicLightTweenParams();

                if (seqLight != null)
                {
                    seqLight.Kill();
                }

                seqLight = DOTween.Sequence()
                    .SetAutoKill(false)
                    .SetLoops(-1, LoopType.Restart);

                seqLight.Append(lightSource.DOIntensity(factory.LightIntensity, factory.FadeInTime));

                if (factory.LivingTime > 0)
                {
                    seqLight.Append(lightSource.DOIntensity(factory.LightIntensity + 0.01f, factory.LivingTime));
                }

                seqLight.Append(lightSource.DOIntensity(0, factory.FadeOutTime));
                seqLight.Play();
            }
        }

        private void PlayFakeGroundLighting()
        {
            Vector3 groundPoint = FlashLightSettings.Params.Agent.GetGroundLightPosition(transform.position);

            if (!groundLightXform)
            {
                groundLightXform = PoolManager.Spawn(factory.LightPrefab.name,
                    factory.LightPrefab,
                    groundPoint,
                    Quaternion.LookRotation(Vector3.up));
                groundLightRenderer = groundLightXform.GetComponent<Renderer>();
            }
            else
            {
                groundLightXform.gameObject.SetActive(true);
                groundLightXform.position = groundPoint;
                groundLightXform.forward = Vector3.up;
            }

            float scale = factory.LightRange * factory.GroundLightScale;
            groundLightXform.localScale = new Vector3(scale, scale, scale);

            Color color = factory.LightColor;
            color.a = 0;
            groundLightRenderer.material.SetColor("_TintColor", color);


            // tween
            if (seqGroundLight != null && !GroundLightTweenParamsIsChanged())
            {
                seqGroundLight.Restart();
            }
            else
            {
                SaveGroundLightTweenParams();

                if (seqGroundLight != null)
                {
                    seqGroundLight.Kill();
                }

                seqGroundLight = DOTween.Sequence()
                    .SetAutoKill(false)
                    .SetLoops(-1, LoopType.Restart)
                    .OnUpdate(UpdateGroundLight);

                seqGroundLight.Append(DOTween.To(() => Alpha, x => Alpha = x,
                    factory.GroundLightAlpha, factory.FadeInTime));

                if (factory.LivingTime > 0)
                {
                    seqGroundLight.Append(DOTween.To(() => Alpha, x => Alpha = x,
                        factory.GroundLightAlpha + 0.01f, factory.LivingTime));
                }

                seqGroundLight.Append(DOTween.To(() => Alpha, x => Alpha = x,
                    0, factory.FadeOutTime));

                seqGroundLight.Play();
            }
        }

        private void InitDynamicLight()
        {
            lightSource.color = factory.LightColor;
            lightSource.range = factory.LightRange;
            lightSource.intensity = 0;
        }

        private void SaveDynamicLightTweenParams()
        {
            fadeInTime = factory.FadeInTime;
            livingTime = factory.LivingTime;
            fadeOutTime = factory.FadeOutTime;
            lightIntensity = factory.LightIntensity;
        }

        private bool DynamicLightTweenParamsIsChanged()
        {
            return !Mathf.Approximately(fadeInTime, factory.FadeInTime) ||
                   !Mathf.Approximately(livingTime, factory.LivingTime) ||
                   !Mathf.Approximately(fadeOutTime, factory.FadeOutTime) ||
                   !Mathf.Approximately(lightIntensity, factory.LightIntensity);
        }

        private void SaveGroundLightTweenParams()
        {
            groundFadeInTime = factory.FadeInTime;
            groundLivingTime = factory.LivingTime;
            groundFadeOutTime = factory.FadeOutTime;
            groundLightAlpha = factory.GroundLightAlpha;
        }

        private bool GroundLightTweenParamsIsChanged()
        {
            return !Mathf.Approximately(groundFadeInTime, factory.FadeInTime) ||
                   !Mathf.Approximately(groundLivingTime, factory.LivingTime) ||
                   !Mathf.Approximately(groundFadeOutTime, factory.FadeOutTime) ||
                   !Mathf.Approximately(groundLightAlpha, factory.GroundLightAlpha);
        }


        //
        private void UpdateGroundLight()
        {
            // alphs
            Color color = factory.LightColor;
            color.a = Alpha;
            groundLightRenderer.material.color = color;

            // position
            groundLightXform.position =
                FlashLightSettings.Params.Agent.GetGroundLightPosition(transform.position);
        }


        //
        private void ReleaseResources()
        {
            if (seqLight != null)
            {
                seqLight.Kill();
                seqLight = null;
            }

            if (seqGroundLight != null)
            {
                seqGroundLight.Kill();
                seqGroundLight = null;
            }

            if (groundLightXform)
            {
                PoolManager.Despawn(groundLightXform);
                groundLightXform = null;
            }
        }
    }
}