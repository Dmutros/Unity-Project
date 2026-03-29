using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject seedPanel;
    public TMP_InputField seedInput;
    public TextMeshProUGUI errorText;
    public void StartGame()
    {
        seedPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }

    public void ConfirmSeed()
    {
        if (int.TryParse(seedInput.text, out int seed))
        {
            if (seed >= -10000 && seed <= 10000)
            {
                PlayerPrefs.SetInt("WorldSeed", seed);
                SceneManager.LoadScene("SceneCharTst");
            }
            else
            {
                errorText.text = "Seed must be between -10000 and 10000.";
            }
        }
        else
        {
            errorText.text = "Please enter a valid number.";
        }
    }

    public void GenerateRandomSeed()
    {
        int randomSeed = Random.Range(-10000, 10001);
        seedInput.text = randomSeed.ToString();
        errorText.text = "";
    }
}
