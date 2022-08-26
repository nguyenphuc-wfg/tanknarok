using System;
using System.Collections;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading;
using FishNet.Object;

namespace FishNetworking.Tanknarok
{
    public class CountdownManager : NetworkBehaviour
    {
        [SerializeField] private float _countdownFrom;
        [SerializeField] private AnimationCurve _countdownCurve;
        [SerializeField] private TextMeshProUGUI _countdownUI;
        [SerializeField] AudioEmitter _audioEmitter;

        private float _countdownTimer;

        private void Start()
        {
            Reset();
        }

        public void Reset()
        {
            _countdownUI.transform.localScale = Vector3.zero;
        }

        public async UniTask Countdown(Action callback)
        {
            InitCountDown();
            int lastCount = Mathf.CeilToInt(_countdownFrom + 1);
            _countdownTimer = _countdownFrom;

            while (_countdownTimer > 0)
            {
                int currentCount = Mathf.CeilToInt(_countdownTimer);

                if (lastCount != currentCount)
                {
                    lastCount = currentCount;
                    SetTimerUI(currentCount);
                }

                float x = _countdownTimer - Mathf.Floor(_countdownTimer);

                float t = _countdownCurve.Evaluate(x);
                if (t >= 0)
                    SetScaleUITimer(t);
        
                _countdownTimer -= Time.deltaTime;
                await UniTask.Yield();
            }

            DisableUI();

            callback?.Invoke();
        }
        
        [ObserversRpc(RunLocally = true)]
        public void InitCountDown()
        {
            _countdownUI.transform.localScale = Vector3.zero;

            _countdownUI.text = _countdownFrom.ToString();
            _countdownUI.gameObject.SetActive(true);
        }

        [ObserversRpc(RunLocally = true)]
        public void SetScaleUITimer(float t)
        {
            _countdownUI.transform.localScale =  Vector3.Lerp(_countdownUI.transform.localScale,Vector3.one * t, Time.deltaTime * 10f);
        }

        [ObserversRpc(RunLocally = true)]
        public void SetTimerUI(int currentCount)
        {
            _countdownUI.text = currentCount.ToString();
            _audioEmitter.PlayOneShot();
        }
        
        [ObserversRpc(RunLocally = true)]
        public void DisableUI()
        {
            _countdownUI.gameObject.SetActive(false);
        }
    }
}