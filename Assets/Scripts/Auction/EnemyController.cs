using System;
using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Animator _animator;           // 人間のキャラクター（Human）のAnimator
    [SerializeField] private Animator _cylinderAnimator;   // リボルバー（Revolver）のAnimator

    public event Action OnActionAnimationFinished;
    public event Action OnReloadAnimationFinished;         // リロード全体が完全に終わった時用

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
        }
    }

    public void PlayShotAnimation()
    {
        if (_animator != null) _animator.SetTrigger("Shot");
    }

    public void PlayMyShotAnimation()
    {
        if (_animator != null) _animator.SetTrigger("MyShot");
    }

    public void PlayHitAnimation()
    {
        if (_animator != null) _animator.SetTrigger("Hit");
    }

    // ★ 人間のリロードアニメーション全体を開始する
    public void PlayReloadAnimation()
    {
        if (_animator != null) _animator.SetTrigger("Reload");
    }

    // ★ 構えを戻す ReturnRv アニメーションを再生する
    public void PlayReturnAnimation()
    {
        if (_animator != null)
        {
            _animator.SetTrigger("ReturnRv");
        }
    }

    // ==========================================================
    // ★ 人間の「Reload」アニメーション内のイベントから呼び出すメソッド
    // ==========================================================

    // ① シリンダーを開くタイミング（Rv_Reload_01 を再生）にこれを割り当てる
    public void TriggerCylinderOpen()
    {
        if (_cylinderAnimator != null)
        {
            _cylinderAnimator.SetTrigger("ReloadOpen");
        }
    }

    // ② シリンダーを閉じるタイミング（Rv_Reload_02 を再生）にこれを割り当てる
    public void TriggerCylinderClose()
    {
        if (_cylinderAnimator != null)
        {
            _cylinderAnimator.SetTrigger("ReloadClose");
        }
    }

    // ==========================================================
    // ★ アニメーション終了時のイベント
    // ==========================================================
    public void AnimationEndEvent()
    {
        OnActionAnimationFinished?.Invoke();
    }

    // 人間の Reload アニメーションの最後に呼び出す
    public void ReloadEndEvent()
    {
        OnReloadAnimationFinished?.Invoke();
    }
}