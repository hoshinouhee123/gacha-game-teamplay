using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStageData", menuName = "TurnSystem/Stage Data")]
public class StageDataSO : ScriptableObject
{
    [Header("스테이지 기본 정보")]
    public int stageId = 1;                  // 스테이지 고유 ID (1, 2, 3...)
    public string stageName = "1-1. 초원";     // 스테이지 이름

    [Header("등장할 적 목록 (최대 3마리)")]
    public List<BattleUnit> enemies = new List<BattleUnit>(); // 이 스테이지에 나올 적 SO들
}