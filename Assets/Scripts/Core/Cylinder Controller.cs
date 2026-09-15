using UnityEngine;
using System.Collections.Generic;

public class CylinderController : MonoBehaviour
{
    [SerializeField] private List<bool> _cylinder = new();
    [SerializeField, Min(3)] private int _minAmmunition = 3;
    private int _maxAmmunition = 9;
    private float _minRate = 0.4f;
    private float _maxRate = 0.8f;

    public int RemainingBullets => _cylinder.Count;


    /// <summary>
    /// シリンダー再生成
    /// </summary>

    public void SetCylinder()
    {
        //シリンダー内にランダムで実弾・空砲を装填

        _cylinder.Clear();
        int random = Random.Range(_minAmmunition, _maxAmmunition);
        Vector2Int rate = new ((int)(random * _minRate), (int)(random * _maxRate) + 1);
        int realCount = Random.Range(rate.x, rate.y);
        for (int c = 0; c < realCount; c++)
        {
            _cylinder.Add(true);
        }
        for (int c = 0; c < random - realCount; c++)
        {
            _cylinder.Add(false);
        }

        //シリンダー内ランダム装填
        for (int r = _cylinder.Count - 1; r > 0; r--)
        {
            int randomIndex = Random.Range(0, r + 1);
            bool temp = _cylinder[r];
            _cylinder[r] = _cylinder[randomIndex];
            _cylinder[randomIndex] = temp;
        }
        Debug.LogWarning("実弾は " + realCount +"発、空砲は " + (random - realCount) + "発だ");
    }

    /// <summary>
    /// 次の弾薬取得
    /// </summary>

    public bool GetShot()
    {
        bool shotType = _cylinder[0];
        _cylinder.RemoveAt(0);
        return shotType;
    }

    /// <summary>
    /// シリンダー内の実弾数取得
    /// </summary>
 
    public int RemainingRealBullets
    {
        get
        {
            int count = 0;
            foreach (bool b in _cylinder) if (b) count++;
            return count;
        }
    }
}
