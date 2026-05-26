using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Quiz/Wave", fileName = "QuizWave")]
public class QuizWaveSO : ScriptableObject
{
    [Min(1)] public int waveNumber = 1;
    [Range(5, 10)] public int questionCountToAsk = 5;
    public List<QuizQuestionSO> questionPool = new List<QuizQuestionSO>();
}
