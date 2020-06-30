using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class MenuController : MonoBehaviour
{
    public int mode;
    public int diffVal;
    public int whiteDiffVal;
    public int blackDiffVal;
    public int col = 0;
    public Button pvp;
    public Button pvai;
    public Button aivai;
    public Button start;
    public Slider diff;
    public Slider whiteDiff;
    public Slider blackDiff;
    public GameObject VAISlider;
    public GameObject WhiteSlider;
    public GameObject BlackSlider;
    public GameObject white;
    public GameObject black;
    public GameObject whiteCheck;
    public GameObject blackCheck;
    public GameObject selector;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        start.interactable = false;
        white.SetActive(false);
        blackCheck.SetActive(false);
        black.SetActive(false);
        selector.SetActive(false);
        VAISlider.SetActive(false);
        WhiteSlider.SetActive(false);
        BlackSlider.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        diffVal = (int)diff.value;
        whiteDiffVal = (int)whiteDiff.value;
        blackDiffVal = (int)blackDiff.value;
    }

    public void StartGame()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void SetPVP()
    {
        mode = 0;
        white.SetActive(false);
        black.SetActive(false);
        VAISlider.SetActive(false);
        WhiteSlider.SetActive(false);
        BlackSlider.SetActive(false);
        start.interactable = true;
        selector.SetActive(true);
        selector.transform.position = pvp.transform.position;
    }
    public void SetAIVAI()
    {
        mode = 1;
        white.SetActive(false);
        black.SetActive(false);
        VAISlider.SetActive(false);
        WhiteSlider.SetActive(true);
        BlackSlider.SetActive(true);
        start.interactable = true;
        selector.SetActive(true);
        selector.transform.position = aivai.transform.position;
    }
    public void SetPVAI()
    {
        mode = 2;
        //white.SetActive(true);
        //black.SetActive(true);
        VAISlider.SetActive(true);
        WhiteSlider.SetActive(false);
        BlackSlider.SetActive(false);
        start.interactable = true;
        selector.SetActive(true);
        selector.transform.position = pvai.transform.position;
    }
    public void WhiteSet()
    {
        col = 0;
        blackCheck.SetActive(false);
        whiteCheck.SetActive(true);
    }
    public void BlackSet()
    {
        col = 1;
        blackCheck.SetActive(true);
        whiteCheck.SetActive(false);
    }
}
