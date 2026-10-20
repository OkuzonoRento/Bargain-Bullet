using System;
using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("éQè∆")]
    [SerializeField] private Animator _animator;

    public event Action OnActionAnimationFinished;
    public event Action OnReloadAnimationFinished;

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

    public void PlayReloadAnimation()
    {
        if (_animator != null) _animator.SetTrigger("Reload");
    }

    // Åö Animation Event Ç©ÇÁåƒÇŒÇÍÇÈ
    public void AnimationEndEvent()
    {
        OnActionAnimationFinished?.Invoke();
    }

    public void ReloadEndEvent()
    {
        OnReloadAnimationFinished?.Invoke();
    }
}