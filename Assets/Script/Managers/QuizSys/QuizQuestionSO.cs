using UnityEngine;

[CreateAssetMenu(menuName = "Quiz/Question", fileName = "QuizQuestion")]
public class QuizQuestionSO : ScriptableObject
{
    [TextArea] public string question;
    public string[] options = new string[4];
    [Range(0, 3)] public int correctIndex = 0;

    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(question)) return false;
        if (options == null || options.Length != 4) return false;
        for (int i = 0; i < 4; i++)
        {
            if (string.IsNullOrWhiteSpace(options[i])) return false;
        }
        return correctIndex >= 0 && correctIndex < 4;
    }
}
